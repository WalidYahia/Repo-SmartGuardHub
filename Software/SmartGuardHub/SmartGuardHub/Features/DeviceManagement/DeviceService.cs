using SmartGuardHub.Features.SensorConfiguration;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols.MQTT;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class DeviceService : IAsyncInitializer
    {
        private readonly ISensorConfigRepository _repo;
        private readonly ILogger<DeviceService> _logger;
        private readonly IMqttService _mqttService;

        public DeviceService(ISensorConfigRepository repo, ILogger<DeviceService> logger, IMqttService mqttService)
        {
            _repo = repo;
            _logger = logger;
            _mqttService = mqttService;
        }

        public async Task InitializeAsync()
        {
            await RefreshSensors();
        }

        public async Task RefreshSensors(bool publishToCloud = true)
        {
            SystemManager.InstalledSensors = await _repo.GetAllAsync();

            if (publishToCloud)
            {
                var versionInfo = await _repo.GetVersionInfoAsync(ConfigType.Sensor);
                var envelope = new SensorConfigEnvelope
                {
                    ConfigVersion = versionInfo?.Version ?? Guid.NewGuid(),
                    UpdateTime    = versionInfo?.UpdateTime ?? DateTime.UtcNow,
                    Sensors       = SystemManager.InstalledSensors
                };

                await _mqttService.PublishAsync(
                    SystemManager.GetMqttTopic(MqttTopics.DeviceSensorConfig),
                    envelope,
                    retainFlag: true, 
                    qos: MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce);

                await _repo.MarkSyncedAsync(ConfigType.Sensor, DateTime.UtcNow);
            }

            _logger.LogInformation("Refreshed devices. Total: {Count}", SystemManager.InstalledSensors.Count);
        }

        public async Task<SensorConfig> CreateDeviceAsync(SensorConfig sensor)
        {
            SystemManager.InstalledSensors.Add(sensor);
            await _repo.SaveAsync(sensor, ConfigSource.Local);
            _logger.LogInformation("Created sensor: {Name} ({UnitId}) SW.{SwitchNo}", sensor.DisplayName, sensor.UnitId, sensor.SwitchNo);
            return sensor;
        }

        public async Task<SensorConfig?> UpdateDeviceAsync(SensorConfig sensor)
        {
            var idx = SystemManager.InstalledSensors.FindIndex(s => s.Id == sensor.Id);
            if (idx < 0) return null;
            SystemManager.InstalledSensors[idx] = sensor;
            await _repo.SaveAsync(sensor, ConfigSource.Local);
            _logger.LogInformation("Updated sensor: {UnitId} SW.{SwitchNo}", sensor.UnitId, sensor.SwitchNo);
            return sensor;
        }

        public async Task<bool> DeleteDeviceAsync(string id)
        {
            var removed = SystemManager.InstalledSensors.RemoveAll(s => s.Id == id) > 0;
            if (removed)
            {
                await _repo.DeleteAsync(id);
                _logger.LogInformation("Deleted sensor: {Id}", id);
            }
            return removed;
        }
    }
}

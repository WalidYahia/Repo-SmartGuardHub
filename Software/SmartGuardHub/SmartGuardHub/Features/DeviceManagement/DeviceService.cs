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
            await RefreshDevices();
        }

        public async Task RefreshDevices()
        {
            SystemManager.InstalledSensors = await _repo.GetAllAsync();
            _mqttService.PublishAsync(SystemManager.GetMqttTopic(MqttTopics.DeviceSensorConfig), SystemManager.InstalledSensors, retainFlag: true);
            _logger.LogInformation("Refreshed devices. Total: {Count}", SystemManager.InstalledSensors.Count);
        }

        public async Task<SensorConfig> CreateDeviceAsync(SensorConfig sensor)
        {
            SystemManager.InstalledSensors.Add(sensor);
            await _repo.SaveAllAsync(SystemManager.InstalledSensors);
            await RefreshDevices();
            _logger.LogInformation("Created sensor: {Name} ({UnitId}) SW.{SwitchNo}", sensor.DisplayName, sensor.UnitId, sensor.SwitchNo);
            return sensor;
        }

        public async Task<SensorConfig?> UpdateDeviceAsync(SensorConfig sensor)
        {
            var idx = SystemManager.InstalledSensors.FindIndex(s => s.Id == sensor.Id);
            if (idx < 0) return null;
            SystemManager.InstalledSensors[idx] = sensor;
            await _repo.SaveAllAsync(SystemManager.InstalledSensors);
            await RefreshDevices();
            _logger.LogInformation("Updated sensor: {UnitId} SW.{SwitchNo}", sensor.UnitId, sensor.SwitchNo);
            return sensor;
        }

        public async Task<bool> DeleteDeviceAsync(string id)
        {
            var removed = SystemManager.InstalledSensors.RemoveAll(s => s.Id == id) > 0;
            if (removed)
            {
                await _repo.SaveAllAsync(SystemManager.InstalledSensors);
                await RefreshDevices();
                _logger.LogInformation("Deleted sensor: {Id}", id);
            }
            return removed;
        }

        public async Task<bool> UpdateListDeviceAsync(List<SensorConfig> scanned)
        {
            foreach (var scannedSensor in scanned)
            {
                var existing = SystemManager.InstalledSensors.FirstOrDefault(s => s.Id == scannedSensor.Id);
                if (existing == null) continue;
                existing.IsOnline             = scannedSensor.IsOnline;
                existing.LastSeen             = scannedSensor.LastSeen;
                existing.LastReading          = scannedSensor.LastReading;
                existing.IsInInchingMode      = scannedSensor.IsInInchingMode;
                existing.InchingModeWidthInMs = scannedSensor.InchingModeWidthInMs;
                existing.LastTimeValueSet     = scannedSensor.LastTimeValueSet;
            }
            await _repo.SaveAllAsync(SystemManager.InstalledSensors);
            await RefreshDevices();
            return true;
        }
    }
}

using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SensorConfiguration;
using SmartGuardHub.Features.UserScenarios;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols.MQTT;
using System.Text.Json;
using System.Text.Json.Serialization;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class ConfigSyncService : BackgroundService
    {
        private readonly ISensorConfigRepository _configRepo;
        private readonly IMqttService _mqttService;
        private readonly IServiceScopeFactory _scopeFactory;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };

        public ConfigSyncService(
            ISensorConfigRepository configRepo,
            IMqttService mqttService,
            IServiceScopeFactory scopeFactory)
        {
            _configRepo   = configRepo;
            _mqttService  = mqttService;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SyncUnsyncedConfigs();
                }
                catch (Exception ex)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var log = scope.ServiceProvider.GetRequiredService<LoggingService>();
                    await log.LogErrorAsync(LogMessageKey.ScanDevicesError, "ConfigSyncService tick failed", ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task SyncUnsyncedConfigs()
        {
            var unsynced = await _configRepo.GetUnsyncedAsync();
            if (unsynced.Count == 0) return;

            var now = DateTime.UtcNow;

            foreach (var record in unsynced)
            {
                switch (record.ConfigType)
                {
                    case ConfigType.Sensor:
                        await PublishSensorConfig(record, now);
                        break;

                    case ConfigType.UserScenario:
                        await PublishUserScenarios(record, now);
                        break;

                    default:
                        // Not yet wired up for cloud sync (e.g. Network) — skip without retrying forever.
                        await _configRepo.MarkSyncedAsync(record.Id, now);
                        break;
                }
            }
        }

        private async Task PublishSensorConfig(DeviceConfigRecord record, DateTime now)
        {
            // The Config field already holds the full serialised list —
            // deserialise and publish; no second DB round-trip needed.
            var sensors = JsonSerializer.Deserialize<List<SensorConfig>>(record.Config, _jsonOptions);
            if (sensors == null || sensors.Count == 0)
            {
                await _configRepo.MarkSyncedAsync(record.Id, now);
                return;
            }

            var envelope = new SensorConfigEnvelope
            {
                ConfigVersion = record.ConfigVersion,
                UpdateTime    = record.UpdateTime,
                Sensors       = sensors
            };

            var topic = SystemManager.GetMqttTopic(MqttTopics.DeviceSensorConfig);
            await _mqttService.PublishAsync(topic, envelope, retainFlag: true, qos: MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
            await _configRepo.MarkSyncedAsync(record.Id, now);
        }

        private async Task PublishUserScenarios(DeviceConfigRecord record, DateTime now)
        {
            var scenarios = JsonSerializer.Deserialize<List<UserScenario>>(record.Config, _jsonOptions);
            if (scenarios == null || scenarios.Count == 0)
            {
                await _configRepo.MarkSyncedAsync(record.Id, now);
                return;
            }

            var envelope = new UserScenarioEnvelope
            {
                ConfigVersion = record.ConfigVersion,
                UpdateTime    = record.UpdateTime,
                Scenarios     = scenarios
            };

            var topic = SystemManager.GetMqttTopic(MqttTopics.DeviceUserScenario);
            await _mqttService.PublishAsync(topic, envelope, retainFlag: true, qos: MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
            await _configRepo.MarkSyncedAsync(record.Id, now);
        }
    }
}

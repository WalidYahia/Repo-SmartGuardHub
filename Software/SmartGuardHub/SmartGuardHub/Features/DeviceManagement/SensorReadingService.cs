using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SensorConfiguration;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols.MQTT;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class SensorReadingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMqttService _mqttService;
        private readonly ISensorReadingRepository _readingRepo;

        // Per-unit scan timestamps for SyncPeriodicity enforcement
        private readonly Dictionary<string, DateTime> _lastScannedAt = [];

        public SensorReadingService(
            IServiceScopeFactory scopeFactory,
            IMqttService mqttService,
            ISensorReadingRepository readingRepo)
        {
            _scopeFactory = scopeFactory;
            _mqttService  = mqttService;
            _readingRepo  = readingRepo;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 1. Sync any readings that were saved but not yet published (pending-first)
                    await SyncPendingReadingsToCloud();

                    // 2. Poll due units, save new readings
                    await ScanAndPersist();
                }
                catch (Exception ex)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var log = scope.ServiceProvider.GetRequiredService<LoggingService>();
                    await log.LogErrorAsync(LogMessageKey.ScanDevicesError, "SensorReadingService tick failed", ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        // ── Pending-first cloud sync ─────────────────────────────────────────

        private async Task SyncPendingReadingsToCloud()
        {
            var pending = await _readingRepo.GetUnsyncedAsync();

            foreach (var reading in pending)
            {
                var sensor = SystemManager.InstalledSensors.FirstOrDefault(s => s.Id == reading.SensorId);
                if (sensor == null || !sensor.EventChangeSync || !reading.IsOnline || reading.Reading == null)
                {
                    // Not publishable — mark synced so it doesn't block the queue
                    await _readingRepo.MarkSyncedAsync(reading.Id, DateTime.UtcNow);
                    continue;
                }

                var now         = DateTime.UtcNow;
                var baseReading = SensorReadingJson.Deserialize(reading.Reading);
                var cloudPayload = SystemManager.Serialize(new
                {
                    value       = baseReading?.Value,
                    status      = baseReading?.Status,
                    readingTime = baseReading?.ReadingTime,
                    publishedAt = now
                });

                await _readingRepo.UpdatePublishedAtAsync(reading.Id, now);

                var topic     = SystemManager.GetMqttTopic(MqttTopics.Readings) + $"/{reading.SensorId}";
                var published = await _mqttService.PublishAsync(topic, cloudPayload, retainFlag: GetRetainFlagAccordingToSensorType((SensorType)sensor.SensorType), qos: GetQosAccordingToSensorType((SensorType)sensor.SensorType), serialize: false);

                if (published)
                    await _readingRepo.MarkSyncedAsync(reading.Id, now);
            }
        }

        // ── Scanning ─────────────────────────────────────────────────────────

        private async Task ScanAndPersist()
        {
            var now   = DateTime.UtcNow;
            var units = SystemManager.InstalledSensors.GroupBy(s => s.UnitId).ToList();

            foreach (var unit in units)
            {
                var sensors         = unit.ToList();
                var syncPeriodicity = sensors[0].SyncPeriodicity;

                if (syncPeriodicity.HasValue
                    && _lastScannedAt.TryGetValue(unit.Key, out var lastScanned)
                    && (now - lastScanned).TotalSeconds < syncPeriodicity.Value)
                    continue;

                var systemSensor = GetSensorReader(sensors[0].SensorType);
                if (systemSensor == null) continue;

                try
                {
                    var results = await systemSensor.GetReadingsAsync(sensors);

                    if (results != null)
                    {
                        foreach (var pollResult in results)
                        {
                            var sensor = sensors.FirstOrDefault(s => s.Id == pollResult.SensorId);
                            if (sensor != null)
                                await PersistReadingIfNeeded(sensor, pollResult, now);
                        }
                        _lastScannedAt[unit.Key] = now;
                    }
                    else
                    {
                        foreach (var sensor in sensors)
                            await PersistOfflineReadingIfNeeded(sensor, now);
                    }
                }
                catch (Exception ex)
                {
                    foreach (var sensor in sensors)
                        await PersistOfflineReadingIfNeeded(sensor, now);

                    using var scope = _scopeFactory.CreateScope();
                    var log = scope.ServiceProvider.GetRequiredService<LoggingService>();
                    await log.LogErrorAsync(LogMessageKey.ScanDevicesError, $"Failed to scan unit {sensors[0].UnitId}", ex);
                }
            }
        }

        private async Task PersistReadingIfNeeded(
            SensorConfig sensor,
            SensorPollResult pollResult,
            DateTime now)
        {
            var previous = await _readingRepo.GetLatestAsync(sensor.Id);

            if (!ShouldLogNewReading(pollResult, previous, sensor, now))
                return;

            await _readingRepo.SaveAsync(new SensorReadingRecord
            {
                UnitId        = sensor.UnitId,
                SensorId      = sensor.Id,
                LogTime       = now,
                Reading       = new SensorReadingJson { Value = pollResult.Value, Status = SensorStatus.Online, ReadingTime = pollResult.ReadingTime }.Serialize(),
                IsOnline      = true,
                SyncedToCloud = !sensor.EventChangeSync,
                UpdatedFrom   = ConfigSource.Local
            });
        }

        private async Task PersistOfflineReadingIfNeeded(SensorConfig sensor, DateTime now)
        {
            var previous = await _readingRepo.GetLatestAsync(sensor.Id);

            // Only log if online status actually changed (avoid flooding the DB)
            if (previous?.IsOnline == false) return;

            var prevReading = SensorReadingJson.Deserialize(previous?.Reading);
            await _readingRepo.SaveAsync(new SensorReadingRecord
            {
                UnitId        = sensor.UnitId,
                SensorId      = sensor.Id,
                LogTime       = now,
                Reading       = new SensorReadingJson { Value = prevReading?.Value, Status = SensorStatus.Offline, ReadingTime = prevReading?.ReadingTime ?? now }.Serialize(),
                IsOnline      = false,
                SyncedToCloud = true,  // offline readings are never published to cloud
                UpdatedFrom = ConfigSource.Local
            });
        }

        // ── Log-to-DB decision ───────────────────────────────────────────────

        private static bool ShouldLogNewReading(
            SensorPollResult result,
            SensorReadingRecord? previous,
            SensorConfig config,
            DateTime now)
        {
            if (previous == null) return true;
            if (result.IsOnline != previous.IsOnline) return true;
            if (!config.OnlySaveRecordOnChange && (now - previous.LogTime).TotalMinutes >= 5) return true;

            var newVal  = result.Value;
            var prevVal = SensorReadingJson.Deserialize(previous.Reading)?.Value;

            if (newVal == prevVal) return false;

            if (!config.EventChangeDelta.HasValue) return true;

            if (double.TryParse(newVal, out var nv) && double.TryParse(prevVal, out var pv))
                return Math.Abs(nv - pv) >= config.EventChangeDelta.Value;

            return true;  // non-numeric: any change
        }

        // ── Factory: resolve reader by sensor type ───────────────────────────

        private ISystemSensor? GetSensorReader(int sensorType)
        {
            using var scope = _scopeFactory.CreateScope();
            var sensors = scope.ServiceProvider.GetRequiredService<IEnumerable<ISystemSensor>>();
            return sensors.FirstOrDefault(s => s.SensorType == (SensorType)sensorType);
        }

        private MQTTnet.Protocol.MqttQualityOfServiceLevel GetQosAccordingToSensorType(SensorType sensorType)
        {
            switch (sensorType)
            {
                case SensorType.SonOffMiniR3Swich:
                    return MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce;

                case SensorType.Unknown:
                case SensorType.Temperature:
                case SensorType.Humidity:
                case SensorType.Pressure:
                case SensorType.Motion:
                case SensorType.Gas:
                case SensorType.Light:
                case SensorType.Vibration:
                case SensorType.Current:
                case SensorType.Voltage:
                default:
                    return MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce;
            }
        }
        private bool GetRetainFlagAccordingToSensorType(SensorType sensorType)
        {
            switch (sensorType)
            {
                case SensorType.SonOffMiniR3Swich:
                    return false;

                case SensorType.Unknown:
                case SensorType.Temperature:
                case SensorType.Humidity:
                case SensorType.Pressure:
                case SensorType.Motion:
                case SensorType.Gas:
                case SensorType.Light:
                case SensorType.Vibration:
                case SensorType.Current:
                case SensorType.Voltage:
                default:
                    return true;
            }
        }
    }
}

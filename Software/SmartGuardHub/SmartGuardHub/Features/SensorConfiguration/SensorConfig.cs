using SmartGuardHub.Infrastructure;

namespace SmartGuardHub.Features.SensorConfiguration
{
    public class SensorConfig
    {
        public string Id { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public Guid SensorId { get; set; }
        public int SwitchNo { get; set; }
        public string UnitId { get; set; } = string.Empty;
        public int? Address { get; set; }
        public int? Port { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public int SensorType { get; set; }
        public int Protocol { get; set; }
        public string DataPath { get; set; } = string.Empty;
        public string InfoPath { get; set; } = string.Empty;
        public string InchingPath { get; set; } = string.Empty;
        public int? SyncPeriodicity { get; set; }
        public bool EventChangeSync { get; set; }
        public double? EventChangeDelta { get; set; }
        public bool IsInInchingMode { get; set; }
        public int InchingModeWidthInMs { get; set; }
        public DateTime InstalledAt { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
        public string? LastReading { get; set; }

        // Runtime state — managed locally, not persisted to cloud
        public bool IsOnline { get; set; }
        public DateTime LastSeen { get; set; }
        public DateTime LastTimeValueSet { get; set; }

        public static string ComputeId(
            string deviceId,
            Enums.SensorType sensorType,
            string unitId,
            Enums.SwitchNo switchNo,
            int? address,
            int? port)
        {
            var unitIdPart  = string.IsNullOrEmpty(unitId)                                 ? "unitId"  : unitId;
            var switchPart  = switchNo == SmartGuardHub.Infrastructure.Enums.SwitchNo.Non ? "switch"  : switchNo.ToString();
            var addressPart = address.HasValue                                             ? address.Value.ToString() : "address";
            var portPart    = port.HasValue                                                ? port.Value.ToString()    : "port";

            return $"{deviceId}_{sensorType}_{unitIdPart}_{switchPart}_{addressPart}_{portPart}";
        }
    }
}

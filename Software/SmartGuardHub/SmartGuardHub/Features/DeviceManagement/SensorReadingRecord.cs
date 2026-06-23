using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class SensorReadingRecord
    {
        public int Id { get; set; }
        public string UnitId { get; set; } = string.Empty;

        /// <summary>Maps to SensorConfig.Id.</summary>
        public string SensorId { get; set; } = string.Empty;

        public DateTime LogTime { get; set; }

        /// <summary>JSON: { "value": "...", "status": "online|offline|error" }</summary>
        public string? Reading { get; set; }

        public bool IsOnline { get; set; }
        public bool SyncedToCloud { get; set; }
        public DateTime? PublishedAt { get; set; }
        public ConfigSource UpdatedFrom { get; set; }

    }
}

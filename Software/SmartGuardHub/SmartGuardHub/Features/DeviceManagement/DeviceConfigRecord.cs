using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.DeviceManagement
{
    /// <summary>
    /// One row per ConfigType.  Config holds the full JSON list for that type,
    /// e.g. all SensorConfig objects serialized as a JSON array.
    /// </summary>
    public class DeviceConfigRecord
    {
        public int Id { get; set; }
        public ConfigType ConfigType { get; set; }
        public DateTime UpdateTime { get; set; }

        /// <summary>JSON array of all configs for this ConfigType (e.g. List&lt;SensorConfig&gt;).</summary>
        public string Config { get; set; } = string.Empty;

        public ConfigSource UpdatedFrom { get; set; }
        public Guid ConfigVersion { get; set; }
        public bool SyncedToCloud { get; set; }
        public DateTime? TimeToSyncedToCloud { get; set; }
    }
}

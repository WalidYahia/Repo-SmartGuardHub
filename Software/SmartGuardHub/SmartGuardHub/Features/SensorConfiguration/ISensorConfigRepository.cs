using SmartGuardHub.Features.DeviceManagement;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.SensorConfiguration
{
    public interface ISensorConfigRepository
    {
        Task<List<SensorConfig>> GetAllAsync();

        /// <summary>Returns the ConfigVersion and UpdateTime for the given ConfigType row, or null if no row exists.</summary>
        Task<(Guid Version, DateTime UpdateTime)?> GetVersionInfoAsync(ConfigType configType);

        /// <summary>Upsert a single config. Local saves generate a new ConfigVersion; cloud saves use the provided version.</summary>
        Task<bool> SaveAsync(SensorConfig config, ConfigSource source, Guid configVersion = default);

        /// <summary>
        /// Replaces the entire stored config list in one DB row.
        /// Cloud source marks the row as already synced; Local source marks it unsynced.
        /// Cloud saves use the provided configVersion; local saves always generate a new one.
        /// </summary>
        Task<bool> SaveAllAsync(List<SensorConfig> configs, ConfigSource source, Guid configVersion = default);

        Task<bool> DeleteAsync(string id);

        Task<List<DeviceConfigRecord>> GetUnsyncedAsync();

        /// <summary>Marks the record for a specific ConfigType as synced (used after a direct publish).</summary>
        Task MarkSyncedAsync(ConfigType configType, DateTime syncedAt);

        /// <summary>Marks a specific record by PK as synced (used by ConfigSyncService).</summary>
        Task MarkSyncedAsync(int id, DateTime syncedAt);
    }
}

using Microsoft.EntityFrameworkCore;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Infrastructure;
using System.Text.Json;
using System.Text.Json.Serialization;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.SensorConfiguration
{
    /// <summary>
    /// Persists sensor configuration as a single DeviceConfigRecord row
    /// whose Config column holds the JSON-serialised List&lt;SensorConfig&gt;.
    ///
    /// There is at most ONE row per ConfigType in the table.
    /// Every local write marks SyncedToCloud = false so ConfigSyncService
    /// will pick it up on its next 1-minute tick.
    /// </summary>
    public class DbSensorConfigRepository : ISensorConfigRepository
    {
        private readonly IDbContextFactory<SmartGuardDbContext> _contextFactory;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            Converters    = { new JsonStringEnumConverter() }
        };

        public DbSensorConfigRepository(IDbContextFactory<SmartGuardDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        // ── Read ─────────────────────────────────────────────────────────────

        public async Task<List<SensorConfig>> GetAllAsync()
        {
            await using var db = _contextFactory.CreateDbContext();

            var record = await db.DeviceConfigs
                .FirstOrDefaultAsync(r => r.ConfigType == ConfigType.Sensor);

            if (record == null)
                return [];

            return JsonSerializer.Deserialize<List<SensorConfig>>(record.Config, _jsonOptions) ?? [];
        }

        public async Task<(Guid Version, DateTime UpdateTime)?> GetVersionInfoAsync(ConfigType configType)
        {
            await using var db = _contextFactory.CreateDbContext();
            var record = await db.DeviceConfigs
                .Where(r => r.ConfigType == configType)
                .Select(r => new { r.ConfigVersion, r.UpdateTime })
                .FirstOrDefaultAsync();

            if (record == null) return null;
            return (record.ConfigVersion, record.UpdateTime);
        }

        // ── Write (single sensor) ─────────────────────────────────────────────

        /// <summary>
        /// Loads the current list, upserts this one sensor, then persists the whole list.
        /// </summary>
        public async Task<bool> SaveAsync(SensorConfig config, ConfigSource source, Guid configVersion = default)
        {
            await using var db = _contextFactory.CreateDbContext();

            var record = await db.DeviceConfigs
                .FirstOrDefaultAsync(r => r.ConfigType == ConfigType.Sensor);

            var all = DeserializeList(record?.Config);

            var idx = all.FindIndex(s => s.Id == config.Id);
            if (idx >= 0)
                all[idx] = config;
            else
                all.Add(config);

            Upsert(db, record, all, source, configVersion);
            await db.SaveChangesAsync();
            return true;
        }

        // ── Write (full list) ─────────────────────────────────────────────────

        /// <summary>
        /// Replaces the entire stored list with the supplied one.
        /// When source is Cloud the record is marked as already synced.
        /// </summary>
        public async Task<bool> SaveAllAsync(List<SensorConfig> configs, ConfigSource source, Guid configVersion = default)
        {
            await using var db = _contextFactory.CreateDbContext();

            var record = await db.DeviceConfigs
                .FirstOrDefaultAsync(r => r.ConfigType == ConfigType.Sensor);

            Upsert(db, record, configs, source, configVersion);
            await db.SaveChangesAsync();
            return true;
        }

        // ── Delete ────────────────────────────────────────────────────────────

        public async Task<bool> DeleteAsync(string id)
        {
            await using var db = _contextFactory.CreateDbContext();

            var record = await db.DeviceConfigs
                .FirstOrDefaultAsync(r => r.ConfigType == ConfigType.Sensor);

            if (record == null) return false;

            var all = DeserializeList(record.Config);
            if (all.RemoveAll(s => s.Id == id) == 0) return false;

            Upsert(db, record, all, ConfigSource.Local, default);
            await db.SaveChangesAsync();
            return true;
        }

        // ── Sync helpers ──────────────────────────────────────────────────────

        public async Task<List<DeviceConfigRecord>> GetUnsyncedAsync()
        {
            await using var db = _contextFactory.CreateDbContext();
            return await db.DeviceConfigs
                .Where(r => !r.SyncedToCloud)
                .OrderBy(r => r.UpdateTime)
                .ToListAsync();
        }

        public async Task MarkSyncedAsync(ConfigType configType, DateTime syncedAt)
        {
            await using var db = _contextFactory.CreateDbContext();
            var record = await db.DeviceConfigs.FirstOrDefaultAsync(r => r.ConfigType == configType);
            if (record == null) return;

            record.SyncedToCloud       = true;
            record.TimeToSyncedToCloud = syncedAt;
            await db.SaveChangesAsync();
        }

        public async Task MarkSyncedAsync(int id, DateTime syncedAt)
        {
            await using var db = _contextFactory.CreateDbContext();
            var record = await db.DeviceConfigs.FindAsync(id);
            if (record == null) return;

            record.SyncedToCloud       = true;
            record.TimeToSyncedToCloud = syncedAt;
            await db.SaveChangesAsync();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static List<SensorConfig> DeserializeList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return [];
            return JsonSerializer.Deserialize<List<SensorConfig>>(json, _jsonOptions) ?? [];
        }

        /// <summary>
        /// Updates an existing record in-place or adds a new one.
        /// SyncedToCloud is set to false for Local saves (needs cloud push),
        /// true for Cloud saves (already received from cloud — no need to re-publish).
        /// </summary>
        private static void Upsert(
            SmartGuardDbContext db,
            DeviceConfigRecord? existing,
            List<SensorConfig> configs,
            ConfigSource source,
            Guid configVersion)
        {
            var now     = DateTime.UtcNow;
            var synced  = source == ConfigSource.Cloud;
            var version = source == ConfigSource.Local ? Guid.NewGuid() : configVersion;
            var json    = JsonSerializer.Serialize(configs, _jsonOptions);

            if (existing != null)
            {
                existing.Config              = json;
                existing.UpdateTime          = now;
                existing.UpdatedFrom         = source;
                existing.ConfigVersion       = version;
                existing.SyncedToCloud       = synced;
                existing.TimeToSyncedToCloud = synced ? now : null;
            }
            else
            {
                db.DeviceConfigs.Add(new DeviceConfigRecord
                {
                    ConfigType           = ConfigType.Sensor,
                    Config               = json,
                    UpdateTime           = now,
                    UpdatedFrom          = source,
                    ConfigVersion        = version,
                    SyncedToCloud        = synced,
                    TimeToSyncedToCloud  = synced ? now : null
                });
            }
        }
    }
}

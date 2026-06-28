using Microsoft.EntityFrameworkCore;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Infrastructure;
using System.Text.Json;
using System.Text.Json.Serialization;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserScenarios
{
    /// <summary>
    /// Persists user scenarios as a single DeviceConfigRecord row (ConfigType.UserScenario)
    /// whose Config column holds the JSON-serialised List&lt;UserScenario&gt;.
    ///
    /// Mirrors DbSensorConfigRepository's one-row-per-ConfigType design so the same
    /// DeviceConfigs table, ConfigSyncService and cloud conflict-resolution logic apply.
    /// </summary>
    public class DbUserScenarioRepository : IUserScenarioRepository
    {
        private readonly IDbContextFactory<SmartGuardDbContext> _contextFactory;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            Converters    = { new JsonStringEnumConverter() }
        };

        public DbUserScenarioRepository(IDbContextFactory<SmartGuardDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        // ── Read ─────────────────────────────────────────────────────────────

        public async Task<List<UserScenario>> GetAllAsync()
        {
            await using var db = _contextFactory.CreateDbContext();

            var record = await db.DeviceConfigs
                .FirstOrDefaultAsync(r => r.ConfigType == ConfigType.UserScenario);

            return record == null ? [] : DeserializeList(record.Config);
        }

        public async Task<List<UserScenario>> GetEnabledAsync()
        {
            var all = await GetAllAsync();
            return all.Where(x => x.IsEnabled).ToList();
        }

        public async Task<UserScenario?> GetByIdAsync(string id)
        {
            var all = await GetAllAsync();
            return all.FirstOrDefault(x => x.Id == id);
        }

        // ── Write (single scenario) ────────────────────────────────────────────

        /// <summary>
        /// Loads the current list, upserts this one scenario, then persists the whole list.
        /// </summary>
        public async Task<bool> SaveAsync(UserScenario scenario, ConfigSource source)
        {
            await using var db = _contextFactory.CreateDbContext();

            var record = await db.DeviceConfigs
                .FirstOrDefaultAsync(r => r.ConfigType == ConfigType.UserScenario);

            var all = DeserializeList(record?.Config);

            var idx = all.FindIndex(s => s.Id == scenario.Id);
            if (idx >= 0)
                all[idx] = scenario;
            else
                all.Add(scenario);

            Upsert(db, record, all, source, default);
            await db.SaveChangesAsync();
            return true;
        }

        // ── Write (full list) ─────────────────────────────────────────────────

        /// <summary>
        /// Replaces the entire stored list with the supplied one.
        /// When source is Cloud the record is marked as already synced.
        /// </summary>
        public async Task<bool> SaveAllAsync(List<UserScenario> scenarios, ConfigSource source, Guid configVersion = default)
        {
            await using var db = _contextFactory.CreateDbContext();

            var record = await db.DeviceConfigs
                .FirstOrDefaultAsync(r => r.ConfigType == ConfigType.UserScenario);

            Upsert(db, record, scenarios, source, configVersion);
            await db.SaveChangesAsync();
            return true;
        }

        // ── Delete ────────────────────────────────────────────────────────────

        public async Task<bool> DeleteAsync(string id, ConfigSource source)
        {
            await using var db = _contextFactory.CreateDbContext();

            var record = await db.DeviceConfigs
                .FirstOrDefaultAsync(r => r.ConfigType == ConfigType.UserScenario);

            if (record == null) return false;

            var all = DeserializeList(record.Config);
            if (all.RemoveAll(s => s.Id == id) == 0) return false;

            Upsert(db, record, all, source, default);
            await db.SaveChangesAsync();
            return true;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static List<UserScenario> DeserializeList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return [];
            return JsonSerializer.Deserialize<List<UserScenario>>(json, _jsonOptions) ?? [];
        }

        /// <summary>
        /// Updates an existing record in-place or adds a new one.
        /// SyncedToCloud is always set to false — every save (Local or Cloud-received)
        /// needs to go through ConfigSyncService's next publish cycle.
        /// </summary>
        private static void Upsert(
            SmartGuardDbContext db,
            DeviceConfigRecord? existing,
            List<UserScenario> scenarios,
            ConfigSource source,
            Guid configVersion)
        {
            var now     = DateTime.UtcNow;
            var version = source == ConfigSource.Local ? Guid.NewGuid() : configVersion;
            var json    = JsonSerializer.Serialize(scenarios, _jsonOptions);

            if (existing != null)
            {
                existing.Config              = json;
                existing.UpdateTime          = now;
                existing.UpdatedFrom         = source;
                existing.ConfigVersion       = version;
                existing.SyncedToCloud       = false;
                existing.TimeToSyncedToCloud = null;
            }
            else
            {
                db.DeviceConfigs.Add(new DeviceConfigRecord
                {
                    ConfigType           = ConfigType.UserScenario,
                    Config               = json,
                    UpdateTime           = now,
                    UpdatedFrom          = source,
                    ConfigVersion        = version,
                    SyncedToCloud        = false,
                    TimeToSyncedToCloud  = null
                });
            }
        }
    }
}

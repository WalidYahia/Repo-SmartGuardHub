using Microsoft.EntityFrameworkCore;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Infrastructure;

namespace SmartGuardHub.Features.SensorConfiguration
{
    public class SensorReadingRepository : ISensorReadingRepository
    {
        private readonly IDbContextFactory<SmartGuardDbContext> _contextFactory;

        public SensorReadingRepository(IDbContextFactory<SmartGuardDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task SaveAsync(SensorReadingRecord reading)
        {
            await using var db = _contextFactory.CreateDbContext();
            db.SensorReadings.Add(reading);
            await db.SaveChangesAsync();
        }

        public async Task<SensorReadingRecord?> GetLatestAsync(string sensorId)
        {
            await using var db = _contextFactory.CreateDbContext();
            return await db.SensorReadings
                .Where(r => r.SensorId == sensorId)
                .OrderByDescending(r => r.LogTime)
                .FirstOrDefaultAsync();
        }

        public async Task<Dictionary<string, SensorReadingRecord>> GetLatestBatchAsync(IEnumerable<string> sensorIds)
        {
            var ids = sensorIds.ToList();
            await using var db = _contextFactory.CreateDbContext();

            // ORDER BY Time DESC in SQL, then pick the first per sensor in memory.
            // EF Core cannot translate GroupBy + OrderBy + First to SQLite, so we do the
            // grouping on the client side after a sorted fetch.
            var rows = await db.SensorReadings
                .Where(r => ids.Contains(r.SensorId))
                .OrderByDescending(r => r.LogTime)
                .ToListAsync();

            return rows
                .GroupBy(r => r.SensorId)
                .ToDictionary(g => g.Key, g => g.First());
        }

        public async Task<List<SensorReadingRecord>> GetUnsyncedAsync()
        {
            await using var db = _contextFactory.CreateDbContext();
            return await db.SensorReadings
                .Where(r => !r.SyncedToCloud)
                .OrderBy(r => r.LogTime)
                .ToListAsync();
        }

        public async Task UpdatePublishedAtAsync(int id, DateTime publishedAt)
        {
            await using var db = _contextFactory.CreateDbContext();
            var record = await db.SensorReadings.FindAsync(id);
            if (record == null) return;

            record.PublishedAt = publishedAt;
            await db.SaveChangesAsync();
        }

        public async Task MarkSyncedAsync(int id, DateTime syncedAt)
        {
            await using var db = _contextFactory.CreateDbContext();
            var record = await db.SensorReadings.FindAsync(id);
            if (record == null) return;

            record.SyncedToCloud = true;
            await db.SaveChangesAsync();
        }
    }
}


using Microsoft.EntityFrameworkCore;
using SmartGuardHub.Infrastructure;

namespace SmartGuardHub.Features.Logging
{
    public class SystemLogRepository : ISystemLogRepository
    {
        private readonly SystemLogDbContext _context;

        public SystemLogRepository(SystemLogDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(SystemLog log)
        {
            if (log == null)
                throw new ArgumentNullException(nameof(log));

            _context.SystemLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetCount()
        {
            return await _context.SystemLogs.CountAsync();
        }

        public async Task TrimOldRowsAsync(int excessCount /* totalSavedCount - maxRowsLimit */)
        {
            var oldRecords = await _context.SystemLogs
                .OrderBy(e => e.Id)
                .Take(excessCount)
                .ToListAsync();

            _context.SystemLogs.RemoveRange(oldRecords);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteLogsOlderThanTime(DateTime time)
        {
            var logsToDelete = _context.SystemLogs.Where(l => l.LogTime < time);
            _context.SystemLogs.RemoveRange(logsToDelete);
            await _context.SaveChangesAsync();
        }

        public async Task<List<SystemLog>> GetLogsByLevel(LogLevel logLevel)
        {
            return await _context.SystemLogs
                .Where(l => l.Level == logLevel.ToString())
                .OrderByDescending(l => l.LogTime)
                .ToListAsync();
        }

        public async Task<List<SystemLog>> GetLogsByMessageKey(LogMessageKey logMessageKey)
        {
            return await _context.SystemLogs
                .Where(l => l.MessageKey == logMessageKey.ToString())
                .OrderByDescending(l => l.LogTime)
                .ToListAsync();
        }

        public async Task<List<SystemLog>> GetLogsByTime(DateTime timeFrom, DateTime timeTo)
        {
            return await _context.SystemLogs
                .Where(l => l.LogTime >= timeFrom && l.LogTime <= timeTo)
                .OrderByDescending(l => l.LogTime)
                .ToListAsync();
        }
    }
}

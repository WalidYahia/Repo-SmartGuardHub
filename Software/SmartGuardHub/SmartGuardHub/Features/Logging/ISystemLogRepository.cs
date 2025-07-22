using SmartGuardHub.Features.DeviceManagement;

namespace SmartGuardHub.Features.Logging
{
    public interface ISystemLogRepository
    {
        Task CreateAsync(SystemLog log);
        Task DeleteLogsOlderThanTime(DateTime time);
        Task<List<SystemLog>> GetLogsByLevel(LogLevel logLevel);
        Task<List<SystemLog>> GetLogsByMessageKey(LogMessageKey logMessageKey);
        Task<List<SystemLog>> GetLogsByTime(DateTime timeFrom, DateTime timeTo);
        Task<int> GetCount();
        Task TrimOldRowsAsync(int excessCount /* totalSavedCount - maxRowsLimit */);
    }

    public enum LogLevel
    {
        INFO = 0,
        TRACE = 1,
        ERROR = 2,
    }
    public enum LogMessageKey
    {
        DevicesController = 0,
        DevicesConflict = 1,
        RestProtocol = 2,
        Message1 = 3,
    }
}

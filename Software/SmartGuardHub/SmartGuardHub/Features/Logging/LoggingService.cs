using System.Reflection.Emit;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Infrastructure;

namespace SmartGuardHub.Features.Logging
{
    public class LoggingService
    {
        private readonly ISystemLogRepository _logRepository;

        public LoggingService(ISystemLogRepository systemLogRepository) 
        {
            _logRepository = systemLogRepository;
        }

        public async Task LogInfoAsync(LogMessageKey messageKey, string message)
        {
            try
            {
                var log = new SystemLog
                {
                    Level = LogLevel.INFO.ToString(),
                    MessageKey = messageKey.ToString(),
                    Message = message
                };

                await _logRepository.CreateAsync(log);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log to database: {messageKey} - {message} - {ex}");
            }
        }

        public async Task LogTraceAsync(LogMessageKey messageKey, string message)
        {
            try
            {
                var log = new SystemLog
                {
                    Level = LogLevel.TRACE.ToString(),
                    MessageKey = messageKey.ToString(),
                    Message = message
                };

                await _logRepository.CreateAsync(log);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log to database: {messageKey} - {message} - {ex}");
            }
        }

        public async Task LogErrorAsync(LogMessageKey messageKey, string message, Exception exception = null)
        {
            try
            {
                var log = new SystemLog
                {
                    Level = LogLevel.ERROR.ToString(),
                    MessageKey = messageKey.ToString(),
                    Message = message,
                    Exception = SystemManager.GetRecursiveMessagesWithStack(exception)
                };

                await _logRepository.CreateAsync(log);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log to database: {messageKey} - {message} - {SystemManager.GetRecursiveMessagesWithStack(ex)}");
            }
        }

        public async Task<List<SystemLog>> GetLogsByLevelAsync(LogLevel level)
        {
            try
            {
                return await _logRepository.GetLogsByLevel(level);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to GetLogsByLevelAsync: {level} - {ex}");
                return new List<SystemLog>();
            }
        }

        public async Task<List<SystemLog>> GetLogsByMessageKeyAsync(LogMessageKey messageKey)
        {
            try
            {
                return await _logRepository.GetLogsByMessageKey(messageKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to GetLogsByMessageKeyAsync: {messageKey} - {ex}");
                return new List<SystemLog>();
            }
        }

        public async Task<List<SystemLog>> GetLogsByTimeAsync(DateTime timeFrom, DateTime timeTo)
        {
            try
            {
                return await _logRepository.GetLogsByTime(timeFrom, timeTo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to GetLogsByMessageKeyAsync: From {timeFrom} To {timeTo} - {ex}");
                return new List<SystemLog>();
            }
        }

        public async Task CleanupOldLogsAsync(int daysToKeep, int countToKeep = 1000)
        {
            try
            {
                int totalCount = await _logRepository.GetCount();

                if (totalCount > countToKeep)
                {
                    int excessCount = totalCount - countToKeep;
                    await _logRepository.TrimOldRowsAsync(excessCount);
                    Console.WriteLine($"Cleanup {excessCount}, log count Was {totalCount}");
                }
                else
                {
                    var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                    await _logRepository.DeleteLogsOlderThanTime(cutoffDate);
                    Console.WriteLine($"Cleanup cutoffDate {cutoffDate}, log count Was {totalCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to CleanupOldLogsAsync: daysToKeep: {daysToKeep} - countToKeep: {countToKeep} - {ex}");
            }
        }
    }
}

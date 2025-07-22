namespace SmartGuardHub.Features.Logging
{
    public class LogCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public LogCleanupService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var loggingService = scope.ServiceProvider.GetRequiredService<LoggingService>();

                    try
                    {
                        await loggingService.CleanupOldLogsAsync(daysToKeep: 7, countToKeep: 1000);
                    }
                    catch (Exception ex)
                    {
                        await loggingService.LogErrorAsync(LogMessageKey.LogsCleanupError, "Failed to cleanup logs", ex);
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}

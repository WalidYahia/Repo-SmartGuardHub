using Microsoft.EntityFrameworkCore;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols.MQTT;

namespace SmartGuardHub.Application
{
    public class ApplicationStartupService : IHostedLifecycleService
    {
        private readonly IServiceProvider _serviceProvider;

        public ApplicationStartupService(
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Initialize system environment
            await SystemManager.InitSystemEnvironment();

            // Ensure database folder exists
            var dbPath = Path.Combine(AppContext.BaseDirectory, "Database", "Production");
            if (!Directory.Exists(dbPath))
            {
                Directory.CreateDirectory(dbPath);
            }

            // Ensure databases are created and migrated
            using (var scope = _serviceProvider.CreateScope())
            {
                // Main database
                var mainContext = scope.ServiceProvider.GetRequiredService<SmartGuardDbContext>();
                await mainContext.Database.MigrateAsync(cancellationToken);
                DatabaseSeeder.SeedData(mainContext);

                // System log database
                var logContext = scope.ServiceProvider.GetRequiredService<SystemLogDbContext>();
                await logContext.Database.MigrateAsync(cancellationToken);
            }

            // Start MQTT service
            var mqttService = _serviceProvider.GetRequiredService<IMqttService>();
            await mqttService.StartAsync();

            // Force resolve MQTT message listener at startup
            using (var scope = _serviceProvider.CreateScope())
            {
                var handler = scope.ServiceProvider.GetRequiredService<MqttMessageListener>();
            }

            // Call async initialization
            using (var scope = _serviceProvider.CreateScope())
            {
                var initializer = scope.ServiceProvider.GetRequiredService<IAsyncInitializer>();
                await initializer.InitializeAsync();
            }
        }

        public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

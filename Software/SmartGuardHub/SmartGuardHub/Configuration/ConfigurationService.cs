using System.Text.Json;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Infrastructure;
using LogLevel = SmartGuardHub.Features.Logging.LogLevel;

namespace SmartGuardHub.Configuration
{
    public class ConfigurationService
    {
        private readonly IServiceProvider _serviceProvider;
        
        private const string MqttConfigPath = "./Configuration/MqttConfig.json";

        public MqttConfig MqttConfig { get; private set; }

        public ConfigurationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task LoadMqttConfig()
        {
            try
            {
                var stringConfig = await FileManager.LoadFileAsync(MqttConfigPath);

                if (!string.IsNullOrEmpty(stringConfig))
                    MqttConfig = JsonSerializer.Deserialize<MqttConfig>(stringConfig);
            }
            catch (Exception ex)
            {
                await Log(LogLevel.ERROR, LogMessageKey.LoadConfig , "MqttConfig", ex);
            }
        }

        private async Task Log(LogLevel logLevel, LogMessageKey logMessageKey, string message, Exception ex = null)
        {
            using var scope = _serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<LoggingService>();

            switch (logLevel)
            {
                case LogLevel.INFO:
                    await logger.LogInfoAsync(logMessageKey, message);
                    break;

                case LogLevel.TRACE:
                    await logger.LogTraceAsync(logMessageKey, message);
                    break;

                case LogLevel.ERROR:
                    await logger.LogErrorAsync(logMessageKey, message, ex);
                    break;
            }

        }
    }


    public class MqttConfig
    {
        public string Broker { get; set; }
        public int Port { get; set; }
        public bool UseTls { get; set; }
        public string ClientId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}

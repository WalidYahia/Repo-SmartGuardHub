using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Protocol;
using SmartGuardHub.Configuration;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols.MQTT;
using System.Text;
using System.Text.Json;
using LogLevel = SmartGuardHub.Features.Logging.LogLevel;

namespace SmartGuardHub.Protocols.MQTT
{
    public class MqttService : IMqttService
    {
        public event Func<string, string, Task>? MessageReceived;

        private IMqttClient _mqttClient;
        private MqttClientOptions _mqttClientTlsOptions;

        private readonly IServiceProvider _serviceProvider;
        private readonly ConfigurationService _config;

        public MqttService(IServiceProvider serviceProvider, ConfigurationService configService)
        {
            _config = configService;
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync()
        {
            await _config.LoadMqttConfig();

            if (_config.MqttConfig == null)
            {
                await Log(LogLevel.ERROR, LogMessageKey.MissingConfig, "MqttConfig");
                return;
            }

            string broker = _config.MqttConfig.Broker;
            int port = _config.MqttConfig.Port;
            string clientId = _config.MqttConfig.ClientId;
            string username = _config.MqttConfig.Username;
            string password = _config.MqttConfig.Password;
            bool useTls = _config.MqttConfig.UseTls;

            var factory = new MqttClientFactory();

            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = useTls, // Enable TLS
                AllowUntrustedCertificates = true, // For testing purposes only
                IgnoreCertificateChainErrors = true, // For testing purposes only
                IgnoreCertificateRevocationErrors = true // For testing purposes only
            };

            // Create a MQTT client instance
            _mqttClient = factory.CreateMqttClient();

            // Create MQTT client options
            _mqttClientTlsOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, port) // MQTT broker address and port
                .WithCredentials(username, password) // Set username and password
                .WithClientId(clientId)
                .WithCleanSession(false)
                .WithTlsOptions(tlsOptions)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(5)) // Set keep-alive period
                .Build();

            // Set up event handlers
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;
            _mqttClient.ConnectedAsync += OnConnected;
            _mqttClient.DisconnectedAsync += OnDisconnected;

            // Start the client
            await ConnectAsync(3);

            // Subscribe to all topics
            await SubscribeAsync(SystemManager.GetMqttTopicPath(MqttTopics.RemoteUpdateTopic_Publish));
            await SubscribeAsync(SystemManager.GetMqttTopicPath(MqttTopics.RemoteActionTopic_Publish));
        }

        /// <summary>
        /// Try to connect to the MQTT broker, retrying up to trialsCount times if necessary.
        /// Set trialsCount = -1 to try for ever.
        /// </summary>
        /// <param name="trialsCount"></param>
        /// <returns></returns>
        public async Task ConnectAsync(int trialsCount)
        {
            int x = 0;
            while (_mqttClient?.IsConnected != true && (trialsCount == -1 || x < trialsCount))
            {
                await TryConnectAsync();

                await Task.Delay(1000);
            }
        }

        public async Task DisconnectAsync()
        {
            if (_mqttClient != null)
            {
                await _mqttClient.DisconnectAsync();
                _mqttClient.Dispose();
            }
        }

        public async Task PublishAsync(string topic, object message, bool retainFlag)
        {
            await ConnectAsync(3);

            if (_mqttClient.IsConnected)
            {
                var json = JsonSerializer.Serialize(message);
                var payload = Encoding.UTF8.GetBytes(json);

                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
                    .WithRetainFlag(retainFlag)
                    .Build();

                await _mqttClient.PublishAsync(mqttMessage);
                await Task.Delay(1000); // Wait for 1 second

                Console.WriteLine($"Published message to topic: {topic}");
            }
            else
                await Log(LogLevel.TRACE, LogMessageKey.MqttNotConnected, "PublishAsync");
        }

        public async Task SubscribeAsync(string topic)
        {
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build());

            Console.WriteLine($"Subscribed to topic: {topic}");
        }

        private async Task TryConnectAsync()
        {
            if (_mqttClient != null)
            {
                await _mqttClient.ConnectAsync(_mqttClientTlsOptions);
            }
        }

        private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = e.ApplicationMessage.ConvertPayloadToString();

            Console.WriteLine($"Received message on topic {topic}: {payload}");

            // Trigger the event for other parts of your application
            if (MessageReceived != null)
            {
                await MessageReceived.Invoke(topic, payload);
            }
        }

        private async Task OnConnected(MqttClientConnectedEventArgs e)
        {
            Console.WriteLine("MQTT client connected");
            await Task.CompletedTask;
        }

        private async Task OnDisconnected(MqttClientDisconnectedEventArgs e)
        {
            Console.WriteLine($"MQTT client disconnected: {e.Reason}");

            await ConnectAsync(-1);

            await Task.CompletedTask;
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
}


// Extension method to register the service
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMqttService(this IServiceCollection services)
    {
        return services.AddSingleton<IMqttService, MqttService>();
    }
}


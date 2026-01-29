using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Protocol;
using Newtonsoft.Json;
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
        private readonly SemaphoreSlim _connectionSemaphore = new SemaphoreSlim(1, 1);

        private IMqttClient _mqttClient;
        private MqttClientOptions _mqttClientTlsOptions;

        private readonly IServiceProvider _serviceProvider;
        private readonly ConfigurationService _config;

        // Event so other parts of app can react to incoming messages
        public event Func<MqttMessageModel, Task> ProcessMessageReceived;

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
            string clientId = SystemManager.DeviceId;
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
            //await ConnectAsync(1);
        }

        /// <summary>
        /// Try to connect to the MQTT broker, retrying up to trialsCount times if necessary.
        /// Set trialsCount = -1 to try for ever.
        /// </summary>
        /// <param name="trialsCount"></param>
        /// <returns></returns>
        public async Task ConnectAsync(int trialsCount)
        {
            // Try to acquire the semaphore (non-blocking check)
            if (await _connectionSemaphore.WaitAsync(0))
            {
                try
                {
                    // Already connected, no need to retry
                    if (_mqttClient?.IsConnected == true)
                    {
                        return;
                    }

                    int x = 0;
                    while (_mqttClient?.IsConnected != true && (trialsCount == -1 || x < trialsCount))
                    {
                        Console.WriteLine(DateTime.Now.ToString() + "> > > ConnectAsync to MQTT");
                        await TryConnectAsync();

                        // Only delay if not connected and we should retry
                        if (_mqttClient?.IsConnected != true && (trialsCount == -1 || x < trialsCount - 1))
                        {
                            await Task.Delay(3000);
                        }
                        x++;
                    }
                }
                finally
                {
                    // Always release the semaphore, even if an exception occurs
                    _connectionSemaphore.Release();
                }
            }
        }

        public async Task DisconnectAsync()
        {
            // Wait for any ongoing connection attempt to complete
            await _connectionSemaphore.WaitAsync();
            try
            {
                if (_mqttClient != null)
                {
                    await _mqttClient.DisconnectAsync();
                    _mqttClient.Dispose();
                }
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public async Task PublishAsync(string topic, object message, bool retainFlag)
        {
            await ConnectAsync(3);

            if (_mqttClient.IsConnected)
            {
                var json = SystemManager.Serialize(message);
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
            try
            {
                if (_mqttClient != null)
                {
                    Console.WriteLine(DateTime.Now.ToString() + "> > > Try Connect to MQTT");
                    await _mqttClient.ConnectAsync(_mqttClientTlsOptions);
                }
            }
            catch (Exception ex)
            {
                await Log(LogLevel.ERROR, LogMessageKey.MqttNotConnected, "Cannot Connect To Mqtt Server", ex);
            }
        }

        private async Task OnConnected(MqttClientConnectedEventArgs e)
        {
            Console.WriteLine("MQTT client connected");

            // Subscribe to all topics
            await SubscribeAsync(SystemManager.GetMqttTopicPath(MqttTopics.RemoteUpdateTopic_Publish));
            await SubscribeAsync(SystemManager.GetMqttTopicPath(MqttTopics.RemoteActionTopic_Publish));

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

        private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = e.ApplicationMessage.ConvertPayloadToString();

            Console.WriteLine($"Received message on topic {topic}: {payload}");

            var mqttReceivedModel = new MqttMessageModel
            {
                Topic = topic,
                Payload = payload
            };

            Console.WriteLine($"📩 Received message on {topic}: {payload}");

            if (ProcessMessageReceived != null)
            {
                // Await all attached handlers
                foreach (var handler in ProcessMessageReceived.GetInvocationList().Cast<Func<MqttMessageModel, Task>>())
                {
                    await handler(mqttReceivedModel);
                }
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


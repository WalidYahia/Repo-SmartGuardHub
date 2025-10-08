using SmartGuardHub.Features.UserCommands;

namespace SmartGuardHub.Protocols.MQTT
{
    // Link of this solution:
    // https://chatgpt.com/share/68e4aeb3-c9bc-800e-af42-7c06b8eaacbc

    public class MqttMessageListener
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMqttService _mqttService;

        public MqttMessageListener(IServiceScopeFactory scopeFactory, IMqttService mqttService)
        {
            _scopeFactory = scopeFactory;
            _mqttService = mqttService;

            Console.WriteLine("+++++++ MQTT Command Listener initialized.");
            _mqttService.ProcessMessageReceived += HandleMqttMessageAsync;
        }

        private async Task HandleMqttMessageAsync(MqttMessageModel receivedModel)
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<UserCommandHandler>();
            await handler.HandleMqttUserCommand(receivedModel);
        }
    }
}

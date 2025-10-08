using System.Text.Json;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols;
using SmartGuardHub.Protocols.MQTT;

namespace SmartGuardHub.Features.UserCommands
{
    public class UserCommandHandler
    {
        private readonly IEnumerable<UserCommand> _userCommands;
        private readonly LoggingService _loggingService;
        private readonly IMqttService _mqttService;
        private readonly IServiceScopeFactory _scopeFactory;

        public UserCommandHandler(IEnumerable<UserCommand> userCommands,
            LoggingService loggingService,
            IMqttService mqttService,
            IServiceScopeFactory scopeFactory)
        {
            _userCommands = userCommands;
            _loggingService = loggingService;

            Console.WriteLine("++++++++++++++++++++++++++++UserCommandHandler created, subscribing to MQTT.");

            _mqttService = mqttService;
            _scopeFactory = scopeFactory;
        }

        public async Task<GeneralResponse> HandleApiUserCommand(JsonCommand jsonCommand)
        {
            if (jsonCommand == null)
            {
                return await HandleNullCommand();
            }
            else
                return await ExcuteCommand(jsonCommand);
        }

        public async Task HandleMqttUserCommand(MqttMessageModel recievedModel)
        {
            var jsonCommand = JsonSerializer.Deserialize<JsonCommand>(recievedModel.Payload);

            GeneralResponse result = null;

            if (jsonCommand == null)
            {
                result = await HandleNullCommand();
            }

            if (recievedModel.Topic.Contains(MqttTopics.RemoteActionTopic_Publish))
            {
                result = await ExcuteCommand(jsonCommand);

                result.RequestId = jsonCommand.RequestId;

                _mqttService.PublishAsync(SystemManager.GetMqttTopicPath(MqttTopics.RemoteUpdateTopic_Ack), result, retainFlag: false);
            }
            else if (recievedModel.Topic.Contains(MqttTopics.RemoteUpdateTopic_Publish))
            {

            }
        }

        private async Task<GeneralResponse> ExcuteCommand(JsonCommand jsonCommand)
        {
            UserCommand? deviceCommand = _userCommands.FirstOrDefault(c => c.jsonCommandType == jsonCommand.JsonCommandType);

            return await deviceCommand.ExecuteCommandAsync(jsonCommand);
        }

        private async Task<GeneralResponse> HandleNullCommand()
        {
            await _loggingService.LogTraceAsync(LogMessageKey.UserCommandHandler, $"HandleUserCommand - Command Is Null");

            return new GeneralResponse
            {
                State = DeviceResponseState.NoContent,
                DevicePayload = "NoContent"
            };
        }
    }
}

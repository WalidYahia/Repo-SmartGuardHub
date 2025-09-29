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

        public UserCommandHandler(IEnumerable<UserCommand> userCommands, LoggingService loggingService, IMqttService mqttService)
        {
            _userCommands = userCommands;
            _loggingService = loggingService;

            Console.WriteLine("++++++++++++++++++++++++++++UserCommandHandler created, subscribing to MQTT.");

            _mqttService = mqttService;
            _mqttService.ProcessMessageReceived += HandleMqttMessageAsync;
        }

        public async Task<GeneralResponse> HandleUserCommand(JsonCommand jsonCommand)
        {
            if (jsonCommand == null)
            {
                await _loggingService.LogTraceAsync(LogMessageKey.UserCommandHandler, $"HandleUserCommand - Command Is Null");

                return new GeneralResponse
                {
                    State = DeviceResponseState.NoContent,
                    DevicePayload = "NoContent"
                };
            }

            UserCommand? deviceCommand = _userCommands.FirstOrDefault(c => c.jsonCommandType == jsonCommand.JsonCommandType);

            return await deviceCommand.ExecuteCommandAsync(jsonCommand);
        }

        private async Task HandleMqttMessageAsync(MqttMessageModel recievedModel)
        {
            var jsonCommand = JsonSerializer.Deserialize<JsonCommand>(recievedModel.Payload);

            if (jsonCommand != null)
            {
                if (recievedModel.Topic.Contains(MqttTopics.RemoteActionTopic_Publish))
                {
                    var result = await HandleUserCommand(jsonCommand);

                    result.RequestId = jsonCommand.RequestId;

                    _mqttService.PublishAsync(SystemManager.GetMqttTopicPath(MqttTopics.RemoteUpdateTopic_Ack), result, retainFlag: false);
                }
                else if (recievedModel.Topic.Contains(MqttTopics.RemoteUpdateTopic_Publish))
                {

                }
            }
        }
    }
}

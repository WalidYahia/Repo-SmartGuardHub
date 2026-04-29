using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SensorConfiguration;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Features.UserScenarios;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols;
using SmartGuardHub.Protocols.MQTT;
using System.Data;
using System.Text.Json;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserCommands
{
    public class UserCommandHandler
    {
        private readonly IEnumerable<UserCommand> _userCommands;
        private readonly LoggingService _loggingService;
        private readonly IMqttService _mqttService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IUserScenarioRepository _scenarioRepo;
        private readonly ISensorConfigRepository _sensorConfigRepo;

        public UserCommandHandler(IEnumerable<UserCommand> userCommands,
            LoggingService loggingService,
            IMqttService mqttService,
            IServiceScopeFactory scopeFactory,
            IUserScenarioRepository userScenarioRepository,
            ISensorConfigRepository sensorConfigRepository)
        {
            _userCommands = userCommands;
            _loggingService = loggingService;

            _mqttService = mqttService;
            _scopeFactory = scopeFactory;

            _scenarioRepo = userScenarioRepository;
            _sensorConfigRepo = sensorConfigRepository;
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
            GeneralResponse result = null;

            try
            {
                if (recievedModel.Topic.Contains(MqttTopics.CloudSensorConfig.ToString()))
                {
                    await HandleCloudSensorConfigAsync(recievedModel.Payload);
                    return;
                }

                if (recievedModel.Topic.Contains(MqttTopics.CloudUserScenario.ToString()))
                {
                    await HandleCloudUserScenarioAsync(recievedModel.Payload);
                    return;
                }

                var jsonCommand = SystemManager.Deserialize<JsonCommand>(recievedModel.Payload);

                if (jsonCommand == null)
                {
                    result = await HandleNullCommand();
                }

                if (recievedModel.Topic.Contains(MqttTopics.RemoteAction.ToString()))
                {
                    switch (jsonCommand.JsonCommandType)
                    {
                        case JsonCommandType.SaveUSerScenario:
                        case JsonCommandType.DeleteUSerScenario:
                            result = await ExcuteUserScenarioCommand(jsonCommand);
                            result.RequestId = jsonCommand.RequestId;
                            break;

                        default:
                            result = await ExcuteCommand(jsonCommand);
                            result.RequestId = jsonCommand.RequestId;
                            break;
                    }

                    _mqttService.PublishAsync(SystemManager.GetMqttTopic(MqttTopics.RemoteAction_Ack), result, retainFlag: false);
                }
                else if (recievedModel.Topic.Contains(MqttTopics.RemoteUpdate.ToString()))
                {

                }

                if (result != null && jsonCommand.CommandPayload != null && result.State == DeviceResponseState.OK)
                    UpdateTopic(jsonCommand.CommandPayload.InstalledSensorId, jsonCommand.JsonCommandType);
            }
            catch (Exception ex)
            {
                result = new GeneralResponse { State = DeviceResponseState.Error, RequestId = "Failed to parse Json" };

                _mqttService.PublishAsync(SystemManager.GetMqttTopic(MqttTopics.RemoteAction_Ack), result, retainFlag: false);
            }
        }

        private async Task<GeneralResponse> ExcuteCommand(JsonCommand jsonCommand)
        {
            UserCommand? deviceCommand = _userCommands.FirstOrDefault(c => c.jsonCommandType == jsonCommand.JsonCommandType);

            return await deviceCommand.ExecuteCommandAsync(jsonCommand);
        }

        private async Task<GeneralResponse> ExcuteUserScenarioCommand(JsonCommand jsonCommand)
        {
            GeneralResponse result;

            bool saveState = false;

            if (jsonCommand.CommandPayload != null && jsonCommand.CommandPayload.UserScenario != null)
            {
                if (jsonCommand.JsonCommandType == JsonCommandType.SaveUSerScenario)
                {
                    saveState = await _scenarioRepo.SaveAsync(jsonCommand.CommandPayload.UserScenario);
                }
                else if (jsonCommand.JsonCommandType == JsonCommandType.DeleteUSerScenario)
                {
                    saveState = await _scenarioRepo.DeleteAsync(jsonCommand.CommandPayload.UserScenario.Id);
                }
                
                if (saveState)
                {
                    result = new GeneralResponse { State = DeviceResponseState.OK };
                    
                    var scenarios = await _scenarioRepo.GetAllAsync();

                    _mqttService.PublishAsync(SystemManager.GetMqttTopic(MqttTopics.UserScenario), scenarios, retainFlag: true);
                }
                else
                    result = new GeneralResponse { State = DeviceResponseState.Error };
            }
            else
            {
                result = new GeneralResponse { State = DeviceResponseState.NoContent };
            }

            return result;
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

        private async Task HandleCloudSensorConfigAsync(string payload)
        {
            var configs = SystemManager.Deserialize<List<SensorConfig>>(payload);

            if (configs == null)
            {
                await _loggingService.LogTraceAsync(LogMessageKey.UserCommandHandler, "CloudSensorConfig - payload is null or invalid");
                return;
            }

            var saved = await _sensorConfigRepo.SaveAllAsync(configs);

            if (!saved)
                await _loggingService.LogErrorAsync(LogMessageKey.UserCommandHandler, "CloudSensorConfig - failed to save config", null);
        }

        private async Task HandleCloudUserScenarioAsync(string payload)
        {
            var scenarios = SystemManager.Deserialize<List<UserScenario>>(payload);

            if (scenarios == null)
            {
                await _loggingService.LogTraceAsync(LogMessageKey.UserCommandHandler, "CloudUserScenario - payload is null or invalid");
                return;
            }

            var saved = await _scenarioRepo.SaveAllAsync(scenarios);

            if (!saved)
                await _loggingService.LogErrorAsync(LogMessageKey.UserCommandHandler, "CloudUserScenario - failed to save scenarios", null);
        }

        private async Task UpdateTopic(string sensorId, JsonCommandType jsonCommandType)
        {
            switch (jsonCommandType)
            {
                case JsonCommandType.TurnOn:
                    //_mqttService.PublishAsync(SystemManager.GetMqttTopicPath(MqttTopics.DeviceDataTopic) + $"/{sensorId}", new UnitMqttPayload { SensorId = sensorId, Value = SwitchOutletStatus.On }, retainFlag: true);
                    break;

                case JsonCommandType.TurnOff:
                    //_mqttService.PublishAsync(SystemManager.GetMqttTopicPath(MqttTopics.DeviceDataTopic) + $"/{sensorId}", new UnitMqttPayload { SensorId = sensorId, Value = SwitchOutletStatus.Off }, retainFlag: true);
                    break;
            }
        }
    }
}

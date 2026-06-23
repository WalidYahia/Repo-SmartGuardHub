using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SensorConfiguration;
using SmartGuardHub.Features.UserScenarios;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols.MQTT;
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
            _userCommands     = userCommands;
            _loggingService   = loggingService;
            _mqttService      = mqttService;
            _scopeFactory     = scopeFactory;
            _scenarioRepo     = userScenarioRepository;
            _sensorConfigRepo = sensorConfigRepository;
        }

        public async Task<GeneralResponse> HandleApiUserCommand(JsonCommand jsonCommand)
        {
            if (jsonCommand == null)
                return await HandleNullCommand();

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
                    return;
                }

                jsonCommand.Source = ConfigSource.Cloud;

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

                    _mqttService.PublishAsync(SystemManager.GetMqttTopic(MqttTopics.RemoteAction_Ack), result, retainFlag: false, qos: MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce);
                }
                else if (recievedModel.Topic.Contains(MqttTopics.RemoteUpdate.ToString()))
                {
                    // reserved
                }
            }
            catch (Exception ex)
            {
                result = new GeneralResponse { State = DeviceResponseState.Error, RequestId = "Failed to parse Json" };
                _mqttService.PublishAsync(SystemManager.GetMqttTopic(MqttTopics.RemoteAction_Ack), result, retainFlag: false, qos: MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce);
            }
        }

        private async Task<GeneralResponse> ExcuteCommand(JsonCommand jsonCommand)
        {
            var deviceCommand = _userCommands.FirstOrDefault(c => c.jsonCommandType == jsonCommand.JsonCommandType);
            return await deviceCommand.ExecuteCommandAsync(jsonCommand);
        }

        private async Task<GeneralResponse> ExcuteUserScenarioCommand(JsonCommand jsonCommand)
        {
            bool saveState = false;

            if (jsonCommand.CommandPayload != null && jsonCommand.CommandPayload.UserScenario != null)
            {
                if (jsonCommand.JsonCommandType == JsonCommandType.SaveUSerScenario)
                    saveState = await _scenarioRepo.SaveAsync(jsonCommand.CommandPayload.UserScenario);
                else if (jsonCommand.JsonCommandType == JsonCommandType.DeleteUSerScenario)
                    saveState = await _scenarioRepo.DeleteAsync(jsonCommand.CommandPayload.UserScenario.Id);

                if (saveState)
                {
                    var scenarios = await _scenarioRepo.GetAllAsync();
                    //_mqttService.PublishAsync(SystemManager.GetMqttTopic(MqttTopics.UserScenario), scenarios, retainFlag: true);
                    return new GeneralResponse { State = DeviceResponseState.OK };
                }

                return new GeneralResponse { State = DeviceResponseState.Error };
            }

            return new GeneralResponse { State = DeviceResponseState.NoContent };
        }

        private async Task<GeneralResponse> HandleNullCommand()
        {
            await _loggingService.LogTraceAsync(LogMessageKey.UserCommandHandler, "HandleUserCommand - Command Is Null");
            return new GeneralResponse { State = DeviceResponseState.NoContent, DevicePayload = "NoContent" };
        }

        private async Task HandleCloudSensorConfigAsync(string payload)
        {
            var envelope = SystemManager.Deserialize<SensorConfigEnvelope>(payload);

            if (envelope?.Sensors == null)
            {
                await _loggingService.LogTraceAsync(LogMessageKey.UserCommandHandler, "CloudSensorConfig - payload is null or invalid");
                return;
            }

            var current = await _sensorConfigRepo.GetVersionInfoAsync(ConfigType.Sensor);

            if (current != null &&
                (envelope.ConfigVersion == current.Value.Version || envelope.UpdateTime <= current.Value.UpdateTime))
            {
                await _loggingService.LogTraceAsync(LogMessageKey.UserCommandHandler, "CloudSensorConfig - skipped (version matches or device config is more recent)");
                return;
            }

            var saved = await _sensorConfigRepo.SaveAllAsync(envelope.Sensors, ConfigSource.Cloud, envelope.ConfigVersion);

            if (saved)
            {
                using var scope = _scopeFactory.CreateScope();
                var deviceService = scope.ServiceProvider.GetRequiredService<DeviceService>();
                await deviceService.RefreshSensors(publishToCloud: false);
            }
            else
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
    }
}

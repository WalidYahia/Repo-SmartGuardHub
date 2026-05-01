using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SensorConfiguration;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Features.UserScenarios;
using SmartGuardHub.Infrastructure;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserCommands
{
    public abstract class UserCommand
    {
        protected readonly IEnumerable<ISystemSensor> _systemSensors;
        protected readonly LoggingService _loggingService;
        protected readonly DeviceService _deviceService;

        public JsonCommandType jsonCommandType { get; set; }

        protected UserCommand(IEnumerable<ISystemSensor> systemSensors, LoggingService loggingService, DeviceService deviceService)
        {
            _systemSensors = systemSensors;
            _loggingService = loggingService;
            _deviceService = deviceService;
        }

        protected abstract Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand);

        public async Task<GeneralResponse> ExecuteCommandAsync(JsonCommand jsonCommand)
        {
            if (await RequestIsValid(jsonCommand))
                return await ExecuteAsync(jsonCommand);

            return new GeneralResponse
            {
                State = DeviceResponseState.DeviceDataIsRequired,
                DevicePayload = "Device data is required"
            };
        }

        protected virtual async Task<bool> RequestIsValid(JsonCommand jsonCommand) =>
            jsonCommand.CommandPayload != null;

        protected SensorConfig? LoadInstalledSensor(string? installedDeviceId) =>
            SystemManager.InstalledSensors.FirstOrDefault(d => d.Id == installedDeviceId);

        protected ISystemSensor? LoadSystemSensor(int sensorType) =>
            _systemSensors.FirstOrDefault(s => s.SensorType == (SensorType)sensorType);

        protected async Task<GeneralResponse> GetInfoResponse(SensorConfig sensor, ISystemSensor systemSensor)
        {
            var command = systemSensor.GetInfoCommand(sensor.UnitId);
            var jsonString = SystemManager.Serialize(command);
            return await systemSensor.SendCommandAsync(sensor.Url + sensor.InfoPath, jsonString);
        }
    }


    public class JsonCommand
    {
        public string? RequestId { get; set; }
        public JsonCommandType JsonCommandType { get; set; }
        public JsonCommandPayload? CommandPayload { get; set; }
    }

    public class JsonCommandPayload
    {
        public string? UnitId { get; set; }
        public SwitchOutlet SwitchNo { get; set; }
        public int? Address { get; set; }
        public int? Port { get; set; }
        public string? InstalledSensorId { get; set; }
        public SensorType SensorType { get; set; }
        public string? Name { get; set; }
        public int InchingTimeInMs { get; set; }
        public UserScenario? UserScenario { get; set; }
    }

    public class UnitMqttPayload
    {
        public string SensorId { get; set; }
        public object Value { get; set; }
    }
}

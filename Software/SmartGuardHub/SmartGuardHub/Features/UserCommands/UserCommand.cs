using Newtonsoft.Json;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserCommands
{
    public abstract class UserCommand
    {
        protected readonly IEnumerable<ISystemUnit> _systemUnits;
        protected readonly LoggingService _loggingService;
        protected readonly DeviceService _deviceService;

        public JsonCommandType jsonCommandType { get; set; }
        protected UserCommand(IEnumerable<ISystemUnit> systemUnits, LoggingService loggingService, DeviceService deviceService)
        {
            _systemUnits = systemUnits;
            _loggingService = loggingService;
            _deviceService = deviceService;
        }

        protected abstract Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand);

        public async Task<GeneralResponse> ExecuteCommandAsync(JsonCommand jsonCommand) 
        {
            if (await RequestIsValid(jsonCommand))
            {
                return await ExecuteAsync(jsonCommand);
            }
            else
            {
                return new GeneralResponse
                {
                    State = DeviceResponseState.DeviceDataIsRequired,
                    DevicePayload = "Device data is required"
                };
            }
        }

        protected virtual async Task<bool> RequestIsValid(JsonCommand jsonCommand)
        {
            return jsonCommand.CommandPayload == null ? false : true;
        }

        protected async Task<SensorDTO> LoadInstalledSensor(string installedDeviceId)
        {
            var device = SystemManager.InstalledSensors.FirstOrDefault(d => d.SensorId == installedDeviceId);

            return device;
        }
        protected async Task<ISystemUnit> LoadSystemUnit(UnitType deviceType)
        {
            var systemDevice = _systemUnits.FirstOrDefault(d => d.DeviceType == deviceType);

            return systemDevice;
        }
        protected async Task<GeneralResponse> GetInfoResponse(string installedDeviceUrl, ISystemUnit systemDevice, JsonCommandPayload jsonCommandPayload)
        {
            var command = systemDevice.GetInfoCommand(jsonCommandPayload.UnitId);
            string jsonString = SystemManager.Serialize(command);
            return await systemDevice.SendCommandAsync(installedDeviceUrl + systemDevice.InfoPath, jsonString);
        }
    }


    public class JsonCommand
    {
        /// <summary>
        /// A unique identifier for the request
        /// For mobile app mqtt-subscribe (each mobile app process only recieved ack of its actions).
        /// </summary>
        public string? RequestId { get; set; }

        public JsonCommandType JsonCommandType { get; set; }

        public JsonCommandPayload? CommandPayload { get; set; }
    }

    public class JsonCommandPayload
    {
        public string? UnitId { get; set; }
        public SwitchOutlet SwitchNo { get; set; }
        public string? InstalledSensorId { get; set; }

        public UnitType DeviceType { get; set; } = UnitType.Unknown;
        public string? Name { get; set; }
        public int InchingTimeInMs { get; set; }
    }

    public class UnitMqttPayload
    {
        public string SensorId { get; set; }

        public object Value { get; set; }
    }
}

using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SensorConfiguration;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserCommands
{
    public class CreateDeviceCommand : UserCommand
    {
        public CreateDeviceCommand(IEnumerable<ISystemUnit> systemUnits, LoggingService loggingService, DeviceService deviceService)
            : base(systemUnits, loggingService, deviceService)
        {
            jsonCommandType = Enums.JsonCommandType.CreateDevice;
        }

        protected override async Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand)
        {
            var payload = jsonCommand.CommandPayload;

            if (string.IsNullOrEmpty(payload.UnitId) || payload.UnitType == UnitType.Unknown || string.IsNullOrEmpty(payload.Name))
                return new GeneralResponse { State = DeviceResponseState.DeviceDataIsRequired, DevicePayload = "Device data is required" };

            if (SystemManager.InstalledSensors.Any(d => d.UnitId == payload.UnitId && d.SwitchNo == (int)payload.SwitchNo))
                return new GeneralResponse { State = DeviceResponseState.DeviceAlreadyRegistered, DevicePayload = "Device already registered" };

            if (SystemManager.InstalledSensors.Any(d => d.DisplayName == payload.Name))
                return new GeneralResponse { State = DeviceResponseState.DeviceNameAlreadyRegistered, DevicePayload = "Device with the same name is already registered" };

            var systemDevice = _systemUnits.FirstOrDefault(d => d.UnitType == payload.UnitType);

            var id = SensorConfig.ComputeId(
                deviceId:   SystemManager.DeviceId,
                sensorType: systemDevice.SensorType,
                unitType:   payload.UnitType,
                unitId:     payload.UnitId ?? "",
                switchNo:   (SwitchNo)(int)payload.SwitchNo,
                address:    payload.Address,
                port:       payload.Port);

            var sensor = new SensorConfig
            {
                Id          = id,
                DeviceId    = SystemManager.DeviceId,
                SwitchNo    = (int)payload.SwitchNo,
                UnitId      = payload.UnitId,
                DisplayName = payload.Name,
                UnitType    = payload.UnitType,
                SensorType  = (int)systemDevice.SensorType,
                Url         = systemDevice.BaseUrl + payload.UnitId + ":" + systemDevice.PortNo,
                Protocol    = (int)systemDevice.ProtocolType,
                IsActive    = true,
                InstalledAt = DateTime.UtcNow,
                Address = payload.Address,
                Port = payload.Port,
                
                
            };

            var created = await _deviceService.CreateDeviceAsync(sensor);

            return new GeneralResponse { State = DeviceResponseState.OK, DevicePayload = created };
        }
    }
}

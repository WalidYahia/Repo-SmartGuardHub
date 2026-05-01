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
        private readonly ISensorUnitDefinitionRepository _unitDefRepo;

        public CreateDeviceCommand(
            IEnumerable<ISystemSensor> systemSensors,
            LoggingService loggingService,
            DeviceService deviceService,
            ISensorUnitDefinitionRepository unitDefRepo)
            : base(systemSensors, loggingService, deviceService)
        {
            _unitDefRepo = unitDefRepo;
            jsonCommandType = Enums.JsonCommandType.CreateDevice;
        }

        protected override async Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand)
        {
            var payload = jsonCommand.CommandPayload;

            if (string.IsNullOrEmpty(payload.UnitId) || payload.SensorType == SensorType.Unknown || string.IsNullOrEmpty(payload.Name))
                return new GeneralResponse { State = DeviceResponseState.DeviceDataIsRequired, DevicePayload = "Device data is required" };

            var unitDef = _unitDefRepo.GetBySensorType(payload.SensorType);
            if (unitDef == null)
                return new GeneralResponse { State = DeviceResponseState.NotFound, DevicePayload = $"Unknown sensor type: {payload.SensorType}" };

            if (SystemManager.InstalledSensors.Any(d => d.UnitId == payload.UnitId && d.SwitchNo == (int)payload.SwitchNo))
                return new GeneralResponse { State = DeviceResponseState.DeviceAlreadyRegistered, DevicePayload = "Device already registered" };

            if (SystemManager.InstalledSensors.Any(d => d.DisplayName == payload.Name))
                return new GeneralResponse { State = DeviceResponseState.DeviceNameAlreadyRegistered, DevicePayload = "Device with the same name is already registered" };

            var id = SensorConfig.ComputeId(
                deviceId:   SystemManager.DeviceId,
                sensorType: payload.SensorType,
                unitId:     payload.UnitId,
                switchNo:   (SwitchNo)(int)payload.SwitchNo,
                address:    payload.Address,
                port:       payload.Port);

            var sensor = new SensorConfig
            {
                Id          = id,
                DeviceId    = SystemManager.DeviceId,
                SwitchNo    = (int)payload.SwitchNo,
                UnitId      = payload.UnitId,
                Address     = payload.Address,
                Port        = payload.Port,
                DisplayName = payload.Name,
                Url         = unitDef.BaseUrl + payload.UnitId + ":" + unitDef.PortNo,
                SensorType  = (int)unitDef.SensorType,
                Protocol    = unitDef.ProtocolType,
                DataPath    = unitDef.DataPath,
                InfoPath    = unitDef.InfoPath,
                InchingPath = unitDef.InchingPath,
                IsActive    = true,
                InstalledAt = DateTime.UtcNow,
            };

            var created = await _deviceService.CreateDeviceAsync(sensor);
            return new GeneralResponse { State = DeviceResponseState.OK, DevicePayload = created };
        }
    }
}

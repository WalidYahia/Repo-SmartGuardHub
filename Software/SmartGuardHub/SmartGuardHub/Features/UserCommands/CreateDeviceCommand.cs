using Microsoft.EntityFrameworkCore;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;

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
            if (string.IsNullOrEmpty(jsonCommand.CommandPayload.UnitId) || string.IsNullOrEmpty(jsonCommand.CommandPayload.Name))
            {
                return new GeneralResponse
                {
                    State = DeviceResponseState.DeviceDataIsRequired,
                    DevicePayload = "Device data is required"
                };
            }

            SensorDTO deviceCheck1 = SystemManager.InstalledSensors.FirstOrDefault(d => d.UnitId == jsonCommand.CommandPayload.UnitId && d.SwitchNo == jsonCommand.CommandPayload.SwitchNo);
            if (deviceCheck1 != null)
            {
                return new GeneralResponse
                {
                    State = DeviceResponseState.DeviceAlreadyRegistered,
                    DevicePayload = "Device already registered"
                };
            }

            SensorDTO deviceCheck2 = SystemManager.InstalledSensors.FirstOrDefault(d => d.Name == jsonCommand.CommandPayload.Name);
            if (deviceCheck2 != null)
            {
                return new GeneralResponse
                {
                    State = DeviceResponseState.DeviceNameAlreadyRegistered,
                    DevicePayload = "Device with the same name is already registered"
                };
            }

            try
            {
                var systemDevice = _systemUnits.FirstOrDefault(d => d.DeviceType == jsonCommand.CommandPayload.DeviceType);

                SensorDTO sensorDTO = new SensorDTO
                {
                    SensorId = jsonCommand.CommandPayload.UnitId + "_" + ((int)jsonCommand.CommandPayload.SwitchNo).ToString(),
                    UnitId = jsonCommand.CommandPayload.UnitId,
                    SwitchNo = jsonCommand.CommandPayload.SwitchNo,
                    Name = jsonCommand.CommandPayload.Name,
                    Type = jsonCommand.CommandPayload.DeviceType,
                    Url = systemDevice.BaseUrl + jsonCommand.CommandPayload.UnitId + ":" + systemDevice.PortNo,
                    Protocol = systemDevice.ProtocolType,
                    IsOnline = false,
                    CreatedAt = SystemManager.TimeNow(),
                };

                var createdDevice = await _deviceService.CreateDeviceAsync(sensorDTO);

                await _deviceService.RefreshDevices();

                return new GeneralResponse
                {
                    State = DeviceResponseState.OK,
                    DevicePayload = createdDevice
                };
            }
            catch (DbUpdateException ex)
            {
                await _loggingService.LogErrorAsync(LogMessageKey.DevicesConflict, $"ConflictError - CreateDevice", ex);

                return new GeneralResponse
                {
                    State = DeviceResponseState.Conflict,
                    DevicePayload = ex.Message
                };
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;

namespace SmartGuardHub.Features.UserCommands
{
    public class RenameDeviceCommand : UserCommand
    {
        public RenameDeviceCommand(IEnumerable<ISystemUnit> systemUnits, LoggingService loggingService, DeviceService deviceService)
            : base(systemUnits, loggingService, deviceService)
        {
            jsonCommandType = Enums.JsonCommandType.RenameDevice;
        }

        protected override async Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand)
        {            
            if (jsonCommand.CommandPayload == null)
            {
                return new GeneralResponse
                {
                    State = DeviceResponseState.DeviceDataIsRequired,
                    DevicePayload = "Device data is required"
                };
            }

            if (string.IsNullOrEmpty(jsonCommand.CommandPayload.Name.Trim()))
            {
                return new GeneralResponse
                {
                    State = DeviceResponseState.DeviceDataIsRequired,
                    DevicePayload = "Device data is required"
                };
            }

            try
            {
                var installedDevice = await LoadInstalledSensor(jsonCommand.CommandPayload.InstalledSensorId);

                if (installedDevice != null)
                {
                    installedDevice.Name = jsonCommand.CommandPayload.Name;

                    var device = await _deviceService.UpdateDeviceAsync(installedDevice);

                    await _deviceService.RefreshDevices();

                    return new GeneralResponse
                    {
                        State = DeviceResponseState.OK,
                        DevicePayload = device
                    };
                }
                else
                {
                    await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"On - Device with ID {jsonCommand.CommandPayload.UnitId}-{(int)jsonCommand.CommandPayload.SwitchNo} not found.");

                    return new GeneralResponse
                    {
                        State = DeviceResponseState.NotFound,
                        DevicePayload = "Device not found"
                    };
                }
            }
            catch (DbUpdateException ex)
            {
                await _loggingService.LogErrorAsync(LogMessageKey.DevicesConflict, $"ConflictError - RenameDevice", ex);

                return new GeneralResponse
                {
                    State = DeviceResponseState.Conflict,
                    DevicePayload = ex.Message
                };
            }      
        }
    }
}

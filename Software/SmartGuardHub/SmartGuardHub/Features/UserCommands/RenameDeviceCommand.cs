using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;

namespace SmartGuardHub.Features.UserCommands
{
    public class RenameDeviceCommand : UserCommand
    {
        public RenameDeviceCommand(IEnumerable<ISystemSensor> systemSensors, LoggingService loggingService, DeviceService deviceService)
            : base(systemSensors, loggingService, deviceService)
        {
            jsonCommandType = Enums.JsonCommandType.RenameDevice;
        }

        protected override async Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand)
        {
            if (string.IsNullOrWhiteSpace(jsonCommand.CommandPayload?.Name))
                return new GeneralResponse { State = DeviceResponseState.DeviceDataIsRequired, DevicePayload = "Device data is required" };

            var sensor = LoadInstalledSensor(jsonCommand.CommandPayload.InstalledSensorId);

            if (sensor == null)
            {
                await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"Rename - Sensor {jsonCommand.CommandPayload.InstalledSensorId} not found.");
                return new GeneralResponse { State = DeviceResponseState.NotFound, DevicePayload = "Device not found" };
            }

            sensor.DisplayName = jsonCommand.CommandPayload.Name;

            var updated = await _deviceService.UpdateDeviceAsync(sensor);
            return new GeneralResponse { State = DeviceResponseState.OK, DevicePayload = updated };
        }
    }
}

using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;

namespace SmartGuardHub.Features.UserCommands
{
    public class GetInfoCommand : UserCommand
    {
        public GetInfoCommand(IEnumerable<ISystemUnit> systemUnits, LoggingService loggingService, DeviceService deviceService)
            : base(systemUnits, loggingService, deviceService)
        {
            jsonCommandType = Enums.JsonCommandType.GetInfo;
        }

        protected override async Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand)
        {
            var sensor = LoadInstalledSensor(jsonCommand.CommandPayload.InstalledSensorId);

            if (sensor != null)
            {
                var systemDevice = await LoadSystemUnit(sensor.UnitType);
                return await GetInfoResponse(sensor.Url, systemDevice, jsonCommand.CommandPayload);
            }

            await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"GetInfo - Sensor {jsonCommand.CommandPayload.InstalledSensorId} not found.");
            return new GeneralResponse { State = DeviceResponseState.NotFound, DevicePayload = "Device not found" };
        }
    }
}

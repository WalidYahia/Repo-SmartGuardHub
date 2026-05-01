using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;

namespace SmartGuardHub.Features.UserCommands
{
    public class GetInfoCommand : UserCommand
    {
        public GetInfoCommand(IEnumerable<ISystemSensor> systemSensors, LoggingService loggingService, DeviceService deviceService)
            : base(systemSensors, loggingService, deviceService)
        {
            jsonCommandType = Enums.JsonCommandType.GetInfo;
        }

        protected override async Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand)
        {
            var sensor = LoadInstalledSensor(jsonCommand.CommandPayload.InstalledSensorId);

            if (sensor != null)
            {
                var systemSensor = LoadSystemSensor(sensor.SensorType);
                return await GetInfoResponse(sensor, systemSensor);
            }

            await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"GetInfo - Sensor {jsonCommand.CommandPayload.InstalledSensorId} not found.");
            return new GeneralResponse { State = DeviceResponseState.NotFound, DevicePayload = "Device not found" };
        }
    }
}

using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserCommands
{
    public class InchingOffCommand : UserCommand
    {
        public InchingOffCommand(IEnumerable<ISystemSensor> systemSensors, LoggingService loggingService, DeviceService deviceService)
            : base(systemSensors, loggingService, deviceService)
        {
            jsonCommandType = Enums.JsonCommandType.InchingOff;
        }

        protected override async Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand)
        {
            var sensor = LoadInstalledSensor(jsonCommand.CommandPayload.InstalledSensorId);

            if (sensor != null)
            {
                var systemSensor = LoadSystemSensor(sensor.SensorType);
                var infoResponse = await GetInfoResponse(sensor, systemSensor);
                var inchingCommand = systemSensor.GetOffInchingCommand(
                    sensor.UnitId, (SwitchOutlet)sensor.SwitchNo,
                    (infoResponse.DevicePayload as SonoffMiniRResponsePayload).Data.Pulses);

                var result = await systemSensor.SendCommandAsync(sensor.Url + sensor.InchingPath, SystemManager.Serialize(inchingCommand));

                if (result.State == DeviceResponseState.OK)
                {
                    sensor.IsInInchingMode      = false;
                    sensor.InchingModeWidthInMs = 0;
                    sensor.LastSeen             = DateTime.Now;
                    result.DevicePayload        = sensor;

                    await _deviceService.UpdateDeviceAsync(sensor);
                }

                return result;
            }

            await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"InchingOff - Sensor {jsonCommand.CommandPayload.InstalledSensorId} not found.");
            return new GeneralResponse { State = DeviceResponseState.NotFound, DevicePayload = "Device not found" };
        }
    }
}

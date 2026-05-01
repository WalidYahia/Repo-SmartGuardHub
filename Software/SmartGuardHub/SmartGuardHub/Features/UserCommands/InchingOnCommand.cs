using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserCommands
{
    public class InchingOnCommand : UserCommand
    {
        public InchingOnCommand(IEnumerable<ISystemSensor> systemSensors, LoggingService loggingService, DeviceService deviceService)
            : base(systemSensors, loggingService, deviceService)
        {
            jsonCommandType = Enums.JsonCommandType.InchingOn;
        }

        protected override async Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand)
        {
            if (jsonCommand.CommandPayload.InchingTimeInMs < 1000)
                return new GeneralResponse { State = DeviceResponseState.InchingIntervalValidationError, DevicePayload = "Insert value > 1000" };

            var sensor = LoadInstalledSensor(jsonCommand.CommandPayload.InstalledSensorId);

            if (sensor != null)
            {
                var systemSensor = LoadSystemSensor(sensor.SensorType);
                var infoResponse = await GetInfoResponse(sensor, systemSensor);
                var inchingCommand = systemSensor.GetOnInchingCommand(
                    sensor.UnitId, (SwitchOutlet)sensor.SwitchNo,
                    jsonCommand.CommandPayload.InchingTimeInMs,
                    (infoResponse.DevicePayload as SonoffMiniRResponsePayload).Data.Pulses);

                var result = await systemSensor.SendCommandAsync(sensor.Url + sensor.InchingPath, SystemManager.Serialize(inchingCommand));

                if (result.State == DeviceResponseState.OK)
                {
                    sensor.IsInInchingMode      = true;
                    sensor.InchingModeWidthInMs = jsonCommand.CommandPayload.InchingTimeInMs;
                    sensor.LastSeen             = DateTime.Now;
                    result.DevicePayload        = sensor;

                    await _deviceService.UpdateDeviceAsync(sensor);
                }

                return result;
            }

            await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"InchingOn - Sensor {jsonCommand.CommandPayload.InstalledSensorId} not found.");
            return new GeneralResponse { State = DeviceResponseState.NotFound, DevicePayload = "Device not found" };
        }
    }
}

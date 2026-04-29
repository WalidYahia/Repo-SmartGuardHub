using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserCommands
{
    public class InchingOnCommand : UserCommand
    {
        public InchingOnCommand(IEnumerable<ISystemUnit> systemUnits, LoggingService loggingService, DeviceService deviceService)
            : base(systemUnits, loggingService, deviceService)
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
                var systemDevice = await LoadSystemUnit(sensor.UnitType);
                var infoResponse = await GetInfoResponse(sensor.Url, systemDevice, jsonCommand.CommandPayload);
                var inchingCommand = systemDevice.GetOnInchingCommand(sensor.UnitId, (SwitchOutlet)sensor.SwitchNo,
                    jsonCommand.CommandPayload.InchingTimeInMs,
                    (infoResponse.DevicePayload as SonoffMiniRResponsePayload).Data.Pulses);

                var result = await systemDevice.SendCommandAsync(sensor.Url + systemDevice.InchingPath, SystemManager.Serialize(inchingCommand));

                if (result.State == DeviceResponseState.OK)
                {
                    sensor.IsInInchingMode    = true;
                    sensor.InchingModeWidthInMs = jsonCommand.CommandPayload.InchingTimeInMs;
                    sensor.LastSeen           = DateTime.Now;

                    result.DevicePayload = sensor;

                    _deviceService.UpdateDeviceAsync(sensor);
                    _deviceService.RefreshDevices();
                }

                return result;
            }

            await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"InchingOn - Sensor {jsonCommand.CommandPayload.InstalledSensorId} not found.");
            return new GeneralResponse { State = DeviceResponseState.NotFound, DevicePayload = "Device not found" };
        }
    }
}

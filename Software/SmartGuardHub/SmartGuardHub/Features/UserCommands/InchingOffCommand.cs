using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserCommands
{
    public class InchingOffCommand : UserCommand
    {
        public InchingOffCommand(IEnumerable<ISystemUnit> systemUnits, LoggingService loggingService, DeviceService deviceService)
            : base(systemUnits, loggingService, deviceService)
        {
            jsonCommandType = Enums.JsonCommandType.InchingOff;
        }

        protected override async Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand)
        {
            var sensor = LoadInstalledSensor(jsonCommand.CommandPayload.InstalledSensorId);

            if (sensor != null)
            {
                var systemDevice = await LoadSystemUnit(sensor.UnitType);
                var infoResponse = await GetInfoResponse(sensor.Url, systemDevice, jsonCommand.CommandPayload);
                var inchingCommand = systemDevice.GetOffInchingCommand(sensor.UnitId, (SwitchOutlet)sensor.SwitchNo,
                    (infoResponse.DevicePayload as SonoffMiniRResponsePayload).Data.Pulses);

                var result = await systemDevice.SendCommandAsync(sensor.Url + systemDevice.InchingPath, SystemManager.Serialize(inchingCommand));

                if (result.State == DeviceResponseState.OK)
                {
                    sensor.IsInInchingMode    = false;
                    sensor.InchingModeWidthInMs = 0;
                    sensor.LastSeen           = DateTime.Now;

                    result.DevicePayload = sensor;

                    _deviceService.UpdateDeviceAsync(sensor);
                    _deviceService.RefreshDevices();
                }

                return result;
            }

            await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"InchingOff - Sensor {jsonCommand.CommandPayload.InstalledSensorId} not found.");
            return new GeneralResponse { State = DeviceResponseState.NotFound, DevicePayload = "Device not found" };
        }
    }
}

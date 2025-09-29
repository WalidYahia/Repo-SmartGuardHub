using Newtonsoft.Json;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;

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
            {
                return new GeneralResponse
                {
                    State = DeviceResponseState.InchingIntervalValidationError,
                    DevicePayload = "Insert value > 1000"
                };
            }

            var installedDevice = await LoadInstalledSensor(jsonCommand.CommandPayload.InstalledSensorId);

            if (installedDevice != null)
            {
                var systemDevice = await LoadSystemUnit(installedDevice.Type);

                GeneralResponse infoResponse = await GetInfoResponse(installedDevice.Url, systemDevice, jsonCommand.CommandPayload);

                var inchingCommand = systemDevice.GetOnInchingCommand(installedDevice.UnitId, installedDevice.SwitchNo, jsonCommand.CommandPayload.InchingTimeInMs, (infoResponse.DevicePayload as SonoffMiniRResponsePayload).Data.Pulses);

                string jsonString = JsonConvert.SerializeObject(inchingCommand);

                GeneralResponse result = await systemDevice.SendCommandAsync(installedDevice.Url + systemDevice.InchingPath, jsonString);

                if (result.State == DeviceResponseState.OK)
                {
                    installedDevice.IsInInchingMode = true;
                    installedDevice.InchingModeWidthInMs = jsonCommand.CommandPayload.InchingTimeInMs;

                    result.DevicePayload = installedDevice;

                    _deviceService.UpdateDeviceAsync(installedDevice);
                    _deviceService.RefreshDevices();
                }

                return result;
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
    }
}

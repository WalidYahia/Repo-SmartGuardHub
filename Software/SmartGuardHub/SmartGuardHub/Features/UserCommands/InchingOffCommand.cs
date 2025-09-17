using Newtonsoft.Json;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;

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
            var installedDevice = await LoadInstalledSensor(jsonCommand.InstalledSensorId);

            if (installedDevice != null)
            {
                var systemDevice = await LoadSystemUnit(installedDevice.Type);

                GeneralResponse infoResponse = await GetInfoResponse(installedDevice.Url, systemDevice, jsonCommand.CommandPayload);

                var inchingCommand = systemDevice.GetOffInchingCommand(installedDevice.UnitId, installedDevice.SwitchNo, (infoResponse.DevicePayload as SonoffMiniRResponsePayload).Data.Pulses);

                string jsonString = JsonConvert.SerializeObject(inchingCommand);

                GeneralResponse result = await systemDevice.SendCommandAsync(installedDevice.Url + systemDevice.InchingPath, jsonString);

                if (result.State == DeviceResponseState.OK)
                {
                    installedDevice.IsInInchingMode = false;
                    installedDevice.InchingModeWidthInMs = 0;

                    await _deviceService.UpdateDeviceAsync(installedDevice);
                    await _deviceService.RefreshDevices();
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

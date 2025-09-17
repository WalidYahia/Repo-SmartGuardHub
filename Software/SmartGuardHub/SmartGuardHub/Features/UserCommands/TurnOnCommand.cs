using Newtonsoft.Json;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserCommands
{
    public class TurnOnCommand : UserCommand
    {
        public TurnOnCommand(IEnumerable<ISystemUnit> systemUnits, LoggingService loggingService, DeviceService deviceService)
            : base(systemUnits, loggingService, deviceService)
        {
            jsonCommandType = Enums.JsonCommandType.TurnOn;
        }

        protected override async Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand)
        {
            var installedDevice = await LoadInstalledSensor(jsonCommand.InstalledSensorId);

            if (installedDevice != null)
            {
                var systemDevice = await LoadSystemUnit(installedDevice.Type);

                var command = systemDevice.GetOnCommand(installedDevice.UnitId, installedDevice.SwitchNo);

                string jsonString = JsonConvert.SerializeObject(command);

                GeneralResponse result = await systemDevice.SendCommandAsync(installedDevice.Url + systemDevice.DataPath, jsonString);

                if (result.State == DeviceResponseState.OK)
                {
                    installedDevice.LatestValue = (int)SwitchOutletStatus.On;

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

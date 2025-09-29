using Newtonsoft.Json;
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
            var installedDevice = await LoadInstalledSensor(jsonCommand.CommandPayload.InstalledSensorId);

            if (installedDevice != null)
            {
                var systemDevice = await LoadSystemUnit(installedDevice.Type);

                GeneralResponse infoResponse = await GetInfoResponse(installedDevice.Url, systemDevice, jsonCommand.CommandPayload);

                return infoResponse;
            }
            else
            {
                await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"On - Device with ID {jsonCommand.CommandPayload.InstalledSensorId} not found.");

                return new GeneralResponse
                {
                    State = DeviceResponseState.NotFound,
                    DevicePayload = "Device not found"
                };
            }
        }
    }
}

using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserCommands
{
    public class LoadAllUnitsCommand : UserCommand
    {
        public LoadAllUnitsCommand(IEnumerable<ISystemUnit> systemUnits, LoggingService loggingService, DeviceService deviceService)
           : base(systemUnits, loggingService, deviceService)
        {
            jsonCommandType = Enums.JsonCommandType.LoaddAllUnits;
        }

        protected override async Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand)
        {
            await _deviceService.RefreshDevices();

            return new GeneralResponse
            {
                State = DeviceResponseState.OK,
                DevicePayload = SystemManager.InstalledSensors
            };
        }
        protected override async Task<bool> RequestIsValid(JsonCommand jsonCommand)
        {
            return true;
        }
    }
}

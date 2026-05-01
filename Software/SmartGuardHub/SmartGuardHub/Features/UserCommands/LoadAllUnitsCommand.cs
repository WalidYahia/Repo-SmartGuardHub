using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;

namespace SmartGuardHub.Features.UserCommands
{
    public class LoadAllUnitsCommand : UserCommand
    {
        public LoadAllUnitsCommand(IEnumerable<ISystemSensor> systemSensors, LoggingService loggingService, DeviceService deviceService)
           : base(systemSensors, loggingService, deviceService)
        {
            jsonCommandType = Enums.JsonCommandType.LoaddAllUnits;
        }

        protected override async Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand) =>
            new GeneralResponse { State = DeviceResponseState.OK, DevicePayload = SystemManager.InstalledSensors };

        protected override async Task<bool> RequestIsValid(JsonCommand jsonCommand) => true;
    }
}

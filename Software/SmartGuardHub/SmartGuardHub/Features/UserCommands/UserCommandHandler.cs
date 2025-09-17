using System.Text.Json;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols;

namespace SmartGuardHub.Features.UserCommands
{
    public class UserCommandHandler
    {
        private readonly IEnumerable<UserCommand> _userCommands;
        private readonly LoggingService _loggingService;

        public UserCommandHandler(IEnumerable<UserCommand> userCommands, LoggingService loggingService)
        {
            _userCommands = userCommands;
            _loggingService = loggingService;
        }

        public async Task<GeneralResponse> HandleUserCommand(JsonCommand jsonCommand)
        {
            if (jsonCommand == null)
            {
                await _loggingService.LogTraceAsync(LogMessageKey.UserCommandHandler, $"HandleUserCommand - Command Is Null");

                return new GeneralResponse
                {
                    State = DeviceResponseState.NoContent,
                    DevicePayload = "NoContent"
                };
            }

            UserCommand? deviceCommand = _userCommands.FirstOrDefault(c => c.jsonCommandType == jsonCommand.jsonCommandType);

            return await deviceCommand.ExecuteCommandAsync(jsonCommand);
        }
    }
}

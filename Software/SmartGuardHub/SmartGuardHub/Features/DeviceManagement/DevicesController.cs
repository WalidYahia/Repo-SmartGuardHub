using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Features.UserCommands;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols;
using SmartGuardHub.Protocols.MQTT;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.DeviceManagement
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        private readonly LoggingService _loggingService;
        private readonly UserCommandHandler _userCommandHandler;
        private readonly IMqttService _mqttService;

        public DevicesController(LoggingService loggingService, UserCommandHandler userCommandHandler, IMqttService mqttService)
        {
            _loggingService = loggingService;
            _userCommandHandler = userCommandHandler;
            _mqttService = mqttService;
        }


        [HttpPost("handleUserCommand")]
        public async Task<IActionResult> HandleUserCommand([FromBody] JsonCommand jsonCommand)
        {
            if (jsonCommand == null)
            {
                return BadRequest("Request body is empty.");
            }

            try
            {
                var result = await _userCommandHandler.HandleApiUserCommand(jsonCommand);

                switch (result.State)
                {
                    case DeviceResponseState.DeviceDataIsRequired:
                    case DeviceResponseState.DeviceAlreadyRegistered:
                    case DeviceResponseState.DeviceNameAlreadyRegistered:
                    case DeviceResponseState.InchingIntervalValidationError:
                    case DeviceResponseState.EmptyPayload:
                    case DeviceResponseState.NoContent:
                        return BadRequest(result);

                    case DeviceResponseState.Conflict:
                        return Conflict(result);

                    //case DeviceResponseState.OK:
                    //case DeviceResponseState.NotFound:
                    //case DeviceResponseState.Timeout:
                    //case DeviceResponseState.BadRequest:
                    //case DeviceResponseState.Error:
                    default:

                        if (result.State == DeviceResponseState.OK)
                        {
                            switch (jsonCommand.JsonCommandType)
                            {
                                case JsonCommandType.TurnOn:
                                    //_mqttService.PublishAsync(SystemManager.GetMqttTopicPath(MqttTopics.DeviceDataTopic) + $"/{jsonCommand.CommandPayload.InstalledSensorId}", new UnitMqttPayload { SensorId = jsonCommand.CommandPayload.InstalledSensorId.ToString(), Value = SwitchOutletStatus.On }, retainFlag: true);
                                    break;

                                case JsonCommandType.TurnOff:
                                    //_mqttService.PublishAsync(SystemManager.GetMqttTopicPath(MqttTopics.DeviceDataTopic) + $"/{jsonCommand.CommandPayload.InstalledSensorId}", new UnitMqttPayload { SensorId = jsonCommand.CommandPayload.InstalledSensorId.ToString(), Value = SwitchOutletStatus.Off }, retainFlag: true);
                                    break;
                            }
                        }

                        return Ok(result);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(LogMessageKey.DevicesController, $"Error - CreateDevice", ex);
                
                var problemDetails = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An error occurred while processing the request."
                };

                return new ObjectResult(problemDetails);
            }
        }
    }

    public class PublishRequest
    {
        public string Topic { get; set; } = string.Empty;
        public object Message { get; set; } = new();
    }
}

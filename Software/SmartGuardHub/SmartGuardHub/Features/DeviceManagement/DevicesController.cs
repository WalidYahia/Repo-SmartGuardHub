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
                var result = await _userCommandHandler.HandleUserCommand(jsonCommand);

                if (result.State == DeviceResponseState.DeviceAlreadyRegistered
                    || result.State == DeviceResponseState.DeviceNameAlreadyRegistered
                    || result.State == DeviceResponseState.DeviceDataIsRequired)
                    return BadRequest(result);

                else if (result.State == DeviceResponseState.Conflict)
                    return Conflict(result);

                else
                {
                    return Ok(result);

                    //MqttPublishUserAction(device, result, new UnitMqttPayload { UnitId = device.Id.ToString(), Value = SwitchOutletStatus.On });
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

        private async Task MqttPublishUserAction(SensorDTO device, GeneralResponse deviceResult, UnitMqttPayload mqttPayload)
        {
            if (deviceResult != null && deviceResult.State == DeviceResponseState.OK)
                _mqttService.PublishAsync(SystemManager.GetMqttTopicPath(MqttTopics.DeviceDataTopic) + $"/{device.Id}", mqttPayload, retainFlag: true);
        }
    }

    public class PublishRequest
    {
        public string Topic { get; set; } = string.Empty;
        public object Message { get; set; } = new();
    }
}

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
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols;
using SmartGuardHub.Protocols.MQTT;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace SmartGuardHub.Features.DeviceManagement
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        private readonly LoggingService _loggingService;
        private readonly DeviceService _deviceService;
        private readonly DeviceCommunicationManager _deviceCommunicationManager;
        private readonly IEnumerable<ISystemDevice> _systemDevices;
        private readonly IMqttService _mqttService;

        public DevicesController(DeviceService deviceService, DeviceCommunicationManager deviceCommunicationManager, IEnumerable<ISystemDevice> systemDevices, IEnumerable<IDeviceProtocol> protocols, LoggingService loggingService, IMqttService mqttService)
        {
            _deviceService = deviceService;
            _loggingService = loggingService;
            _systemDevices = systemDevices;
            _deviceCommunicationManager = deviceCommunicationManager;

            _mqttService = mqttService;
        }


        [HttpPost("createDevice")]
        public async Task<IActionResult> CreateDevice([FromBody] ApiCreateDeviceRequest apiCreateDeviceRequest)
        {
            if (apiCreateDeviceRequest == null)
            {
                return BadRequest("Request body is empty.");
            }

            if (string.IsNullOrEmpty(apiCreateDeviceRequest.DeviceId) || string.IsNullOrEmpty(apiCreateDeviceRequest.Name))
            {
                return BadRequest("Device data is required.");
            }

            DeviceDTO deviceCheck1 = SystemManager.Units.FirstOrDefault(d => d.DeviceId == apiCreateDeviceRequest.DeviceId && d.SwitchNo == apiCreateDeviceRequest.SwitchNo);
            if (deviceCheck1 != null)
            {
                return BadRequest("Device already registered.");
            }

            DeviceDTO deviceCheck2 = SystemManager.Units.FirstOrDefault(d => d.Name == apiCreateDeviceRequest.Name);
            if (deviceCheck2 != null)
            {
                return BadRequest("Device with the same name is already registered.");
            }

            try
            {
                var systemDevice = _systemDevices.FirstOrDefault(d => d.DeviceType == apiCreateDeviceRequest.DeviceType);

                DeviceDTO deviceDTO = new DeviceDTO
                {
                    DeviceId = apiCreateDeviceRequest.DeviceId,
                    SwitchNo = apiCreateDeviceRequest.SwitchNo,
                    Name = apiCreateDeviceRequest.Name,
                    Type = apiCreateDeviceRequest.DeviceType,
                    Url = systemDevice.BaseUrl + apiCreateDeviceRequest.DeviceId + ":" + systemDevice.PortNo,
                    Protocol = systemDevice.ProtocolType,
                    IsOnline = false,
                    CreatedAt = SystemManager.TimeNow(),
                };

                var createdDevice = await _deviceService.CreateDeviceAsync(deviceDTO);

                await _deviceService.RefreshDevices();

                return Ok(deviceDTO);
            }
            catch (DbUpdateException ex)
            {
                await _loggingService.LogErrorAsync(LogMessageKey.DevicesConflict, $"ConflictError - CreateDevice", ex);

                return Conflict(ex.Message);
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

        [HttpPost("renameDevice")]
        public async Task<IActionResult> RenameDevice([FromBody] ApiRenameDeviceRequest apiRenameDeviceRequest)
        {
            if (apiRenameDeviceRequest == null)
            {
                return BadRequest("Request body is empty.");
            }

            if (string.IsNullOrEmpty(apiRenameDeviceRequest.Name.Trim()))
            {
                return BadRequest("Device data is required.");
            }

            DeviceDTO selectedDevice = SystemManager.Units.FirstOrDefault(d => d.DeviceId == apiRenameDeviceRequest.DeviceId && d.SwitchNo == apiRenameDeviceRequest.SwitchNo);
            if (selectedDevice == null)
            {
                return BadRequest("This device is not registered");
            }

            try
            {
                selectedDevice.Name = apiRenameDeviceRequest.Name;

                var createdDevice = await _deviceService.UpdateDeviceAsync(selectedDevice);

                await _deviceService.RefreshDevices();

                return Ok(selectedDevice);
            }
            catch (DbUpdateException ex)
            {
                await _loggingService.LogErrorAsync(LogMessageKey.DevicesController, $"ConflictError - RenameDevice", ex);
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(LogMessageKey.DevicesController, $"Error - RenameDevice", ex);

                var problemDetails = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An error occurred while processing the request."
                };

                return new ObjectResult(problemDetails);
            }
        }

        [HttpGet("loadDevices")]
        public async Task<IActionResult> LoadAllDevices()
        {
            try
            {
                await _deviceService.RefreshDevices();

                return Ok(SystemManager.Units);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(LogMessageKey.DevicesController, $"Error - LoadAllDevices", ex);

                var problemDetails = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An error occurred while processing the request."
                };

                return new ObjectResult(problemDetails);
            }
        }

        [HttpPost("on")]
        public async Task<IActionResult> On([FromBody] ApiSwitchRequest apiSwitchRequest)
        {
            try
            {
                if (apiSwitchRequest == null)
                {
                    return BadRequest("Request body is empty.");
                }

                DeviceDTO device = SystemManager.Units.FirstOrDefault(d => d.DeviceId == apiSwitchRequest.DeviceId && d.SwitchNo == apiSwitchRequest.SwitchNo);

                if (device != null)
                {
                    var systemDevice = _systemDevices.FirstOrDefault(d => d.DeviceType == device.Type);
                    var command = systemDevice.GetOnCommand(apiSwitchRequest.DeviceId, apiSwitchRequest.SwitchNo);

                    string jsonString = JsonConvert.SerializeObject(command);

                    var result = await _deviceCommunicationManager.SendCommandAsync(device, device.Url + "/zeroconf/switches", jsonString);

                    await MqttPublishUserAction(result, new UnitMqttPayload {UnitId = device.Id.ToString(), Value = SwitchOutletStatus.On });

                    return Ok(result);
                }
                else
                {
                    await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"On - Device with ID {apiSwitchRequest.DeviceId}-{(int)apiSwitchRequest.SwitchNo} not found.");

                    return Ok(new DeviceResponse { State = DeviceResponseState.NotFound });
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(LogMessageKey.DevicesController, $"An error occurred while turning on the device {apiSwitchRequest.DeviceId}-{(int)apiSwitchRequest.SwitchNo}", ex);

                var problemDetails = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "Internal Server Error",
                    Detail = ex.Message
                };

                return new ObjectResult(problemDetails);
            }
        }

        [HttpPost("off")]
        public async Task<IActionResult> Off([FromBody] ApiSwitchRequest apiSwitchRequest)
        {
            try
            {
                if (apiSwitchRequest == null)
                {
                    return BadRequest("Request body is empty.");
                }

                DeviceDTO device = SystemManager.Units.FirstOrDefault(d => d.DeviceId == apiSwitchRequest.DeviceId && d.SwitchNo == apiSwitchRequest.SwitchNo);

                if (device != null)
                {
                    var systemDevice = _systemDevices.FirstOrDefault(d => d.DeviceType == device.Type);
                    var command = systemDevice.GetOffCommand(apiSwitchRequest.DeviceId, apiSwitchRequest.SwitchNo);

                    string jsonString = JsonConvert.SerializeObject(command);

                    var result = await _deviceCommunicationManager.SendCommandAsync(device, device.Url + "/zeroconf/switches", jsonString);

                    await MqttPublishUserAction(result, new UnitMqttPayload { UnitId = device.Id.ToString(), Value = SwitchOutletStatus.Off });

                    return Ok(result);
                }
                else
                {
                    await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"Off - Device with ID {apiSwitchRequest.DeviceId}-{(int)apiSwitchRequest.SwitchNo} not found.");

                    return Ok(new DeviceResponse { State = DeviceResponseState.NotFound });
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(LogMessageKey.DevicesController, $"An error occurred while turning off the device {apiSwitchRequest.DeviceId}-{(int)apiSwitchRequest.SwitchNo}", ex);

                var problemDetails = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "Internal Server Error",
                    Detail = ex.Message
                };

                return new ObjectResult(problemDetails);
            }
        }

        [HttpPost("getInfo")]
        public async Task<IActionResult> GetInfo(string deviceId)
        {
            try
            {
                DeviceDTO device = SystemManager.Units.FirstOrDefault(d => d.DeviceId == deviceId);

                var result = await GetInfoResponse(device);

                if (result != null)
                    return Ok(result);
                else
                {
                    await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"Info - Device with ID {deviceId} not found.");

                    return Ok(new DeviceResponse { State = DeviceResponseState.NotFound });
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(LogMessageKey.DevicesController, $"An error occurred while get info of the device {deviceId}", ex);
                
                var problemDetails = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "Internal Server Error",
                    Detail = ex.Message
                };

                return new ObjectResult(problemDetails);
            }
        }

        [HttpPost("enableInchingMode")]
        public async Task<IActionResult> EnableInchingMode([FromBody] ApiEnableInchingModeRequest apiEnableInchingModeRequest)
        {
            try
            {
                if (apiEnableInchingModeRequest == null)
                {
                    return BadRequest("Request body is empty.");
                }

                DeviceDTO device = SystemManager.Units.FirstOrDefault(d => d.DeviceId == apiEnableInchingModeRequest.DeviceId && d.SwitchNo == apiEnableInchingModeRequest.SwitchNo);

                if (device != null)
                {
                    var systemDevice = _systemDevices.FirstOrDefault(d => d.DeviceType == device.Type);

                    DeviceResponse infoResponse = await GetInfoResponse(device);

                    var inchingCommand = systemDevice.GetOnInchingCommand(device.DeviceId, device.SwitchNo, apiEnableInchingModeRequest.InchingTimeInMs, (infoResponse.DevicePayload as SonoffMiniRResponsePayload).Data.Pulses);

                    string jsonString = JsonConvert.SerializeObject(inchingCommand);

                    var result = await _deviceCommunicationManager.SendCommandAsync(device, device.Url + "/zeroconf/pulses", jsonString);

                    return Ok(result);
                }
                else
                {
                    await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"InchingOn - Device with ID {apiEnableInchingModeRequest.DeviceId}-{(int)apiEnableInchingModeRequest.SwitchNo} not found.");

                    return Ok(new DeviceResponse { State = DeviceResponseState.NotFound });
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(LogMessageKey.DevicesController, $"An error occurred while inchingOn Device with ID {apiEnableInchingModeRequest.DeviceId}-{(int)apiEnableInchingModeRequest.SwitchNo}", ex);

                var problemDetails = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "Internal Server Error",
                    Detail = ex.Message
                };

                return new ObjectResult(problemDetails);
            }
        }

        [HttpPost("disableInchingMode")]
        public async Task<IActionResult> DisableInchingMode([FromBody] ApiDisableInchingModeRequest apiDisableInchingModeRequest)
        {
            try
            {
                if (apiDisableInchingModeRequest == null)
                {
                    return BadRequest("Request body is empty.");
                }

                DeviceDTO device = SystemManager.Units.FirstOrDefault(d => d.DeviceId == apiDisableInchingModeRequest.DeviceId && d.SwitchNo == apiDisableInchingModeRequest.SwitchNo);

                if (device != null)
                {
                    var systemDevice = _systemDevices.FirstOrDefault(d => d.DeviceType == device.Type);

                    DeviceResponse infoResponse = await GetInfoResponse(device);

                    var inchingCommand = systemDevice.GetOffInchingCommand(device.DeviceId, device.SwitchNo, (infoResponse.DevicePayload as SonoffMiniRResponsePayload).Data.Pulses);

                    string jsonString = JsonConvert.SerializeObject(inchingCommand);

                    var result = await _deviceCommunicationManager.SendCommandAsync(device, device.Url + "/zeroconf/pulses", jsonString);

                    return Ok(result);
                }
                else
                {
                    await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"InchingOff - Device with Device with ID {apiDisableInchingModeRequest.DeviceId}-{(int)apiDisableInchingModeRequest.SwitchNo} not found.");

                    return Ok(new DeviceResponse { State = DeviceResponseState.NotFound });
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(LogMessageKey.DevicesController, $"An error occurred while inchingOff Device with ID {apiDisableInchingModeRequest.DeviceId}-{(int)apiDisableInchingModeRequest.SwitchNo}", ex);
                
                var problemDetails = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "Internal Server Error",
                    Detail = ex.Message
                };

                return new ObjectResult(problemDetails);
            }
        }

        private async Task<DeviceResponse> GetInfoResponse(DeviceDTO device)
        {
            if (device != null)
            {
                var systemDevice = _systemDevices.FirstOrDefault(d => d.DeviceType == device.Type);
                var command = systemDevice.GetInfoCommand(device.DeviceId);
                string jsonString = JsonConvert.SerializeObject(command);
                return await _deviceCommunicationManager.SendCommandAsync(device, device.Url + "/zeroconf/info", jsonString);
            }
            else
            {
                return null;
            }
        }

        private async Task MqttPublishUserAction(DeviceResponse deviceResult, UnitMqttPayload mqttPayload)
        {
            if (deviceResult != null && deviceResult.State == DeviceResponseState.OK)
                _mqttService.PublishAsync(SystemManager.GetMqttTopicPath(MqttTopics.ActionTopic_Hub), mqttPayload, retainFlag: false);
        }
    }

    public class PublishRequest
    {
        public string Topic { get; set; } = string.Empty;
        public object Message { get; set; } = new();
    }
}

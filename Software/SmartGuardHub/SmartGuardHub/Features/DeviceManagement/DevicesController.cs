using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace SmartGuardHub.Features.DeviceManagement
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        private readonly DeviceService _deviceService;
        private readonly DeviceCommunicationManager _deviceCommunicationManager;
        private readonly IEnumerable<ISystemDevice> _systemDevices;
        private readonly ILogger<DevicesController> _logger;

        public DevicesController(DeviceService deviceService, DeviceCommunicationManager deviceCommunicationManager, IEnumerable<ISystemDevice> systemDevices, IEnumerable<IDeviceProtocol> protocols, ILogger<DevicesController> logger)
        {
            _deviceService = deviceService;
            _logger = logger;
            _systemDevices = systemDevices;
            _deviceCommunicationManager = deviceCommunicationManager;
        }


        [HttpPost("createDevice")]
        public async Task<IActionResult> CreateDevice(DeviceType deviceType, string deviceId, SwitchOutlet switchNo, string name)
        {
            if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(name))
            {
                return BadRequest("Device data is required.");
            }

            DeviceDTO deviceCheck1 = SystemManager.Devices.FirstOrDefault(d => d.DeviceId == deviceId && d.SwitchNo == switchNo);
            if (deviceCheck1 != null)
            {
                return BadRequest("Device already registered.");
            }

            DeviceDTO deviceCheck2 = SystemManager.Devices.FirstOrDefault(d => d.Name == name);
            if (deviceCheck2 != null)
            {
                return BadRequest("Device with the same name is already registered.");
            }

            try
            {
                var systemDevice = _systemDevices.FirstOrDefault(d => d.DeviceType == deviceType);

                DeviceDTO deviceDTO = new DeviceDTO
                {
                    DeviceId = deviceId,
                    SwitchNo = switchNo,
                    Name = name,
                    Type = deviceType,
                    Url = systemDevice.BaseUrl + deviceId + ":" + systemDevice.PortNo,
                    Protocol = systemDevice.ProtocolType,
                    IsOnline = false,
                    CreatedAt = DateTime.UtcNow,
                };

                var createdDevice = await _deviceService.CreateDeviceAsync(deviceDTO);

                await _deviceService.RefreshDevices();

                return Ok(deviceDTO);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
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
        public async Task<IActionResult> RenameDevice(int id, string name)
        {
            if (string.IsNullOrEmpty(name.Trim()))
            {
                return BadRequest("Device data is required.");
            }

            DeviceDTO selectedDevice = SystemManager.Devices.FirstOrDefault(d => d.Id == id);
            if (selectedDevice == null)
            {
                return BadRequest("This device is not registered");
            }

            try
            {
                selectedDevice.Name = name;

                var createdDevice = await _deviceService.UpdateDeviceAsync(selectedDevice);

                await _deviceService.RefreshDevices();

                return Ok(selectedDevice);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
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

                return Ok(SystemManager.Devices);
            }
            catch (Exception)
            {
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
        public async Task<IActionResult> On(string deviceId, SwitchOutlet switchNo)
        {
            try
            {
                DeviceDTO device = SystemManager.Devices.FirstOrDefault(d => d.DeviceId == deviceId && d.SwitchNo == switchNo);

                if (device != null)
                {
                    var systemDevice = _systemDevices.FirstOrDefault(d => d.DeviceType == device.Type);
                    var command = systemDevice.GetOnCommand(deviceId, switchNo);

                    string jsonString = JsonConvert.SerializeObject(command);

                    var result = await _deviceCommunicationManager.SendCommandAsync(device, device.Url + "/zeroconf/switches", jsonString);

                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning($"Device with ID {deviceId} and switch no. {switchNo} not found.");

                    return Ok(new DeviceResponse { State = DeviceResponseState.NotFound });
                }
            }
            catch (Exception ex)
            {
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
        public async Task<IActionResult> Off(string deviceId, SwitchOutlet switchNo)
        {
            try
            {
                DeviceDTO device = SystemManager.Devices.FirstOrDefault(d => d.DeviceId == deviceId && d.SwitchNo == switchNo);

                if (device != null)
                {
                    var systemDevice = _systemDevices.FirstOrDefault(d => d.DeviceType == device.Type);
                    var command = systemDevice.GetOffCommand(deviceId, switchNo);

                    string jsonString = JsonConvert.SerializeObject(command);

                    var result = await _deviceCommunicationManager.SendCommandAsync(device, device.Url + "/zeroconf/switches", jsonString);

                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning($"Device with ID {deviceId} and switch no. {switchNo} not found.");

                    return Ok(new DeviceResponse { State = DeviceResponseState.NotFound });
                }
            }
            catch (Exception ex)
            {
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
                DeviceDTO device = SystemManager.Devices.FirstOrDefault(d => d.DeviceId == deviceId);

                var result = await GetInfoResponse(device);

                if (result != null)
                    return Ok(result);
                else
                {
                    _logger.LogWarning($"Device with ID {deviceId} not found.");

                    return Ok(new DeviceResponse { State = DeviceResponseState.NotFound });
                }
            }
            catch (Exception ex)
            {
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
        public async Task<IActionResult> EnableInchingMode(int id, int inchingTimeInMs)
        {
            try
            {
                DeviceDTO device = SystemManager.Devices.FirstOrDefault(d => d.Id == id);

                if (device != null)
                {
                    var systemDevice = _systemDevices.FirstOrDefault(d => d.DeviceType == device.Type);

                    DeviceResponse infoResponse = await GetInfoResponse(device);

                    var inchingCommand = systemDevice.GetOnInchingCommand(device.DeviceId, device.SwitchNo, inchingTimeInMs, (infoResponse.DevicePayload as SonoffMiniRResponsePayload).Data.Pulses);

                    string jsonString = JsonConvert.SerializeObject(inchingCommand);

                    var result = await _deviceCommunicationManager.SendCommandAsync(device, device.Url + "/zeroconf/pulses", jsonString);

                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning($"Device with ID {id})");

                    return Ok(new DeviceResponse { State = DeviceResponseState.NotFound });
                }
            }
            catch (Exception ex)
            {
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
        public async Task<IActionResult> DisableInchingMode(int id)
        {
            try
            {

                DeviceDTO device = SystemManager.Devices.FirstOrDefault(d => d.Id == id);

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
                    _logger.LogWarning($"Device with ID {id})");

                    return Ok(new DeviceResponse { State = DeviceResponseState.NotFound });
                }
            }
            catch (Exception ex)
            {
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
    }
}

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

        public DevicesController(DeviceService deviceService, DeviceCommunicationManager deviceCommunicationManager, IEnumerable<ISystemDevice> systemDevices, IEnumerable<IDeviceProtocol> protocols, LoggingService loggingService)
        {
            _deviceService = deviceService;
            _loggingService = loggingService;
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
        public async Task<IActionResult> RenameDevice(string deviceId, SwitchOutlet switchNo, string name)
        {
            if (string.IsNullOrEmpty(name.Trim()))
            {
                return BadRequest("Device data is required.");
            }

            DeviceDTO selectedDevice = SystemManager.Devices.FirstOrDefault(d => d.DeviceId == deviceId && d.SwitchNo == switchNo);
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

                return Ok(SystemManager.Devices);
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
                    await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"On - Device with ID {deviceId}-{(int)switchNo} not found.");

                    return Ok(new DeviceResponse { State = DeviceResponseState.NotFound });
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(LogMessageKey.DevicesController, $"An error occurred while turning on the device {deviceId}-{(int)switchNo}", ex);

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
                    await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"Off - Device with ID {deviceId}-{(int)switchNo} not found.");

                    return Ok(new DeviceResponse { State = DeviceResponseState.NotFound });
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(LogMessageKey.DevicesController, $"An error occurred while turning off the device {deviceId}-{(int)switchNo}", ex);

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
        public async Task<IActionResult> EnableInchingMode(string deviceId, SwitchOutlet switchNo, int inchingTimeInMs)
        {
            try
            {
                DeviceDTO device = SystemManager.Devices.FirstOrDefault(d => d.DeviceId == deviceId && d.SwitchNo == switchNo);

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
                    await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"InchingOn - Device with ID {deviceId}-{(int)switchNo} not found.");

                    return Ok(new DeviceResponse { State = DeviceResponseState.NotFound });
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(LogMessageKey.DevicesController, $"An error occurred while inchingOn Device with ID {deviceId}-{(int)switchNo}", ex);

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
        public async Task<IActionResult> DisableInchingMode(string deviceId, SwitchOutlet switchNo)
        {
            try
            {

                DeviceDTO device = SystemManager.Devices.FirstOrDefault(d => d.DeviceId == deviceId && d.SwitchNo == switchNo);

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
                    await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"InchingOff - Device with Device with ID {deviceId}-{(int)switchNo} not found.");

                    return Ok(new DeviceResponse { State = DeviceResponseState.NotFound });
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(LogMessageKey.DevicesController, $"An error occurred while inchingOff Device with ID {deviceId}-{(int)switchNo}", ex);
                
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

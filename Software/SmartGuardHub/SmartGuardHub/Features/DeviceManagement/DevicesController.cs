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
        private readonly IEnumerable<ISystemDevice> _systemDevices;
        private readonly IEnumerable<IDeviceProtocol> _protocols;
        private readonly ILogger<DevicesController> _logger;
        
        public DevicesController(DeviceService deviceService, IEnumerable<ISystemDevice> systemDevices, IEnumerable<IDeviceProtocol> protocols, ILogger<DevicesController> logger)
        {
            _deviceService = deviceService;
            _logger = logger;
            _systemDevices = systemDevices;
            _protocols = protocols;
        }


        [HttpPost("on")]
        public object On(string deviceId, SwitchOutlet switchNo)
        {
            DeviceDTO device = SystemManager.Devices.FirstOrDefault(d => d.DeviceId == deviceId && d.SwitchNo == switchNo);

            if (device != null)
            {
                var systemDevice = _systemDevices.FirstOrDefault(d => d.DeviceType == device.Type);
                var protocol = _protocols.FirstOrDefault(p => p.ProtocolType == device.Protocol);
                var command = systemDevice.GetOnCommand(deviceId, switchNo);

                string jsonString = JsonConvert.SerializeObject(command);

                return protocol.SendCommandAsync(device.Url + "/zeroconf/switches", jsonString).Result.Content.ReadAsStringAsync().Result;
            }
            else
            {
                _logger.LogWarning($"Device with ID {deviceId} and switch no. {switchNo} not found.");
                return NotFound($"Device with ID {deviceId} and switch no. {switchNo} not found.");
            }
        }

        [HttpPost("off")]
        public object Off(string deviceId, SwitchOutlet switchNo)
        {
            DeviceDTO device = SystemManager.Devices.FirstOrDefault(d => d.DeviceId == deviceId && d.SwitchNo == switchNo);

            if (device != null)
            {
                var systemDevice = _systemDevices.FirstOrDefault(d => d.DeviceType == device.Type);
                var protocol = _protocols.FirstOrDefault(p => p.ProtocolType == device.Protocol);
                var command = systemDevice.GetOffCommand(deviceId, switchNo);

                string jsonString = JsonConvert.SerializeObject(command);

                return protocol.SendCommandAsync(device.Url + "/zeroconf/switches", jsonString).Result.Content.ReadAsStringAsync().Result;
            }
            else
            {
                _logger.LogWarning($"Device with ID {deviceId} and switch no. {switchNo} not found.");
                return NotFound($"Device with ID {deviceId} and switch no. {switchNo} not found.");
            }

        }

        [HttpPost("getInfo")]
        public object GetInfo(string deviceId)
        {
            DeviceDTO device = SystemManager.Devices.FirstOrDefault(d => d.DeviceId == deviceId);

            if (device != null)
            {
                var systemDevice = _systemDevices.FirstOrDefault(d => d.DeviceType == device.Type);
                var protocol = _protocols.FirstOrDefault(p => p.ProtocolType == device.Protocol);
                var command = systemDevice.GetInfoCommand(deviceId);

                string jsonString = JsonConvert.SerializeObject(command);

                return protocol.SendCommandAsync(device.Url + "/zeroconf/info", jsonString).Result.Content.ReadAsStringAsync().Result;
            }
            else
            {
                _logger.LogWarning($"Device with ID {deviceId} not found.");
                return NotFound($"Device with ID {deviceId} not found.");
            }

        }

        [HttpPost("createDevice")]
        public async Task<IActionResult> CreateDevice(DeviceType deviceType, string deviceId, SwitchOutlet switchNo, string name)
        {
            if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(name))
            {
                return BadRequest("Device data is required.");
            }

            DeviceDTO deviceCheck1 = SystemManager.Devices.FirstOrDefault(d => d.DeviceId == deviceId && d.SwitchNo == switchNo);
            if(deviceCheck1 != null)
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

        [HttpPost("enableInchingMode")]
        public object EnableInchingMode(int id, int inchingTimeInMs)
        {
            DeviceDTO device = SystemManager.Devices.FirstOrDefault(d => d.Id == id);

            if (device != null)
            {
                var systemDevice = _systemDevices.FirstOrDefault(d => d.DeviceType == device.Type);
                var protocol = _protocols.FirstOrDefault(p => p.ProtocolType == device.Protocol);

                DeviceResponse deviceInfo = JsonConvert.DeserializeObject<DeviceResponse>(GetInfo(device.DeviceId).ToString());

                var inchingCommand = systemDevice.GetOnInchingCommand(device.DeviceId, device.SwitchNo, inchingTimeInMs, deviceInfo.Data.Pulses);
                string jsonString = JsonConvert.SerializeObject(inchingCommand);

                return protocol.SendCommandAsync(device.Url + "/zeroconf/pulses", jsonString).Result.Content.ReadAsStringAsync().Result;
            }
            else
            {
                _logger.LogWarning($"Device with ID {id})");
                return NotFound($"Device with ID {id} not found.");
            }
        }

        [HttpPost("disableInchingMode")]
        public object DisableInchingMode(int id)
        {
            DeviceDTO device = SystemManager.Devices.FirstOrDefault(d => d.Id == id);

            if (device != null)
            {
                var systemDevice = _systemDevices.FirstOrDefault(d => d.DeviceType == device.Type);
                var protocol = _protocols.FirstOrDefault(p => p.ProtocolType == device.Protocol);

                DeviceResponse deviceInfo = JsonConvert.DeserializeObject<DeviceResponse>(GetInfo(device.DeviceId).ToString());

                var inchingCommand = systemDevice.GetOffInchingCommand(device.DeviceId, device.SwitchNo, deviceInfo.Data.Pulses);
                string jsonString = JsonConvert.SerializeObject(inchingCommand);

                return protocol.SendCommandAsync(device.Url + "/zeroconf/pulses", jsonString).Result.Content.ReadAsStringAsync().Result;
            }
            else
            {
                _logger.LogWarning($"Device with ID {id})");
                return NotFound($"Device with ID {id} not found.");
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
    }
}

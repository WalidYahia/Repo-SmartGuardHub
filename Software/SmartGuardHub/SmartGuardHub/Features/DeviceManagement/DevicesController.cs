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



        //[HttpGet]
        //public async Task<IActionResult> GetAllDevices()
        //{
        //    var devices = await _deviceService.GetAllDevicesAsync();
        //    return Ok(devices);
        //}
        //[HttpGet("{deviceId}/{switchNo}")]
        //public async Task<IActionResult> GetDevice(string deviceId, int switchNo)
        //{
        //    var device = await _deviceService.GetDeviceAsync(deviceId, (SwitchNo)switchNo);
        //    if (device == null)
        //    {
        //        return NotFound();
        //    }
        //    return Ok(device);
        //}

        [HttpPost("on")]
        public object On(string deviceId, SwitchNo switchNo)
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
        public object Off(string deviceId, SwitchNo switchNo)
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
        public async Task<IActionResult> CreateDevice(DeviceType deviceType, string deviceId, SwitchNo switchNo, string name)
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

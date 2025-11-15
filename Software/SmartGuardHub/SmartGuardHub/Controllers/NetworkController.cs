using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Network;
using NetworkInterface = System.Net.NetworkInformation.NetworkInterface;

namespace SmartGuardHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NetworkController : ControllerBase
    {
        private readonly NetworkConfigurationManager _networkManager;

        public NetworkController(NetworkConfigurationManager networkManager)
        {
            _networkManager = networkManager;
        }

        // Enable setup mode (reset to default network)
        [HttpPost("setup-mode")]
        public async Task<IActionResult> EnableSetupMode()
        {
            if (!SystemManager.IsRaspberryPi)
                return Ok();

            var result = await _networkManager.EnableSetupModeAsync();

            if (result)
            {
                return Ok(new
                {
                    message = "Setup mode enabled successfully",
                    lanIP = "10.0.0.1",
                    wifiIP = "20.0.0.1",
                    wifiSSID = "SmartHomeHub-Setup",
                    wifiPassword = "12345678"
                });
            }

            return StatusCode(500, new { message = "Failed to enable setup mode" });
        }

        // Get current network information
        [HttpGet("info")]
        public IActionResult GetNetworkInfo()
        {
            if (!SystemManager.IsRaspberryPi)
                return Ok();

            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                var info = new List<object>();

                foreach (var ni in networkInterfaces)
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    {
                        var ipProps = ni.GetIPProperties();
                        var addresses = new List<string>();

                        foreach (var ip in ipProps.UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                addresses.Add(ip.Address.ToString());
                            }
                        }

                        info.Add(new
                        {
                            name = ni.Name,
                            description = ni.Description,
                            type = ni.NetworkInterfaceType.ToString(),
                            status = ni.OperationalStatus.ToString(),
                            addresses = addresses,
                            macAddress = ni.GetPhysicalAddress().ToString()
                        });
                    }
                }

                return Ok(info);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Apply user's network configuration
        [HttpPost("configure")]
        public async Task<IActionResult> ConfigureNetwork([FromBody] NetworkConfig request)
        {
            if (!SystemManager.IsRaspberryPi)
                return Ok();

            if (request == null)
            {
                return BadRequest(new { message = "Invalid configuration" });
            }

            // Validate inputs
            if (!request.UseDHCP && string.IsNullOrEmpty(request.StaticIP))
            {
                return BadRequest(new { message = "Static IP is required when DHCP is disabled for LAN" });
            }

            if (request.NetworkMode == NetworkMode.WIFI)
            {
                if(string.IsNullOrEmpty(request.WiFiSSID) || string.IsNullOrEmpty(request.WiFiPassword))
                {
                    return BadRequest(new { message = "WiFi data is missing" });
                }
            }

            var result = await _networkManager.ApplyUserConfigurationAsync(request);

            if (result)
            {
                return Ok(new
                {
                    message = "Network configuration applied successfully. Device will reconnect to your network.",
                    reconnectIn = "30 seconds"
                });
            }

            return StatusCode(500, new { message = "Failed to apply network configuration" });
        }

        // Test network connectivity
        [HttpGet("test-connectivity")]
        public async Task<IActionResult> TestConnectivity([FromQuery] string host = "8.8.8.8")
        {
            if (!SystemManager.IsRaspberryPi)
                return Ok();

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, 3000);

                return Ok(new
                {
                    success = reply.Status == IPStatus.Success,
                    status = reply.Status.ToString(),
                    roundtripTime = reply.RoundtripTime,
                    address = reply.Address?.ToString()
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }
    }
}

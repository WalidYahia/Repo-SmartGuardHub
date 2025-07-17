using System.Text.Json;
using System.Text;
using SmartGuardHub.Features.DeviceManagement;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using SmartGuardHub.Features.SystemDevices;

namespace SmartGuardHub.Protocols
{
    public class RestProtocol : IDeviceProtocol
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RestProtocol> _logger;

        public DeviceProtocolType ProtocolType => DeviceProtocolType.Rest;

        public RestProtocol(HttpClient httpClient, ILogger<RestProtocol> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<DeviceResponse> SendCommandAsync(string destination, string command, object? parameters = null)
        {
            try
            {
                // For Sonoff devices in DIY mode, typically REST API on port 8081
                // $"http://{hostName}:8081/zeroconf/switches";

                var content = new StringContent(command, Encoding.UTF8, "application/json");

                _logger.LogDebug("Sending REST command to {Url}: {Command}", destination, command);

                HttpResponseMessage response = await _httpClient.PostAsync(destination, content);


                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("REST response: {Response}", responseContent);

                    return new DeviceResponse
                    {
                        State = DeviceResponseState.OK,
                        DevicePayload = responseContent
                    };
                }
                else
                {
                    _logger.LogWarning("REST command failed with status {Status} for device {DeviceId}",
                        response.StatusCode, destination); 
                    
                    DeviceResponse deviceResponse = new DeviceResponse();

                    switch (response.StatusCode)
                    {
                        case System.Net.HttpStatusCode.BadRequest:
                            deviceResponse.State = DeviceResponseState.BadRequest;
                            break;

                        case System.Net.HttpStatusCode.NotFound:
                            deviceResponse.State = DeviceResponseState.NotFound;
                            break;

                        case System.Net.HttpStatusCode.RequestTimeout:
                            deviceResponse.State = DeviceResponseState.Timeout;
                            break;

                        default:
                            deviceResponse.State = DeviceResponseState.Error;
                            break;
                    }

                    return deviceResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send REST command {Command} to device {DeviceId}", command, destination);
                return new DeviceResponse();
            }
        }

        //public async Task<bool> DiscoverDevicesAsync()
        //{
        //    try
        //    {
        //        _logger.LogInformation("Starting REST device discovery (mDNS scan)");

        //        // Simplified discovery - scan local network for Sonoff devices
        //        // In real implementation, you'd use mDNS or network scanning
        //        var tasks = new List<Task>();

        //        for (int i = 1; i < 255; i++)
        //        {
        //            var ip = $"192.168.1.{i}";
        //            tasks.Add(ScanDeviceAsync(ip));
        //        }

        //        await Task.WhenAll(tasks);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to discover REST devices");
        //        return false;
        //    }
        //}

        //private async Task ScanDeviceAsync(string ip)
        //{
        //    try
        //    {
        //        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        //        var response = await _httpClient.GetAsync($"http://{ip}:8081/zeroconf/info", cts.Token);

        //        if (response.IsSuccessStatusCode)
        //        {
        //            var content = await response.Content.ReadAsStringAsync();
        //            _logger.LogInformation("Found Sonoff device at {IP}: {Info}", ip, content);
        //            // Parse and store discovered device info
        //        }
        //    }
        //    catch
        //    {
        //        // Ignore - device not found at this IP
        //    }
        //}

        //private DeviceStatusResponse ParseStatusResponse(string responseContent)
        //{
        //    try
        //    {
        //        using var doc = JsonDocument.Parse(responseContent);
        //        var root = doc.RootElement;

        //        var isOnline = true;
        //        var switchState = root.GetProperty("data").GetProperty("switch").GetString();
        //        var status = switchState == "on" ? DeviceStatus.On : DeviceStatus.Off;

        //        var properties = new Dictionary<string, object>();
        //        foreach (var prop in root.GetProperty("data").EnumerateObject())
        //        {
        //            properties[prop.Name] = prop.Value.ToString() ?? "";
        //        }

        //        return new DeviceStatusResponse
        //        {
        //            IsOnline = isOnline,
        //            Status = status,
        //            Properties = properties
        //        };
        //    }
        //    catch (Exception)
        //    {
        //        return new DeviceStatusResponse { IsOnline = false, Status = DeviceStatus.Unknown };
        //    }
        //}
    }
}

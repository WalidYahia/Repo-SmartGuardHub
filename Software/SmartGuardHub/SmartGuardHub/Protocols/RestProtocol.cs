using System.Text.Json;
using System.Text;
using SmartGuardHub.Features.DeviceManagement;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Infrastructure;

namespace SmartGuardHub.Protocols
{
    public class RestProtocol : IDeviceProtocol
    {
        private readonly LoggingService _loggingService;
        private readonly HttpClient _httpClient;
        public UnitProtocolType ProtocolType => UnitProtocolType.Rest;

        public RestProtocol(HttpClient httpClient, LoggingService loggingService)
        {
            _httpClient = httpClient;
            _loggingService = loggingService;
        }

        public async Task<GeneralResponse> SendCommandAsync(string destination, string command, object? parameters = null)
        {
            try
            {
                // For Sonoff devices in DIY mode, typically REST API on port 8081
                // $"http://{hostName}:8081/zeroconf/switches";

                var content = new StringContent(command, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync(destination, content);


                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();

                    return new GeneralResponse
                    {
                        State = DeviceResponseState.OK,
                        DevicePayload = responseContent
                    };
                }
                else
                {                    
                    GeneralResponse deviceResponse = new GeneralResponse();

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
                await _loggingService.LogErrorAsync(LogMessageKey.RestProtocol, $"Failed to send REST command {command} to device {destination}", ex);

                return new GeneralResponse();
            }
        }
    }
}

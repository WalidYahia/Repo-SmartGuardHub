using SmartGuardHub.Features.Logging;

namespace SmartGuardHub.Cloud
{
    /// <summary>
    /// Handles all HTTP communication with the SyncroCloud API.
    /// Uses its own dedicated HttpClient — separate from the local Sonoff device RestProtocol client.
    /// </summary>
    public class SyncroCloudService : ISyncroCloudService
    {
        private readonly HttpClient _httpClient;
        private readonly LoggingService _loggingService;

        public SyncroCloudService(HttpClient httpClient, LoggingService loggingService)
        {
            _httpClient = httpClient;
            _loggingService = loggingService;
        }

        // SyncroCloud API calls will be implemented here
    }
}

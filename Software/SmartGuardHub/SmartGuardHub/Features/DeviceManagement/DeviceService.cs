using SmartGuardHub.Protocols;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class DeviceService
    {
        //private readonly IDeviceRepository _deviceRepository;
        private readonly IEnumerable<IDeviceProtocol> _protocols;
        private readonly ILogger<DeviceService> _logger;

        public DeviceService(/*IDeviceRepository deviceRepository, */IEnumerable<IDeviceProtocol> protocols, ILogger<DeviceService> logger)
        {
            //_deviceRepository = deviceRepository;
            _protocols = protocols;
            _logger = logger;
        }
    }
}

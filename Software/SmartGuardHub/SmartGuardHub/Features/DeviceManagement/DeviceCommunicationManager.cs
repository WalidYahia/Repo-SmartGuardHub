using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Protocols;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class DeviceCommunicationManager
    {
        private readonly IEnumerable<ISystemDevice> _systemDevices;
        private readonly IEnumerable<IDeviceProtocol> _protocols;
        private readonly ILogger<DeviceCommunicationManager> _logger;

        public DeviceCommunicationManager(IEnumerable<ISystemDevice> systemDevices, IEnumerable<IDeviceProtocol> protocols, ILogger<DeviceCommunicationManager> logger)
        {
            _systemDevices = systemDevices;
            _protocols = protocols;
            _logger = logger;
        }

        public async Task<DeviceResponse> SendCommandAsync(DeviceDTO device, string destination, string command, object? parameters = null)
        {
            var protocol = _protocols.FirstOrDefault(p => p.ProtocolType == device.Protocol);

            var systemDevice = _systemDevices.FirstOrDefault(d => d.DeviceType == device.Type);

            var result = await protocol.SendCommandAsync(destination, command, parameters);

            if (result.State == DeviceResponseState.OK)
            {
                return systemDevice.ParseResponse(result);
            }
            else
                return result;
        }
    }
}

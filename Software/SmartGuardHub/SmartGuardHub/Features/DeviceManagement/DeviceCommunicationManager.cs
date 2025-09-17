using System.Text.Json;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class DeviceCommunicationManager
    {
        private readonly IEnumerable<ISystemUnit> _systemUnits;
        private readonly IEnumerable<IDeviceProtocol> _protocols;
        public DeviceCommunicationManager(IEnumerable<ISystemUnit> systemUnits, IEnumerable<IDeviceProtocol> protocols)
        {
            _systemUnits = systemUnits;
            _protocols = protocols;
        }

        public async Task<GeneralResponse> SendCommandAsync(SensorDTO device, string destination, string command, object? parameters = null)
        {
            var protocol = _protocols.FirstOrDefault(p => p.ProtocolType == device.Protocol);

            var systemDevice = _systemUnits.FirstOrDefault(d => d.DeviceType == device.Type);

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

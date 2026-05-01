using SmartGuardHub.Features.SensorConfiguration;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class DeviceCommunicationManager
    {
        private readonly IEnumerable<ISystemSensor> _systemSensors;
        private readonly IEnumerable<IDeviceProtocol> _protocols;

        public DeviceCommunicationManager(IEnumerable<ISystemSensor> systemSensors, IEnumerable<IDeviceProtocol> protocols)
        {
            _systemSensors = systemSensors;
            _protocols = protocols;
        }

        public async Task<GeneralResponse> SendCommandAsync(SensorConfig device, string destination, string command, object? parameters = null)
        {
            var protocol     = _protocols.FirstOrDefault(p => p.ProtocolType == (UnitProtocolType)device.Protocol);
            var systemSensor = _systemSensors.FirstOrDefault(s => s.SensorType == (SensorType)device.SensorType);

            var result = await protocol.SendCommandAsync(destination, command, parameters);

            return result.State == DeviceResponseState.OK
                ? systemSensor.ParseResponse(result)
                : result;
        }
    }
}

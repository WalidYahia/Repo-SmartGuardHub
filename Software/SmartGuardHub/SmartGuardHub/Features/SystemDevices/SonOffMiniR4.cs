using SmartGuardHub.Protocols;

namespace SmartGuardHub.Features.SystemDevices
{
    public class SonOffMiniR4 : ISystemDevice
    {
        public DeviceType DeviceType => DeviceType.SonoffMiniR4M;
        public DeviceProtocolType ProtocolType => DeviceProtocolType.Rest;
        public string BaseUrl => "http://eWeLink_";
        public string PortNo => "8081";
        public DeviceProtocolType GetDeviceProtocol()
        {
            throw new NotImplementedException();
        }

        public string GetDeviceUrl(string deviceId)
        {
            throw new NotImplementedException();
        }

        public DeviceRequest GetInfoCommand(string deviceId)
        {
            throw new NotImplementedException();
        }

        public DeviceRequest GetOffCommand(string deviceId, SwitchNo switchNo)
        {
            throw new NotImplementedException();
        }

        public DeviceRequest GetOnCommand(string deviceId, SwitchNo switchNo)
        {
            throw new NotImplementedException();
        }

        public DeviceRequest GetSignalStrengthCommand(string deviceId)
        {
            throw new NotImplementedException();
        }
    }
}

using System.Net.Sockets;
using SmartGuardHub.Protocols;

namespace SmartGuardHub.Features.SystemDevices
{
    public interface ISystemDevice
    {
        DeviceType DeviceType { get; }
        DeviceProtocolType ProtocolType { get; }

        string BaseUrl { get; }
        string PortNo { get; }

        public DeviceRequest GetOnCommand(string deviceId, SwitchNo switchNo);
        public DeviceRequest GetOffCommand(string deviceId, SwitchNo switchNo);
        public DeviceRequest GetInfoCommand(string deviceId);
        public DeviceRequest GetSignalStrengthCommand(string deviceId);
        public string GetDeviceUrl(string deviceId);
        public DeviceProtocolType GetDeviceProtocol();
    }

    public enum DeviceType
    {
        SonoffMiniR3 = 0,
        SonoffMiniR4M = 1,
    }

    public enum SwitchNo
    {
        First = 0,
        Second = 1,
        Third = 2,
        Fourth = 3,
    }
}

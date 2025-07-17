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

        public DeviceRequest GetOnCommand(string deviceId, SwitchOutlet switchNo);
        public DeviceRequest GetOffCommand(string deviceId, SwitchOutlet switchNo);
        public DeviceRequest GetInfoCommand(string deviceId);
        public DeviceRequest GetSignalStrengthCommand(string deviceId);
        public DeviceRequest GetOnInchingCommand(string deviceId, SwitchOutlet switchNo, int InchingTime, List<SonoffMiniRPayloadDataPulse> devicePulses);
        public DeviceRequest GetOffInchingCommand(string deviceId, SwitchOutlet switchNo, List<SonoffMiniRPayloadDataPulse> devicePulses);

        public string GetDeviceUrl(string deviceId);
        public DeviceProtocolType GetDeviceProtocol();
        public DeviceResponse ParseResponse(DeviceResponse deviceResponse);

    }

    public enum DeviceType
    {
        SonoffMiniR3 = 0,
        SonoffMiniR4M = 1,
    }

    public enum SwitchOutlet
    {
        First = 0,
        Second = 1,
        Third = 2,
        Fourth = 3,
    }
}

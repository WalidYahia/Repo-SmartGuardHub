namespace SmartGuardHub.Features.SystemDevices
{
    public interface ISystemDevice
    {
        public DeviceRequest GetOnCommand(string deviceId, SwitchNo switchNo);
        public DeviceRequest GetOffCommand(string deviceId, SwitchNo switchNo);
        public DeviceRequest GetInfoCommand(string deviceId);
        public DeviceRequest GetSignalStrengthCommand(string deviceId);
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

    public enum Request
    {
        On = 0,
        Off = 1,
        GetInfo = 2,
        GetSignalStrength = 3,
    }
}

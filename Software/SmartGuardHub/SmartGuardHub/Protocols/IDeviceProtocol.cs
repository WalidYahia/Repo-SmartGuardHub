using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;

namespace SmartGuardHub.Protocols
{
    public interface IDeviceProtocol
    {
        DeviceProtocolType ProtocolType { get; }

        Task<DeviceResponse> SendCommandAsync(string destination , string command, object? parameters = null);

        //Task<bool> DiscoverDevicesAsync();

        //Task<bool> TestConnectionAsync(string deviceId);
    }

    public interface IDeviceResponse
    {

    }
    public class DeviceStatusResponse
    {
        public bool IsOnline { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public DateTime Timestamp { get; set; } = SystemManager.TimeNow();
    }

    public enum DeviceProtocolType
    {
        Zigbee = 1,
        Rest = 2,
        Mqtt = 3
    }
}

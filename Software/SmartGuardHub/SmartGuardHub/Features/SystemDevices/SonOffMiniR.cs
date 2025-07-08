using System.Diagnostics;
using System.Net.Sockets;
using SmartGuardHub.Protocols;

namespace SmartGuardHub.Features.SystemDevices
{
    public class SonOffMiniR: ISystemDevice
    {
        public DeviceType DeviceType => DeviceType.SonoffMiniR3;
        public DeviceProtocolType ProtocolType => DeviceProtocolType.Rest;
        public string BaseUrl => "http://eWeLink_";
        public string PortNo => "8081";

        // http://eWeLink_10016ca843:8081/zeroconf/switches

        public DeviceRequest GetOnCommand(string deviceId, SwitchNo switchNo)
        {
            return new DeviceRequest
            {
                Deviceid = deviceId,
                Data = new DeviceRequestData
                {
                    Switches = new List<DeviceDataSwitch>
                    {
                        new DeviceDataSwitch
                        {
                            Switch = "on",
                            Outlet = (int)switchNo
                        }
                    }
                }
            };
        }

        public DeviceRequest GetOffCommand(string deviceId, SwitchNo switchNo)
        {
            return new DeviceRequest
            {
                Deviceid = deviceId,
                Data = new DeviceRequestData
                {
                    Switches = new List<DeviceDataSwitch>
                    {
                        new DeviceDataSwitch
                        {
                            Switch = "off",
                            Outlet = (int)switchNo
                        }
                    }
                }
            };
        }

        public DeviceRequest GetInfoCommand(string deviceId)
        {
            return new DeviceRequest
            {
                Deviceid = deviceId,
                Data = new DeviceRequestData { }
            };
        }

        public DeviceRequest GetSignalStrengthCommand(string deviceId)
        {
            return new DeviceRequest
            {
                Deviceid = deviceId,
                Data = new DeviceRequestData { }
            };
        }

        public string GetDeviceUrl(string deviceId)
        {
            return "http://eWeLink_" + deviceId + ":8081/zeroconf";
        }

        public DeviceProtocolType GetDeviceProtocol()
        {
            return DeviceProtocolType.Rest;
        }
    }
}

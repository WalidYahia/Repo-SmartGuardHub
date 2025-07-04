using System.Diagnostics;

namespace SmartGuardHub.Features.SystemDevices
{
    public class SonOffMiniR
    {
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

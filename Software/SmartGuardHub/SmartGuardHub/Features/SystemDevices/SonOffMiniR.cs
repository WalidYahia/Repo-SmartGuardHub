using System;
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

        public DeviceRequest GetOnCommand(string deviceId, SwitchOutlet switchNo)
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

        public DeviceRequest GetOffCommand(string deviceId, SwitchOutlet switchNo)
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

        //only supports multiples of 500 in range of 500~3599500
        public DeviceRequest GetOnInchingCommand(string deviceId, SwitchOutlet switchNo, int InchingTime, List<DeviceDataPulse> devicePulses)
        {
            foreach (var pulse in devicePulses)
            {
                if (pulse.Outlet == switchNo)
                {
                    pulse.Switch = "on";
                    pulse.Pulse = "on";
                    pulse.Width = InchingTime;
                }
            }

            return new DeviceRequest
            {
                Deviceid = deviceId,
                Data = new DeviceRequestData
                {
                    Pulses = devicePulses
                }
            };
        }

        public DeviceRequest GetOffInchingCommand(string deviceId, SwitchOutlet switchNo, List<DeviceDataPulse> devicePulses)
        {
            foreach (var pulse in devicePulses)
            {
                if (pulse.Outlet == switchNo)
                {
                    pulse.Switch = "off";
                    pulse.Pulse = "off";
                    pulse.Width = 0;
                }
            }

            return new DeviceRequest
            {
                Deviceid = deviceId,
                Data = new DeviceRequestData
                {
                    Pulses = devicePulses
                }
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

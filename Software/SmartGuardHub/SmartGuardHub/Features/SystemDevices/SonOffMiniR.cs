using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.SystemDevices
{
    public class SonOffMiniR: ISystemUnit
    {
        private readonly IEnumerable<IDeviceProtocol> _protocols;
        public SonOffMiniR(IEnumerable<IDeviceProtocol> protocols)
        {
            _protocols = protocols;
        }

        public UnitType DeviceType => UnitType.SonoffMiniR3;
        public UnitProtocolType ProtocolType => UnitProtocolType.Rest;
        public string BaseUrl => "http://eWeLink_";
        public string PortNo => "8081";
        public string DataPath => "/zeroconf/switches";
        public string InfoPath => "/zeroconf/info";
        public string InchingPath => "/zeroconf/pulses";


        public DeviceRequest GetOnCommand(string deviceId, SwitchOutlet switchNo)
        {
            return new DeviceRequest
            {
                Deviceid = deviceId,
                Data = new DeviceRequestData
                {
                    Switches = new List<SonoffMiniRPayloadDataSwitch>
                    {
                        new SonoffMiniRPayloadDataSwitch
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
                    Switches = new List<SonoffMiniRPayloadDataSwitch>
                    {
                        new SonoffMiniRPayloadDataSwitch
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
        public DeviceRequest GetOnInchingCommand(string deviceId, SwitchOutlet switchNo, int InchingTime, List<SonoffMiniRPayloadDataPulse> devicePulses)
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

        public DeviceRequest GetOffInchingCommand(string deviceId, SwitchOutlet switchNo, List<SonoffMiniRPayloadDataPulse> devicePulses)
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

        public UnitProtocolType GetDeviceProtocol()
        {
            return UnitProtocolType.Rest;
        }

        public GeneralResponse ParseResponse(GeneralResponse deviceResponse)
        {
            var devicePayload = Newtonsoft.Json.JsonConvert.DeserializeObject<SonoffMiniRResponsePayload>(deviceResponse.DevicePayload);

            if (devicePayload.Error != 0)
            {
                return new GeneralResponse
                {
                    State = DeviceResponseState.BadRequest,
                    DevicePayload = devicePayload
                };
            }

            return new GeneralResponse
            {
                State = deviceResponse.State,
                DevicePayload = devicePayload
            };
        }

        public async Task<GeneralResponse> SendCommandAsync(string destination, string command, object? parameters = null)
        {
            var protocol = _protocols.FirstOrDefault(p => p.ProtocolType == ProtocolType);

            if(SystemManager.IsRaspberryPi)
                destination = destination.Replace(":8081", ".local:8081");

            var result = await protocol.SendCommandAsync(destination, command, parameters);

            if (result.State == DeviceResponseState.OK)
            {
                return ParseResponse(result);
            }
            else
                return result;
        }
    }
}

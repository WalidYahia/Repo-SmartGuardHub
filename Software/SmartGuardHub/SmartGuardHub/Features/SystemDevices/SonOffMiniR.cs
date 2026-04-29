using SmartGuardHub.Features.SensorConfiguration;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

        public UnitType UnitType => UnitType.SonoffMiniR3;
        public SensorType SensorType => SensorType.Swich;
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

        public async Task<SensorConfig> MapRawInfoResponseToSensorConfig(object rawInfoResponse, SensorConfig sensor)
        {
            var unitResponse = rawInfoResponse as SonoffMiniRResponsePayload;
            var result = ShallowCopy(sensor);

            if (unitResponse != null)
            {
                var switchOutlet = (SwitchOutlet)sensor.SwitchNo;

                var latestValue = Enum.TryParse<SwitchOutletStatus>(
                    unitResponse.Data.Switches.FirstOrDefault(o => o.Outlet == (int)switchOutlet)?.Switch, true, out var status)
                    ? ((int)status).ToString()
                    : ((int)SwitchOutletStatus.Off).ToString();

                var inchingData = unitResponse.Data.Pulses.FirstOrDefault(o => o.Outlet == switchOutlet);

                result.IsOnline           = true;
                result.IsInInchingMode    = inchingData?.Switch == "on" && inchingData.Pulse == "on";
                result.InchingModeWidthInMs = inchingData?.Width ?? 0;
                result.LastTimeValueSet   = latestValue != sensor.LastReading ? DateTime.Now : sensor.LastTimeValueSet;
                result.LastSeen           = DateTime.Now;
                result.LastReading        = latestValue;
            }

            return result;
        }

        private static SensorConfig ShallowCopy(SensorConfig s) => new SensorConfig
        {
            Id = s.Id, DeviceId = s.DeviceId, SensorId = s.SensorId,
            SwitchNo = s.SwitchNo, UnitId = s.UnitId, Address = s.Address, Port = s.Port,
            DisplayName = s.DisplayName, Url = s.Url, UnitType = s.UnitType,
            SensorType = s.SensorType, Protocol = s.Protocol,
            SyncPeriodicity = s.SyncPeriodicity, EventChangeSync = s.EventChangeSync,
            EventChangeDelta = s.EventChangeDelta, InstalledAt = s.InstalledAt,
            IsActive = s.IsActive, Notes = s.Notes,
            LastReading = s.LastReading, IsOnline = s.IsOnline,
            LastSeen = s.LastSeen, IsInInchingMode = s.IsInInchingMode,
            InchingModeWidthInMs = s.InchingModeWidthInMs, LastTimeValueSet = s.LastTimeValueSet
        };
    }
}

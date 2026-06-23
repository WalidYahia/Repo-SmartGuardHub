using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.SensorConfiguration;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.SystemDevices
{
    public class SonOffMiniR3Switch : ISystemSensor
    {
        private readonly IEnumerable<IDeviceProtocol> _protocols;

        public SonOffMiniR3Switch(IEnumerable<IDeviceProtocol> protocols)
        {
            _protocols = protocols;
        }

        public SensorType SensorType => SensorType.SonOffMiniR3Swich;
        public UnitProtocolType ProtocolType => UnitProtocolType.Rest;

        public DeviceRequest GetOnCommand(string unitId, SwitchOutlet switchNo) =>
            new DeviceRequest
            {
                Deviceid = unitId,
                Data = new DeviceRequestData
                {
                    Switches = new List<SonoffMiniRPayloadDataSwitch>
                    {
                        new SonoffMiniRPayloadDataSwitch { Switch = "on", Outlet = (int)switchNo }
                    }
                }
            };

        public DeviceRequest GetOffCommand(string unitId, SwitchOutlet switchNo) =>
            new DeviceRequest
            {
                Deviceid = unitId,
                Data = new DeviceRequestData
                {
                    Switches = new List<SonoffMiniRPayloadDataSwitch>
                    {
                        new SonoffMiniRPayloadDataSwitch { Switch = "off", Outlet = (int)switchNo }
                    }
                }
            };

        public DeviceRequest GetInfoCommand(string unitId) =>
            new DeviceRequest { Deviceid = unitId, Data = new DeviceRequestData() };

        public DeviceRequest GetOnInchingCommand(string unitId, SwitchOutlet switchNo, int inchingTimeMs, List<SonoffMiniRPayloadDataPulse> pulses)
        {
            foreach (var pulse in pulses)
            {
                if (pulse.Outlet == switchNo)
                {
                    pulse.Switch = "on";
                    pulse.Pulse  = "on";
                    pulse.Width  = inchingTimeMs;
                }
            }
            return new DeviceRequest { Deviceid = unitId, Data = new DeviceRequestData { Pulses = pulses } };
        }

        public DeviceRequest GetOffInchingCommand(string unitId, SwitchOutlet switchNo, List<SonoffMiniRPayloadDataPulse> pulses)
        {
            foreach (var pulse in pulses)
            {
                if (pulse.Outlet == switchNo)
                {
                    pulse.Switch = "off";
                    pulse.Pulse  = "off";
                    pulse.Width  = 0;
                }
            }
            return new DeviceRequest { Deviceid = unitId, Data = new DeviceRequestData { Pulses = pulses } };
        }

        public async Task<GeneralResponse> SendCommandAsync(string destination, string command, object? parameters = null)
        {
            var protocol = _protocols.FirstOrDefault(p => p.ProtocolType == ProtocolType);

            if (SystemManager.IsRaspberryPi)
                destination = destination.Replace(":8081", ".local:8081");

            var result = await protocol.SendCommandAsync(destination, command, parameters);

            return result.State == DeviceResponseState.OK ? ParseResponse(result) : result;
        }

        public GeneralResponse ParseResponse(GeneralResponse deviceResponse)
        {
            var payload = Newtonsoft.Json.JsonConvert.DeserializeObject<SonoffMiniRResponsePayload>(deviceResponse.DevicePayload);

            return payload.Error != 0
                ? new GeneralResponse { State = DeviceResponseState.BadRequest, DevicePayload = payload }
                : new GeneralResponse { State = deviceResponse.State, DevicePayload = payload };
        }

        public async Task<List<SensorPollResult>?> GetReadingsAsync(List<SensorConfig> sensors)
        {
            if (sensors.Count == 0) return null;

            var first    = sensors[0];
            var command  = GetInfoCommand(first.UnitId);
            var response = await SendCommandAsync(first.Url + first.InfoPath, SystemManager.Serialize(command));

            if (response is not { State: DeviceResponseState.OK, DevicePayload: SonoffMiniRResponsePayload payload })
                return null;

            var now = DateTime.UtcNow;
            return sensors.Select(s => MapReading(s, payload, now)).ToList();
        }

        private static SensorPollResult MapReading(SensorConfig sensor, SonoffMiniRResponsePayload payload, DateTime now)
        {
            var result = new SensorPollResult
            {
                SensorId = sensor.Id,
                IsOnline = true,
                ReadingTime = now
            };

            if (payload.Data != null)
            {
                var switchOutlet = (SwitchOutlet)sensor.SwitchNo;
                result.Value = Enum.TryParse<SwitchOutletStatus>(
                    payload.Data.Switches?.FirstOrDefault(o => o.Outlet == (int)switchOutlet)?.Switch, true, out var status)
                    ? ((int)status).ToString()
                    : ((int)SwitchOutletStatus.Off).ToString();
            }

            return result;
        }
    }
}

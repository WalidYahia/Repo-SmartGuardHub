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

        public async Task<SensorConfig> MapRawInfoResponseToSensorConfig(object rawInfoResponse, SensorConfig sensor)
        {
            var unitResponse = rawInfoResponse as SonoffMiniRResponsePayload;
            var result = ShallowCopy(sensor);

            if (unitResponse?.Data != null)
            {
                var switchOutlet = (SwitchOutlet)sensor.SwitchNo;

                var latestValue = Enum.TryParse<SwitchOutletStatus>(
                    unitResponse.Data.Switches?.FirstOrDefault(o => o.Outlet == (int)switchOutlet)?.Switch, true, out var status)
                    ? ((int)status).ToString()
                    : ((int)SwitchOutletStatus.Off).ToString();

                var inchingData = unitResponse.Data.Pulses?.FirstOrDefault(o => o.Outlet == switchOutlet);

                result.IsOnline             = true;
                result.IsInInchingMode      = inchingData?.Switch == "on" && inchingData.Pulse == "on";
                result.InchingModeWidthInMs = inchingData?.Width ?? 0;
                result.LastTimeValueSet     = latestValue != sensor.LastReading ? DateTime.Now : sensor.LastTimeValueSet;
                result.LastSeen             = DateTime.Now;
                result.LastReading          = latestValue;
            }

            return result;
        }

        private static SensorConfig ShallowCopy(SensorConfig s) => new SensorConfig
        {
            Id = s.Id, DeviceId = s.DeviceId, SensorId = s.SensorId,
            SwitchNo = s.SwitchNo, UnitId = s.UnitId, Address = s.Address, Port = s.Port,
            DisplayName = s.DisplayName, Url = s.Url,
            SensorType = s.SensorType, Protocol = s.Protocol,
            DataPath = s.DataPath, InfoPath = s.InfoPath, InchingPath = s.InchingPath,
            SyncPeriodicity = s.SyncPeriodicity, EventChangeSync = s.EventChangeSync,
            EventChangeDelta = s.EventChangeDelta, InstalledAt = s.InstalledAt,
            IsActive = s.IsActive, Notes = s.Notes, LastReading = s.LastReading,
            IsOnline = s.IsOnline, LastSeen = s.LastSeen,
            IsInInchingMode = s.IsInInchingMode, InchingModeWidthInMs = s.InchingModeWidthInMs,
            LastTimeValueSet = s.LastTimeValueSet
        };
    }
}

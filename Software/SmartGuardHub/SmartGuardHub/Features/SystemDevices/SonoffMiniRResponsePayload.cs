using Newtonsoft.Json;
using SmartGuardHub.Features.DeviceManagement;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.SystemDevices
{
    public class SonoffMiniRResponsePayload
    {
        [JsonProperty(PropertyName = "seq")]
        public int Seq { get; set; }

        [JsonProperty(PropertyName = "error")]
        public int Error { get; set; }

        [JsonProperty(PropertyName = "data")]
        public SonoffMiniRPayloadData Data { get; set; }
    }

    public class SonoffMiniRPayloadData
    {
        [JsonProperty(PropertyName = "signalStrength")]
        public int SignalStrength { get; set; }


        [JsonProperty(PropertyName = "switches")]
        public List<SonoffMiniRPayloadDataSwitch> Switches { get; set; }

        [JsonProperty(PropertyName = "configure")]
        public List<SonoffMiniRPayloadDataStartup> Configure { get; set; }

        [JsonProperty(PropertyName = "pulses")]
        public List<SonoffMiniRPayloadDataPulse> Pulses { get; set; }


        [JsonProperty(PropertyName = "sledOnline")]
        public string SledOnline { get; set; }

        [JsonProperty(PropertyName = "fwVersion")]
        public string FwVersion { get; set; }

        [JsonProperty(PropertyName = "staMac")]
        public string StaMac { get; set; }

        [JsonProperty(PropertyName = "rssi")]
        public int Rssi { get; set; }

        [JsonProperty(PropertyName = "bssid")]
        public string Bssid { get; set; }
    }
    public class SonoffMiniRPayloadDataSwitch
    {
        [JsonProperty(PropertyName = "switch")]
        public string Switch { get; set; }

        [JsonProperty(PropertyName = "outlet")]
        public int Outlet { get; set; }
    }
    public class SonoffMiniRPayloadDataStartup
    {
        [JsonProperty(PropertyName = "startup")]
        public string Startup { get; set; }

        [JsonProperty(PropertyName = "outlet")]
        public int Outlet { get; set; }
    }
    public class SonoffMiniRPayloadDataPulse
    {
        [JsonProperty(PropertyName = "pulse")]
        public string Pulse { get; set; }

        [JsonProperty(PropertyName = "switch")]
        public string Switch { get; set; }

        [JsonProperty(PropertyName = "outlet")]
        public SwitchOutlet Outlet { get; set; }

        [JsonProperty(PropertyName = "width")]
        public int Width { get; set; }

        public SonoffMiniRPayloadDataPulse(SwitchOutlet outlet)
        {
            Outlet = outlet;
            Width = 0; // Default width for pulse
            Switch = "off"; // Default Switch for pulse
            Pulse = "off"; // Default pulse state
        }
    }
}

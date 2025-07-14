using Newtonsoft.Json;
using SmartGuardHub.Features.DeviceManagement;

namespace SmartGuardHub.Features.SystemDevices
{
    public class DeviceResponse
    {
        [JsonProperty(PropertyName = "seq")]
        public int Seq { get; set; }

        [JsonProperty(PropertyName = "error")]
        public int Error { get; set; }

        [JsonProperty(PropertyName = "data")]
        public DeviceResponseData Data { get; set; }
    }

    public class DeviceResponseData
    {
        [JsonProperty(PropertyName = "signalStrength")]
        public int SignalStrength { get; set; }


        [JsonProperty(PropertyName = "switches")]
        public List<DeviceDataSwitch> Switches { get; set; }

        [JsonProperty(PropertyName = "configure")]
        public List<DeviceDataStartup> Configure { get; set; }

        [JsonProperty(PropertyName = "pulses")]
        public List<DeviceDataPulse> Pulses { get; set; }


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
    public class DeviceDataSwitch
    {
        [JsonProperty(PropertyName = "switch")]
        public string Switch { get; set; }

        [JsonProperty(PropertyName = "outlet")]
        public int Outlet { get; set; }
    }
    public class DeviceDataStartup
    {
        [JsonProperty(PropertyName = "startup")]
        public string Startup { get; set; }

        [JsonProperty(PropertyName = "outlet")]
        public int Outlet { get; set; }
    }
    public class DeviceDataPulse
    {
        [JsonProperty(PropertyName = "pulse")]
        public string Pulse { get; set; }

        [JsonProperty(PropertyName = "switch")]
        public string Switch { get; set; }

        [JsonProperty(PropertyName = "outlet")]
        public SwitchOutlet Outlet { get; set; }

        [JsonProperty(PropertyName = "width")]
        public int Width { get; set; }

        public DeviceDataPulse(SwitchOutlet outlet)
        {
            Outlet = outlet;
            Width = 0; // Default width for pulse
            Switch = "off"; // Default Switch for pulse
            Pulse = "off"; // Default pulse state
        }
    }
}

using Newtonsoft.Json;

namespace SmartGuardHub.Features.SystemDevices
{
    public class DeviceRequest
    {
        [JsonProperty(PropertyName = "deviceid")]
        public string Deviceid { get; set; }

        [JsonProperty(PropertyName = "data")]
        public DeviceRequestData Data { get; set; }
    }

    public class DeviceRequestData
    {
        [JsonProperty(PropertyName = "switches")]
        public List<DeviceDataSwitch> Switches { get; set; }
    }
}

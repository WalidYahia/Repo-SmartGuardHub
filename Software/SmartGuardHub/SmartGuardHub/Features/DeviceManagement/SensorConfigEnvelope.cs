using SmartGuardHub.Features.SensorConfiguration;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class SensorConfigEnvelope
    {
        public Guid ConfigVersion { get; set; }
        public DateTime UpdateTime { get; set; }
        public List<SensorConfig> Sensors { get; set; } = [];
    }
}

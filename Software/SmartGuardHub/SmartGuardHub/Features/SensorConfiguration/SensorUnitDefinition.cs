using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.SensorConfiguration
{
    public class SensorUnitDefinition
    {
        public string Name { get; set; } = string.Empty;
        public SensorType SensorType { get; set; }
        public int ConnectionProtocol { get; set; }
        public int ProtocolType { get; set; }
        public string BaseUrl { get; set; } = string.Empty;
        public string PortNo { get; set; } = string.Empty;
        public string DataPath { get; set; } = string.Empty;
        public string InfoPath { get; set; } = string.Empty;
        public string InchingPath { get; set; } = string.Empty;
        public int? SyncPeriodicity { get; set; }
        public bool EventChangeSync { get; set; }
        public double? EventChangeDelta { get; set; }
    }
}

namespace SmartGuardHub.Features.DeviceManagement
{
    public class SensorPollResult
    {
        public string SensorId { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public string? Value { get; set; }
        public DateTime ReadingTime { get; set; }
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class SensorReadingJson
    {
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = SensorStatus.Online;

        [JsonPropertyName("readingTime")]
        public DateTime? ReadingTime { get; set; }

        public string Serialize() => JsonSerializer.Serialize(this);

        public static SensorReadingJson? Deserialize(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<SensorReadingJson>(json);
        }
    }

    public static class SensorStatus
    {
        public const string Online  = "online";
        public const string Offline = "offline";
        public const string Error   = "error";
    }
}

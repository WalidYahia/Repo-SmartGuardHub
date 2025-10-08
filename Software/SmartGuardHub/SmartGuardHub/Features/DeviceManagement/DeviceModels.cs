using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Protocols;
using JsonIgnoreAttribute = System.Text.Json.Serialization.JsonIgnoreAttribute;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class Sensor
    {
        public string SensorId { get; set; }

        [Required]
        public string UnitId { get; set; } = string.Empty; // Sonoff device ID

        [Required]
        public int SwitchNo { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Url { get; set; }

        public int Type { get; set; }

        public int Protocol { get; set; }

        public bool IsOnline { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime LastSeen { get; set; }

        public bool IsInInchingMode { get; set; }

        public int InchingModeWidthInMs { get; set; }

        public string? LatestValue { get; set; }

        public string? FwVersion { get; set; }

        // Device-specific properties as JSON
        public string? RawResponse { get; set; }
    }



    // DTOs for API 
    public class SensorDTO
    {
        public string SensorId { get; set; }

        [Required]
        public string UnitId { get; set; } = string.Empty;

        [Required]
        public SwitchOutlet SwitchNo { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Url { get; set; }

        public UnitType Type { get; set; }

        public UnitProtocolType Protocol { get; set; }

        public bool IsOnline { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime LastSeen { get; set; }

        public bool IsInInchingMode { get; set; }

        public int InchingModeWidthInMs { get; set; }

        public object LatestValue { get; set; }

        public string? FwVersion { get; set; }

        public string? RawResponse { get; set; }
    }
}

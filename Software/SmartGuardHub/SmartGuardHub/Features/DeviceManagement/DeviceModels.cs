using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Protocols;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class Device
    {
        public int Id { get; set; }

        [Required]
        public string DeviceId { get; set; } = string.Empty; // Sonoff device ID

        [Required]
        public SwitchNo SwitchNo { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Url { get; set; }

        public DeviceType Type { get; set; }

        public DeviceProtocolType Protocol { get; set; }

        public bool IsOnline { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime LastSeen { get; set; }

        public string? FwVersion { get; set; }

        // Device-specific properties as JSON
        public string? RawResponse { get; set; }
    }



    // DTOs for API 
    public class DeviceDTO
    {
        [Required]
        public string DeviceId { get; set; } = string.Empty;

        [Required]
        public SwitchNo SwitchNo { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Url { get; set; }

        public DeviceType Type { get; set; }

        public DeviceProtocolType Protocol { get; set; }

        public bool IsOnline { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime LastSeen { get; set; }

        public string? FwVersion { get; set; }

        public string? RawResponse { get; set; }
    }
}

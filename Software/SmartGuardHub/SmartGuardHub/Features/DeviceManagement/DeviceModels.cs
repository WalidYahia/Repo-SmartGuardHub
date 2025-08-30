using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Features.Users;
using SmartGuardHub.Protocols;
using JsonIgnoreAttribute = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class Device
    {
        public int Id { get; set; }

        [Required]
        public string DeviceId { get; set; } = string.Empty; // Sonoff device ID

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

        public string? FwVersion { get; set; }

        // Device-specific properties as JSON
        public string? RawResponse { get; set; }

        // Navigation property for Many-to-Many relationship
        public ICollection<User_Device> UserDevices { get; set; } = new List<User_Device>();
    }



    // DTOs for API 
    public class DeviceDTO
    {
        public int Id { get; set; }

        [Required]
        public string DeviceId { get; set; } = string.Empty;

        [Required]
        public SwitchOutlet SwitchNo { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Url { get; set; }

        public DeviceType Type { get; set; }

        public DeviceProtocolType Protocol { get; set; }

        public bool IsOnline { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime LastSeen { get; set; }

        public bool IsInInchingMode { get; set; }

        public int InchingModeWidthInMs { get; set; }

        public string? FwVersion { get; set; }

        public string? RawResponse { get; set; }
    }

    public class ApiSwitchRequest
    {
        public string DeviceId { get; set; }
        public SwitchOutlet SwitchNo { get; set; }
    }
    public class ApiCreateDeviceRequest
    {
        public DeviceType DeviceType { get; set; }
        public string DeviceId { get; set; }
        public SwitchOutlet SwitchNo { get; set; }
        public string Name { get; set; }
    }
    
    public class ApiRenameDeviceRequest
    {
        public string DeviceId { get; set; }
        public SwitchOutlet SwitchNo { get; set; }
        public string Name { get; set; }
    }
    public class ApiEnableInchingModeRequest
    {
        public string DeviceId { get; set; }
        public SwitchOutlet SwitchNo { get; set; }
        public int InchingTimeInMs { get; set; }
    }
    public class ApiDisableInchingModeRequest
    {
        public string DeviceId { get; set; }
        public SwitchOutlet SwitchNo { get; set; }
    }
    public class UnitMqttPayload
    {
        public string UnitId { get; set; }

        public object Value { get; set; }
    }
}

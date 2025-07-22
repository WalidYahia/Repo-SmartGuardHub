using System.ComponentModel.DataAnnotations;
using SmartGuardHub.Infrastructure;

namespace SmartGuardHub.Features.Logging
{
    public class SystemLog
    {
        public int Id { get; set; }

        [Required]
        public DateTime LogTime { get; set; } = SystemManager.TimeNow();

        [Required]
        public string Level { get; set; }

        [Required]
        public string MessageKey { get; set; }

        public string Message { get; set; }
        public string? Exception { get; set; }
    }
}

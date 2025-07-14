using System.ComponentModel.DataAnnotations;
using SmartGuardHub.Features.DeviceManagement;

namespace SmartGuardHub.Features.Users
{
    public class User_Device
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int DeviceId { get; set; }

        // Navigation properties
        public User User { get; set; }
        public Device Device { get; set; }
    }
}

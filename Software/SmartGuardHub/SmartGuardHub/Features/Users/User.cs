using System.ComponentModel.DataAnnotations;

namespace SmartGuardHub.Features.Users
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property for Many-to-Many relationship
        public ICollection<User_Device> UserDevices { get; set; } = new List<User_Device>();
    }
}

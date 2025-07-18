using SmartGuardHub.Features.DeviceManagement;

namespace SmartGuardHub.Infrastructure
{
    public class SystemManager
    {
        public static List<DeviceDTO> Devices = new List<DeviceDTO>();

        public static DateTime TimeNow()
        {
            return DateTime.UtcNow;
        }
    }
}

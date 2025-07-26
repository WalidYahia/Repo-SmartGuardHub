using System.Collections.Concurrent;
using SmartGuardHub.Features.DeviceManagement;

namespace SmartGuardHub.Infrastructure
{
    public class SystemManager
    {
        //public List<DeviceDTO> Devices = new List<DeviceDTO>();
        public static ConcurrentBag<DeviceDTO> Devices = new ConcurrentBag<DeviceDTO>();


        public static DateTime TimeNow()
        {
            return DateTime.UtcNow;
        }

        public static string GetRecursiveMessagesWithStack(Exception ex)
        {
            if (ex == null) return string.Empty;

            var messages = new List<string>();
            Exception last = ex;

            while (ex != null)
            {
                messages.Add(ex.Message);
                last = ex;
                ex = ex.InnerException;
            }

            return string.Join(" --> ", messages) + Environment.NewLine + last.StackTrace;
        }
    }
}

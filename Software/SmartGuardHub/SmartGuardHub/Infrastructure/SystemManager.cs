using System.Collections.Concurrent;
using SmartGuardHub.Features.DeviceManagement;

namespace SmartGuardHub.Infrastructure
{
    public class SystemManager
    {
        public static string DeviceId = "SmartGuard-" + GetCpuSerial();

        //public List<DeviceDTO> Devices = new List<DeviceDTO>();
        public static ConcurrentBag<SensorDTO> InstalledSensors = new ConcurrentBag<SensorDTO>();

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

        public static string GetMqttTopicPath(string topicName)
        {
            return DeviceId + "/" + topicName;
        }

        private static string GetCpuSerial()
        {
            return "123";
        }
    }

    public class MqttTopics
    {
        /// <summary>
        /// Publish from Device, Subscribe from Mobile
        /// </summary>
        public const string InstalledDevices = "InstalledDevices";


        /// <summary>
        /// Publish from Device, Subscribe from Mobile
        /// </summary>
        public const string DeviceDataTopic = "DeviceData";

        /// <summary>
        /// Publish from Mobile, Subscribe from Device
        /// </summary>
        public const string RemoteActionTopic_Publish = "RemoteAction";

        /// <summary>
        /// Publish from Device, Subscribe from Mobile
        /// </summary>
        public const string RemoteActionTopic_Ack = "RemoteAction_Ack";

        /// <summary>
        /// Publish from Cloud, Subscribe from Device
        /// </summary>
        public const string RemoteUpdateTopic_Publish = "RemoteUpdate";

        /// <summary>
        /// Publish from Device, Subscribe from Cloud
        /// </summary>
        public const string RemoteUpdateTopic_Ack = "RemoteUpdate_Ack";
    }
}

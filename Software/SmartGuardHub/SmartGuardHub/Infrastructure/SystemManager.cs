using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SmartGuardHub.Features.DeviceManagement;

namespace SmartGuardHub.Infrastructure
{
    public class SystemManager
    {
        public static string DeviceId { get; set; }

        public static bool IsOnPi { get; set; }

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

        public static void InitSystemEnvironment()
        {
            // Linux or Windows
            CheckEnvironment();

            // Get CpuSerial
            string cpuSerial = GetCpuSerial();

            // Set DeviceId
            DeviceId = "SmartGuard-" + cpuSerial;

            // Set Hostname
            SetHostname(DeviceId);
        }

        public static void CheckEnvironment()
        {
            IsOnPi = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            Console.WriteLine($"++++++++ Environment: {IsOnPi} ++++++++");
        }
        private static string GetCpuSerial()
        {
            string cpuSerial = "unknownCpuSerial";

            if (IsOnPi)
            {
                try
                {
                    var cpuInfo = File.ReadAllText("/proc/cpuinfo");
                    foreach (var line in cpuInfo.Split('\n'))
                    {
                        if (line.StartsWith("Serial"))
                        {
                            var parts = line.Split(':');
                            if (parts.Length > 1)
                                cpuSerial = parts[1].Trim();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"++++++++ Failed to get CPU serial: {ex.Message} ++++++++");
                }
            }
            else
            {
                cpuSerial = Environment.MachineName;
            }

            Console.WriteLine($"++++++++ CPU serial: {cpuSerial} ++++++++");

            return cpuSerial;
        }
        private static void SetHostname(string newHostname)
        {
            if(IsOnPi)
            {
                try
                {
                    // Update the hostname file (persistent)
                    File.WriteAllText("/etc/hostname", newHostname + "\n");

                    // Update /etc/hosts entry for localhost
                    var hosts = File.ReadAllText("/etc/hosts");
                    hosts = System.Text.RegularExpressions.Regex.Replace(
                        hosts,
                        @"127\.0\.1\.1\s+\S+",
                        $"127.0.1.1\t{newHostname}"
                    );
                    File.WriteAllText("/etc/hosts", hosts);

                    // Apply immediately (temporary until reboot)
                    RunCommand($"hostnamectl set-hostname {newHostname}");

                    Console.WriteLine($"++++++++ Hostname changed to: {newHostname} ++++++++");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"++++++++ Failed to set hostname: {ex.Message} ++++++++");
                }
            }
        }
        private static void RunCommand(string cmd)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"{cmd}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
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

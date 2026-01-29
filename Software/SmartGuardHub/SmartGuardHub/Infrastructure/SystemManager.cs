using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SmartGuardHub.Features.DeviceManagement;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace SmartGuardHub.Infrastructure
{
    public class SystemManager
    {
        public static string DeviceId { get; set; }

        public static bool IsRaspberryPi { get; set; }

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

        public static async Task InitSystemEnvironment()
        {
            // Linux or Windows
            CheckEnvironment();

            // Get CpuSerial
            string cpuSerial = GetCpuSerial();

            // Set DeviceId
            DeviceId = "SmartGuard-" + cpuSerial;

            // Set Hostname
            await SetHostname(DeviceId);
        }

        public static void CheckEnvironment()
        {
            IsRaspberryPi = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            Console.WriteLine($"++++++++ Environment: {IsRaspberryPi} ++++++++");
        }
        private static string GetCpuSerial()
        {
            string cpuSerial = "unknownCpuSerial";

            if (IsRaspberryPi)
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
        private static async Task SetHostname(string newHostname)
        {
            if(IsRaspberryPi)
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
                    await ExecuteCommandAsync($"hostnamectl set-hostname {newHostname}");

                    Console.WriteLine($"++++++++ Hostname changed to: {newHostname} ++++++++");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"++++++++ Failed to set hostname: {ex.Message} ++++++++");
                }
            }
        }
        public static async Task<string> ExecuteCommandAsync(string cmd, bool ignoreErrors = false)
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

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();


            if (process.ExitCode != 0 && !ignoreErrors)
            {
                throw new Exception($"Command failed: {error}");
            }

            return output;
        }

        public static async Task WriteFileAsync(string directory, string fileName, string content, bool writeSafely = true)
        {
            // Ensure target directory exists
            Directory.CreateDirectory(directory);

            if (!writeSafely)
            {
                await File.WriteAllTextAsync(Path.Combine(directory, fileName), content);
                return;
            }

            // Create temp file in the SAME directory to ensure atomic rename
            string targetPath = Path.Combine(directory, fileName);
            string tempPath = Path.Combine(directory, $"{fileName}.tmp_{Guid.NewGuid():N}");

            try
            {
                await File.WriteAllTextAsync(tempPath, content, Encoding.UTF8);

                // Atomic move (on same filesystem)
                File.Move(tempPath, targetPath, overwrite: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing file '{targetPath}': {ex.Message}");
                throw;
            }
            finally
            {
                // Clean up in case move failed
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }
        public static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(
                obj,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
        }
    }

    public class MqttTopics
    {
        /// <summary>
        /// Publish from Device, Subscribe from Mobile
        /// </summary>
        public const string InstalledUnits = "InstalledUnits";


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

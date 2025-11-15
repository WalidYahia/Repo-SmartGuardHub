using SmartGuardHub.Infrastructure;
using System.Text;

namespace SmartGuardHub.Network
{
    public class NetworkConfigurationManager
    {
        private const string DEFAULT_LAN_IP = "10.0.0.1";
        private const string DEFAULT_WIFI_IP = "20.0.0.1";
        private const string DEFAULT_SSID = "SmartHomeHub-Setup";
        private const string DEFAULT_AP_PASSWORD = "12345678";

        public NetworkConfigurationManager()
        {
            // Ensure NetworkManager is installed
            VerifyNetworkManager();
        }

        private void VerifyNetworkManager()
        {
            var result = SystemManager.ExecuteCommandAsync("systemctl is-active NetworkManager", ignoreErrors: true).Result;
            if (!result.Trim().Equals("active", StringComparison.OrdinalIgnoreCase))
                throw new Exception("NetworkManager is not active. Please ensure it is installed and enabled.");
        }

        /// <summary>
        /// Enables setup mode: Raspberry Pi acts as Access Point + LAN static IP.
        /// </summary>
        public async Task<bool> EnableSetupModeAsync()
        {
            try
            {
                Console.WriteLine("=== Enabling Setup Mode ===");

                // Stop conflicting services
                await SystemManager.ExecuteCommandAsync("sudo systemctl stop dnsmasq", ignoreErrors: true);
                await SystemManager.ExecuteCommandAsync("sudo systemctl stop hostapd", ignoreErrors: true);

                // Disconnect current connections
                await SystemManager.ExecuteCommandAsync("sudo nmcli dev disconnect wlan0", ignoreErrors: true);
                await SystemManager.ExecuteCommandAsync("sudo nmcli dev disconnect eth0", ignoreErrors: true);

                // Create LAN static IP
                await ConfigureStaticIPAsync("eth0", DEFAULT_LAN_IP, gateway: null);

                // Create Access Point on wlan0
                await ConfigureAccessPointAsync(DEFAULT_SSID, DEFAULT_AP_PASSWORD, DEFAULT_WIFI_IP);

                Console.WriteLine("=== Setup Mode Enabled ===");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enabling setup mode: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Applies user’s chosen configuration (LAN or WiFi client).
        /// </summary>
        public async Task<bool> ApplyUserConfigurationAsync(NetworkConfig config)
        {
            try
            {
                Console.WriteLine("=== Applying User Configuration ===");

                // Remove setup AP if exists
                await SystemManager.ExecuteCommandAsync("sudo nmcli connection delete wifi-ap", ignoreErrors: true);
                await SystemManager.ExecuteCommandAsync("sudo nmcli connection delete lan-setup", ignoreErrors: true);

                if (config.NetworkMode == NetworkMode.LAN)
                {
                    if (config.UseDHCP)
                        await ConfigureDHCPAsync("eth0");
                    else
                        await ConfigureStaticIPAsync("eth0", config.StaticIP!, config.Gateway);
                }
                else if (config.NetworkMode == NetworkMode.WIFI && !string.IsNullOrEmpty(config.WiFiSSID))
                {
                    await ConfigureWiFiClientAsync(config.WiFiSSID!, config.WiFiPassword!, config.UseDHCP, config.StaticIP, config.Gateway);
                }

                Console.WriteLine("=== User Configuration Applied ===");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying user configuration: {ex.Message}");
                return false;
            }
        }

        // ──────────────────────────────────────────────────────────────
        // NMCLI BASED METHODS
        // ──────────────────────────────────────────────────────────────

        private async Task ConfigureStaticIPAsync(string interfaceName, string ipAddress, string? gateway)
        {
            gateway ??= $"{ipAddress.Split('.')[0]}.{ipAddress.Split('.')[1]}.{ipAddress.Split('.')[2]}.1";

            string connectionName = $"{interfaceName}-static";

            // Delete any previous connection for the same interface
            await SystemManager.ExecuteCommandAsync($"sudo nmcli connection delete {connectionName}", ignoreErrors: true);

            // Create and apply static configuration
            await SystemManager.ExecuteCommandAsync(
                $"sudo nmcli connection add type ethernet ifname {interfaceName} con-name {connectionName} " +
                $"ipv4.addresses {ipAddress}/24 ipv4.gateway {gateway} ipv4.dns '8.8.8.8 8.8.4.4' ipv4.method manual");

            await SystemManager.ExecuteCommandAsync($"sudo nmcli connection up {connectionName}");
        }

        private async Task ConfigureDHCPAsync(string interfaceName)
        {
            string connectionName = $"{interfaceName}-dhcp";

            await SystemManager.ExecuteCommandAsync($"sudo nmcli connection delete {connectionName}", ignoreErrors: true);
            await SystemManager.ExecuteCommandAsync(
                $"sudo nmcli connection add type ethernet ifname {interfaceName} con-name {connectionName} ipv4.method auto");

            await SystemManager.ExecuteCommandAsync($"sudo nmcli connection up {connectionName}");
        }

        //private async Task ConfigureAccessPointAsync(string ssid, string password, string ipAddress)
        //{
        //    string connectionName = "wifi-ap";

        //    // Delete existing AP connection
        //    await SystemManager.ExecuteCommandAsync($"sudo nmcli connection delete {connectionName}", ignoreErrors: true);

        //    // Create AP with static IP
        //    await SystemManager.ExecuteCommandAsync(
        //        $"sudo nmcli connection add type wifi ifname wlan0 con-name {connectionName} autoconnect yes " +
        //        $"ssid '{ssid}' mode ap ipv4.addresses {ipAddress}/24 ipv4.method manual");

        //    await SystemManager.ExecuteCommandAsync(
        //        $"sudo nmcli connection modify {connectionName} wifi-sec.key-mgmt wpa-psk wifi-sec.psk '{password}'");

        //    await SystemManager.ExecuteCommandAsync($"sudo nmcli connection up {connectionName}");
        //}
        private async Task ConfigureAccessPointAsync(string ssid, string password, string ipAddress)
        {
            string connectionName = "wifi-ap";

            // Delete existing AP connection if exists
            await SystemManager.ExecuteCommandAsync($"sudo nmcli connection delete {connectionName}", ignoreErrors: true);

            // Disconnect from any other Wi-Fi network (e.g., previous 'walid' network)
            await SystemManager.ExecuteCommandAsync($"sudo nmcli device disconnect wlan0", ignoreErrors: true);

            // Create AP with shared IPv4 (DHCP + NAT)
            await SystemManager.ExecuteCommandAsync(
                $"sudo nmcli connection add type wifi ifname wlan0 con-name {connectionName} autoconnect yes " +
                $"ssid '{ssid}' mode ap ipv4.addresses {ipAddress}/24 ipv4.method shared");

            // Configure Wi-Fi security and band
            await SystemManager.ExecuteCommandAsync(
                $"sudo nmcli connection modify {connectionName} " +
                $"802-11-wireless.band bg 802-11-wireless.channel 6 " +
                $"wifi-sec.key-mgmt wpa-psk wifi-sec.psk '{password}'");

            // Bring up the connection
            await SystemManager.ExecuteCommandAsync($"sudo nmcli connection up {connectionName}");
        }



        private async Task ConfigureWiFiClientAsync(string ssid, string password, bool useDHCP, string? staticIP, string? gateway)
        {
            string connectionName = "wifi-client";

            await SystemManager.ExecuteCommandAsync($"sudo nmcli connection delete {connectionName}", ignoreErrors: true);

            if (useDHCP)
            {
                await SystemManager.ExecuteCommandAsync(
                    $"sudo nmcli dev wifi connect '{ssid}' password '{password}' ifname wlan0 name {connectionName}");
            }
            else
            {
                gateway ??= $"{staticIP?.Split('.')[0]}.{staticIP?.Split('.')[1]}.{staticIP?.Split('.')[2]}.1";

                await SystemManager.ExecuteCommandAsync(
                    $"sudo nmcli connection add type wifi ifname wlan0 con-name {connectionName} ssid '{ssid}' " +
                    $"wifi-sec.key-mgmt wpa-psk wifi-sec.psk '{password}' ipv4.addresses {staticIP}/24 ipv4.gateway {gateway} " +
                    $"ipv4.dns '8.8.8.8 8.8.4.4' ipv4.method manual");

                await SystemManager.ExecuteCommandAsync($"sudo nmcli connection up {connectionName}");
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    // SUPPORTING CLASSES
    // ──────────────────────────────────────────────────────────────

    public class NetworkConfig
    {
        public NetworkMode NetworkMode { get; set; }
        public bool UseDHCP { get; set; }
        public string? StaticIP { get; set; }
        public string? Gateway { get; set; }
        public string? SubnetMask { get; set; }
        public string? WiFiSSID { get; set; }
        public string? WiFiPassword { get; set; }
    }

    public enum NetworkMode
    {
        LAN,
        WIFI
    }
}

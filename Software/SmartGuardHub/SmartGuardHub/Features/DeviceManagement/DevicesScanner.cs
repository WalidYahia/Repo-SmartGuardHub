using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SensorConfiguration;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class DevicesScanner : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DevicesScanner(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var scanned = await ScanForConfiguredUnits();

                    if (!ModelsAreTheSame(SystemManager.InstalledSensors, scanned))
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var deviceService = scope.ServiceProvider.GetRequiredService<DeviceService>();
                        await deviceService.UpdateListDeviceAsync(scanned);
                        await deviceService.RefreshDevices();
                    }
                }
                catch (Exception ex)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var loggingService = scope.ServiceProvider.GetRequiredService<LoggingService>();
                    await loggingService.LogErrorAsync(LogMessageKey.ScanDevicesError, "Failed to scan devices", ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
        }

        private bool ModelsAreTheSame(List<SensorConfig> current, List<SensorConfig> scanned) =>
            SystemManager.Serialize(current) == SystemManager.Serialize(scanned);

        private async Task<List<SensorConfig>> ScanForConfiguredUnits()
        {
            var scanned = new List<SensorConfig>();
            var units = SystemManager.InstalledSensors.GroupBy(s => s.UnitId).ToList();

            foreach (var unit in units)
            {
                var sensors = unit.ToList();
                try
                {
                    var systemDevice = await GetSystemUnit(sensors[0].UnitType);
                    var command = systemDevice.GetInfoCommand(unit.Key);
                    var response = await systemDevice.SendCommandAsync(sensors[0].Url + systemDevice.InfoPath, SystemManager.Serialize(command));

                    if (response?.DevicePayload != null)
                    {
                        foreach (var sensor in sensors)
                            scanned.Add(await systemDevice.MapRawInfoResponseToSensorConfig(response.DevicePayload, sensor));
                    }
                }
                catch (Exception ex)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var loggingService = scope.ServiceProvider.GetRequiredService<LoggingService>();
                    await loggingService.LogErrorAsync(LogMessageKey.ScanDevicesError, $"Failed to scan unit {sensors[0].UnitId}", ex);
                }
            }

            return scanned;
        }

        private async Task<ISystemUnit> GetSystemUnit(UnitType unitType)
        {
            using var scope = _scopeFactory.CreateScope();
            var units = scope.ServiceProvider.GetRequiredService<IEnumerable<ISystemUnit>>();
            return units.FirstOrDefault(d => d.UnitType == unitType);
        }
    }
}

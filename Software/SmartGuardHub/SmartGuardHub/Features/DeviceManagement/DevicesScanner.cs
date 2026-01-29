using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Features.UserCommands;
using SmartGuardHub.Infrastructure;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                    var scannedUnits = await ScanForConfiguredUnits();

                    bool modelsAreTheSame = await ModelsAreTheSame(SystemManager.InstalledSensors, scannedUnits);

                    if (!modelsAreTheSame)
                    {
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var deviceService = scope.ServiceProvider.GetRequiredService<DeviceService>();

                            await deviceService.UpdateListDeviceAsync(scannedUnits);

                            await deviceService.RefreshDevices();
                        }
                    }
                }
                catch (Exception ex)
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var loggingService = scope.ServiceProvider.GetRequiredService<LoggingService>();

                        await loggingService.LogErrorAsync(LogMessageKey.ScanDevicesError, "Failed to scan devices", ex);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
        }


        private async Task<bool> ModelsAreTheSame(ConcurrentBag<SensorDTO> current, List<SensorDTO> scanned)
        {
            var koko = SystemManager.Serialize(current);
            var koko2 = SystemManager.Serialize(scanned);

            return
                SystemManager.Serialize(current) ==
                SystemManager.Serialize(scanned);
        }

        private async Task<List<SensorDTO>> ScanForConfiguredUnits()
        {
            List<SensorDTO> scannedUnits = new List<SensorDTO>();

            var installedUnits = SystemManager.InstalledSensors.GroupBy(s => s.UnitId).ToList();

            foreach (var unit in installedUnits)
            {
                var sensors = unit.ToList();

                try
                {
                    var systemDevice = await GetSystemUnit(sensors[0].Type);

                    var command = systemDevice.GetInfoCommand(unit.Key);

                    string jsonString = SystemManager.Serialize(command);

                    var response = await systemDevice.SendCommandAsync(sensors[0].Url + systemDevice.InfoPath, jsonString);

                    if (response != null && response.DevicePayload != null)
                    {
                        foreach (var sensor in sensors)
                        {
                            scannedUnits.Add(await systemDevice.MapRawInfoResponseToSensorDto(response.DevicePayload, sensor));
                        }
                    }
                }
                catch (Exception ex)
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var loggingService = scope.ServiceProvider.GetRequiredService<LoggingService>();

                        await loggingService.LogErrorAsync(LogMessageKey.ScanDevicesError, $"Failed to scan device, {sensors[0].UnitId}", ex);
                    }
                }
            }

            return scannedUnits;
        }

        private async Task<ISystemUnit> GetSystemUnit(UnitType unitType)
        {
            using var scope = _scopeFactory.CreateScope();
            var units = scope.ServiceProvider.GetRequiredService<IEnumerable<ISystemUnit>>();        
            return units.FirstOrDefault(d => d.DeviceType == unitType);
        }   
    }

    public class ScannedResponseModel
    {
        public UnitType UnitType { get; set; }

        public GeneralResponse GeneralResponse { get; set; }
    }
}

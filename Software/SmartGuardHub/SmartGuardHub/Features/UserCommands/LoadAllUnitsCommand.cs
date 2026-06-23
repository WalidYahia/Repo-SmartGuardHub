using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SensorConfiguration;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;

namespace SmartGuardHub.Features.UserCommands
{
    public class LoadAllUnitsCommand : UserCommand
    {
        private readonly ISensorReadingRepository _readingRepo;

        public LoadAllUnitsCommand(
            IEnumerable<ISystemSensor> systemSensors,
            LoggingService loggingService,
            DeviceService deviceService,
            ISensorReadingRepository readingRepo)
            : base(systemSensors, loggingService, deviceService)
        {
            _readingRepo = readingRepo;
            jsonCommandType = Enums.JsonCommandType.LoaddAllSensors;
        }

        protected override async Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand)
        {
            var sensors = SystemManager.InstalledSensors;
            var readings = await _readingRepo.GetLatestBatchAsync(sensors.Select(s => s.Id));

            var payload = sensors.Select(sensor =>
            {
                readings.TryGetValue(sensor.Id, out var record);
                var reading = SensorReadingJson.Deserialize(record?.Reading);
                return new
                {
                    id                   = sensor.Id,
                    deviceId             = sensor.DeviceId,
                    sensorId             = sensor.SensorId,
                    unitId               = sensor.UnitId,
                    displayName          = sensor.DisplayName,
                    sensorType           = sensor.SensorType,
                    isInInchingMode      = sensor.IsInInchingMode,
                    inchingModeWidthInMs = sensor.InchingModeWidthInMs,
                    lastReading          = reading?.Value,
                    lastSeen             = reading?.ReadingTime,
                };
            }).ToList();

            return new GeneralResponse { State = DeviceResponseState.OK, DevicePayload = payload };
        }

        protected override async Task<bool> RequestIsValid(JsonCommand jsonCommand) => true;
    }
}

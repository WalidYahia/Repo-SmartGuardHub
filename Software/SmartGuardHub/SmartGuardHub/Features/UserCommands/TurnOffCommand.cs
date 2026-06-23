using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SensorConfiguration;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserCommands
{
    public class TurnOffCommand : UserCommand
    {
        private readonly ISensorReadingRepository _readingRepo;

        public TurnOffCommand(
            IEnumerable<ISystemSensor> systemSensors,
            LoggingService loggingService,
            DeviceService deviceService,
            ISensorReadingRepository readingRepo)
            : base(systemSensors, loggingService, deviceService)
        {
            _readingRepo = readingRepo;
            jsonCommandType = Enums.JsonCommandType.TurnOff;
        }

        protected override async Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand)
        {
            var sensor = LoadInstalledSensor(jsonCommand.CommandPayload.InstalledSensorId);

            if (sensor != null)
            {
                var systemSensor = LoadSystemSensor(sensor.SensorType);
                var command = systemSensor.GetOffCommand(sensor.UnitId, (SwitchOutlet)sensor.SwitchNo);
                var result  = await systemSensor.SendCommandAsync(sensor.Url + sensor.DataPath, SystemManager.Serialize(command));

                if (result.State == DeviceResponseState.OK)
                {
                    var now = DateTime.UtcNow;
                    var previous = await _readingRepo.GetLatestAsync(sensor.Id);
                    var newValue = ((int)SwitchOutletStatus.Off).ToString();

                    await _readingRepo.SaveAsync(new SensorReadingRecord
                    {
                        UnitId        = sensor.UnitId,
                        SensorId      = sensor.Id,
                        LogTime       = now,
                        Reading       = new SensorReadingJson { Value = newValue, Status = SensorStatus.Online, ReadingTime = now }.Serialize(),
                        IsOnline      = true,
                        SyncedToCloud = jsonCommand.Source == ConfigSource.Cloud || !sensor.EventChangeSync,
                        UpdatedFrom   = jsonCommand.Source
                    });

                    result.DevicePayload = new
                    {
                        id = sensor.Id,
                        deviceId = sensor.DeviceId,
                        sensorId = sensor.SensorId,
                        unitId = sensor.UnitId,
                        displayName = sensor.DisplayName,
                        sensorType = sensor.SensorType,
                        isInInchingMode = sensor.IsInInchingMode,
                        inchingModeWidthInMs = sensor.InchingModeWidthInMs,
                        lastReading = newValue,
                        lastSeen = now,
                    };
                }

                return result;
            }

            await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"TurnOff - Sensor {jsonCommand.CommandPayload.InstalledSensorId} not found.");
            return new GeneralResponse { State = DeviceResponseState.NotFound };
        }
    }
}

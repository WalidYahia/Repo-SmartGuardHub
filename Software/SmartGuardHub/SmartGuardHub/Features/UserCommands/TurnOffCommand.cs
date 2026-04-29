using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserCommands
{
    public class TurnOffCommand : UserCommand
    {
        public TurnOffCommand(IEnumerable<ISystemUnit> systemUnits, LoggingService loggingService, DeviceService deviceService)
            : base(systemUnits, loggingService, deviceService)
        {
            jsonCommandType = Enums.JsonCommandType.TurnOff;
        }

        protected override async Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand)
        {
            var sensor = LoadInstalledSensor(jsonCommand.CommandPayload.InstalledSensorId);

            if (sensor != null)
            {
                var systemDevice = await LoadSystemUnit(sensor.UnitType);
                var command = systemDevice.GetOffCommand(sensor.UnitId, (SwitchOutlet)sensor.SwitchNo);
                var result = await systemDevice.SendCommandAsync(sensor.Url + systemDevice.DataPath, SystemManager.Serialize(command));

                if (result.State == DeviceResponseState.OK)
                {
                    sensor.LastReading      = ((int)SwitchOutletStatus.Off).ToString();
                    sensor.LastSeen         = DateTime.Now;
                    sensor.LastTimeValueSet = DateTime.Now;

                    result.DevicePayload = sensor;

                    _deviceService.UpdateDeviceAsync(sensor);
                    _deviceService.RefreshDevices();
                }

                return result;
            }

            await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"TurnOff - Sensor {jsonCommand.CommandPayload.InstalledSensorId} not found.");
            return new GeneralResponse { State = DeviceResponseState.NotFound, DevicePayload = "Device not found" };
        }
    }
}

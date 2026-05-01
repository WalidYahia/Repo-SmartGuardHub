using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserCommands
{
    public class TurnOnCommand : UserCommand
    {
        public TurnOnCommand(IEnumerable<ISystemSensor> systemSensors, LoggingService loggingService, DeviceService deviceService)
            : base(systemSensors, loggingService, deviceService)
        {
            jsonCommandType = Enums.JsonCommandType.TurnOn;
        }

        protected override async Task<GeneralResponse> ExecuteAsync(JsonCommand jsonCommand)
        {
            var sensor = LoadInstalledSensor(jsonCommand.CommandPayload.InstalledSensorId);

            if (sensor != null)
            {
                var systemSensor = LoadSystemSensor(sensor.SensorType);
                var command = systemSensor.GetOnCommand(sensor.UnitId, (SwitchOutlet)sensor.SwitchNo);
                var result  = await systemSensor.SendCommandAsync(sensor.Url + sensor.DataPath, SystemManager.Serialize(command));

                if (result.State == DeviceResponseState.OK)
                {
                    sensor.LastReading      = ((int)SwitchOutletStatus.On).ToString();
                    sensor.LastSeen         = DateTime.Now;
                    sensor.LastTimeValueSet = DateTime.Now;
                    result.DevicePayload    = sensor;

                    await _deviceService.UpdateDeviceAsync(sensor);
                }

                return result;
            }

            await _loggingService.LogTraceAsync(LogMessageKey.DevicesController, $"TurnOn - Sensor {jsonCommand.CommandPayload.InstalledSensorId} not found.");
            return new GeneralResponse { State = DeviceResponseState.NotFound, DevicePayload = "Device not found" };
        }
    }
}

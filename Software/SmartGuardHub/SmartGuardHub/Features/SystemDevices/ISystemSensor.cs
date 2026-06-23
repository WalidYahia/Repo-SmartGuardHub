using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.SensorConfiguration;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.SystemDevices
{
    public interface ISystemSensor
    {
        SensorType SensorType { get; }
        UnitProtocolType ProtocolType { get; }

        Task<GeneralResponse> SendCommandAsync(string destination, string command, object? parameters = null);

        DeviceRequest GetOnCommand(string unitId, SwitchOutlet switchNo);
        DeviceRequest GetOffCommand(string unitId, SwitchOutlet switchNo);
        DeviceRequest GetInfoCommand(string unitId);
        DeviceRequest GetOnInchingCommand(string unitId, SwitchOutlet switchNo, int inchingTimeMs, List<SonoffMiniRPayloadDataPulse> pulses);
        DeviceRequest GetOffInchingCommand(string unitId, SwitchOutlet switchNo, List<SonoffMiniRPayloadDataPulse> pulses);

        GeneralResponse ParseResponse(GeneralResponse deviceResponse);

        /// <summary>
        /// Polls the physical unit, maps the response for every sensor in the list,
        /// and returns one result per sensor. Returns null when the unit is unreachable.
        /// </summary>
        Task<List<SensorPollResult>?> GetReadingsAsync(List<SensorConfig> sensors);
    }
}

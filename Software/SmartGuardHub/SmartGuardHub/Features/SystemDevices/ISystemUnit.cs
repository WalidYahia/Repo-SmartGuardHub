using System.Net.Sockets;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.SystemDevices
{
    public interface ISystemUnit
    {
        UnitType DeviceType { get; }
        UnitProtocolType ProtocolType { get; }

        string BaseUrl { get; }
        string PortNo { get; }
        public string DataPath { get; }
        public string InfoPath { get; }
        public string InchingPath { get; }

        public Task<GeneralResponse> SendCommandAsync(string destination, string command, object? parameters = null);

        public DeviceRequest GetOnCommand(string deviceId, SwitchOutlet switchNo);
        public DeviceRequest GetOffCommand(string deviceId, SwitchOutlet switchNo);
        public DeviceRequest GetInfoCommand(string deviceId);
        public DeviceRequest GetSignalStrengthCommand(string deviceId);
        public DeviceRequest GetOnInchingCommand(string deviceId, SwitchOutlet switchNo, int InchingTime, List<SonoffMiniRPayloadDataPulse> devicePulses);
        public DeviceRequest GetOffInchingCommand(string deviceId, SwitchOutlet switchNo, List<SonoffMiniRPayloadDataPulse> devicePulses);

        public string GetDeviceUrl(string deviceId);
        public UnitProtocolType GetDeviceProtocol();
        public GeneralResponse ParseResponse(GeneralResponse deviceResponse);
        //public Task<SensorDTO_Mini> MapRawInfoResponseToDtoMini(object rawInfoResponse, SensorDTO sensorDTO);
        public Task<SensorDTO> MapRawInfoResponseToSensorDto(object rawInfoResponse, SensorDTO sensorDTO);
    }
}

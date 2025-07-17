namespace SmartGuardHub.Features.SystemDevices
{
    public class DeviceResponse
    {
        public DeviceResponseState State { get; set; } = DeviceResponseState.Error;

        public dynamic DevicePayload { get; set; }
    }

    public enum DeviceResponseState
    {
        OK = 0,
        Error = 1,
        NotFound = 2,
        Timeout = 3,
        BadRequest = 4,
    }
}

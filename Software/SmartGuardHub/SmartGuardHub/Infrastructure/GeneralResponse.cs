namespace SmartGuardHub.Infrastructure
{
    public class GeneralResponse
    {
        /// <summary>
        /// A unique identifier for the request
        /// For mobile app mqtt-subscribe (each mobile app process only recieved ack of its actions).
        /// </summary>
        public string RequestId { get; set; }

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
        DeviceDataIsRequired = 5,
        DeviceAlreadyRegistered = 6,
        DeviceNameAlreadyRegistered = 7,
        Conflict = 8,
        InchingIntervalValidationError = 9,
        EmptyPayload = 10,
        NoContent = 11,
    }
}

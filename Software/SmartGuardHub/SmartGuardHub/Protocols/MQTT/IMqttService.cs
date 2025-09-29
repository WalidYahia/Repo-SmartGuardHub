namespace SmartGuardHub.Protocols.MQTT
{
    public interface IMqttService
    {
        Task StartAsync();
        Task DisconnectAsync();
        Task PublishAsync(string topic, object message, bool retainFlag);
        Task SubscribeAsync(string topic);

        Task ConnectAsync(int trialsCount);

        public event Func<MqttMessageModel, Task> ProcessMessageReceived;
    }
}

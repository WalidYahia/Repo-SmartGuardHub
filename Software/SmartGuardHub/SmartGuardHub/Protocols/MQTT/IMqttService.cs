namespace SmartGuardHub.Protocols.MQTT
{
    public interface IMqttService
    {
        Task StartAsync();
        Task DisconnectAsync();
        Task<bool> PublishAsync(string topic, object message, bool retainFlag, MQTTnet.Protocol.MqttQualityOfServiceLevel qos, bool serialize = true);
        Task SubscribeAsync(string topic);

        Task ConnectAsync(int trialsCount);

        public event Func<MqttMessageModel, Task> ProcessMessageReceived;
    }
}

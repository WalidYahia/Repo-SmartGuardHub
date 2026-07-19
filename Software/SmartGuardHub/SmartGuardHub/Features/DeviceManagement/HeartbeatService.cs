using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols.MQTT;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class HeartbeatService : BackgroundService
    {
        private readonly IMqttService _mqttService;

        public HeartbeatService(IMqttService mqttService)
        {
            _mqttService = mqttService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var payload = new
                {
                    deviceId  = SystemManager.DeviceId,
                    localTime = SystemManager.TimeNow()
                };

                _ = _mqttService.PublishAsync(
                    SystemManager.GetMqttTopic(MqttTopics.Heartbeat),
                    payload,
                    retainFlag: false,
                    qos: MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce);

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}

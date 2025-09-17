using System.Text.Json;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols;
using SmartGuardHub.Protocols.MQTT;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class DeviceService : IAsyncInitializer
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly ILogger<DeviceService> _logger;
        private readonly IMqttService _mqttService;

        public DeviceService(IDeviceRepository deviceRepository, ILogger<DeviceService> logger, IMqttService mqttService)
        {
            _deviceRepository = deviceRepository;
            _logger = logger;
            _mqttService = mqttService;
        }

        public async Task InitializeAsync()
        {
            await RefreshDevices();
        }

        public async Task RefreshDevices()
        {
            SystemManager.InstalledSensors = new (await GetAllDevicesAsync());

            _mqttService.PublishAsync(SystemManager.GetMqttTopicPath(MqttTopics.InstalledDevices), SystemManager.InstalledSensors, true);

            _logger.LogInformation("Refreshed devices. Total devices: {Count}", SystemManager.InstalledSensors.Count);
        }

        public async Task<IEnumerable<SensorDTO>> GetAllDevicesAsync()
        {
            var devices = await _deviceRepository.GetAllAsync();
            return devices.Select(MapToDeviceDTO);
        }

        public async Task<SensorDTO?> GetDeviceAsync(string deviceId, Enums.SwitchOutlet switchNo)
        {
            var device = await _deviceRepository.GetByDeviceIdAndSwitchAsync(deviceId, (int)switchNo);
            return device != null ? MapToDeviceDTO(device) : null;
        }

        public async Task<SensorDTO?> GetDeviceAsync(string name)
        {
            var device = await _deviceRepository.GetDeviceByName(name);
            return device != null ? MapToDeviceDTO(device) : null;
        }

        public async Task<Device> CreateDeviceAsync(SensorDTO deviceDTO)
        {
            // Check if device already exists
            var existingDevice = await _deviceRepository.GetByDeviceIdAndSwitchAsync(deviceDTO.UnitId, (int)deviceDTO.SwitchNo);
            if (existingDevice != null)
            {
                throw new InvalidOperationException($"Device with ID {deviceDTO.UnitId} and switch no. {(int)deviceDTO.SwitchNo} already exists");
            }

            var device = new Device
            {
                Name = deviceDTO.Name,
                DeviceId = deviceDTO.UnitId,
                Url = deviceDTO.Url,
                SwitchNo = (int)deviceDTO.SwitchNo,
                Type = (int)deviceDTO.Type,
                Protocol = (int)deviceDTO.Protocol,
                LastSeen = deviceDTO.LastSeen,
                FwVersion = deviceDTO.FwVersion,
                CreatedAt = deviceDTO.CreatedAt,
                RawResponse = deviceDTO.RawResponse,
                IsOnline = deviceDTO.IsOnline,
            };

            var createdDevice = await _deviceRepository.CreateAsync(device);
            _logger.LogInformation("Created new device: {0} ({1}) - SW.{2}", device.Name, device.DeviceId, (int)deviceDTO.SwitchNo);

            return createdDevice;
        }

        public async Task<Device> UpdateDeviceAsync(SensorDTO deviceDTO)
        {
            var device = MapToDevice(deviceDTO);

            var updatedDevice = await _deviceRepository.UpdateAsync(device);
            _logger.LogInformation("Updated device: {0}-SW.{1} [{2}] ", device.DeviceId, (int)deviceDTO.SwitchNo, device.Name);

            return updatedDevice;
        }

        public async Task<bool> DeleteDeviceAsync(SensorDTO deviceDTO)
        {
            var success = await _deviceRepository.DeleteAsync(deviceDTO.Id);
            if (success)
            {
                _logger.LogInformation("Deleted device: {0} ({1}) - SW.{2}", deviceDTO.Name, deviceDTO.UnitId, (int)deviceDTO.SwitchNo);
            }

            return success;
        }

        private static SensorDTO MapToDeviceDTO(Device device)
        {
            return new SensorDTO
            {
                Id = device.Id,
                Name = device.Name,
                UnitId = device.DeviceId,
                Url = device.Url,
                SwitchNo = (SwitchOutlet)device.SwitchNo,
                Type = (UnitType)device.Type,
                Protocol = (UnitProtocolType)device.Protocol,
                IsOnline = device.IsOnline,
                LastSeen = device.LastSeen,
                FwVersion = device.FwVersion,
                CreatedAt = device.CreatedAt,
                RawResponse = device.RawResponse,
                IsInInchingMode = device.IsInInchingMode,
                InchingModeWidthInMs = device.InchingModeWidthInMs
            };
        }
        private static Device MapToDevice(SensorDTO deviceDto)
        {
            return new Device
            {
                Id = deviceDto.Id,
                Name = deviceDto.Name,
                DeviceId = deviceDto.UnitId,
                Url = deviceDto.Url,
                SwitchNo = (int)deviceDto.SwitchNo,
                Type = (int)deviceDto.Type,
                Protocol = (int)deviceDto.Protocol,
                IsOnline = deviceDto.IsOnline,
                LastSeen = deviceDto.LastSeen,
                FwVersion = deviceDto.FwVersion,
                CreatedAt = deviceDto.CreatedAt,
                RawResponse = deviceDto.RawResponse,
                InchingModeWidthInMs = deviceDto.InchingModeWidthInMs,
                IsInInchingMode = deviceDto.IsInInchingMode,
            };
        }
    }
}

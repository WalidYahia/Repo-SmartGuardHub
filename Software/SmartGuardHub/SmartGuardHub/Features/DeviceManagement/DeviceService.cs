using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class DeviceService : IAsyncInitializer
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IEnumerable<IDeviceProtocol> _protocols;
        private readonly ILogger<DeviceService> _logger;

        public DeviceService(IDeviceRepository deviceRepository, IEnumerable<IDeviceProtocol> protocols, ILogger<DeviceService> logger)
        {
            _deviceRepository = deviceRepository;
            _protocols = protocols;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            await RefreshDevices();
        }

        public async Task RefreshDevices()
        {
            SystemManager.Devices = (await GetAllDevicesAsync()).ToList();
        }

        public async Task<IEnumerable<DeviceDTO>> GetAllDevicesAsync()
        {
            var devices = await _deviceRepository.GetAllAsync();
            return devices.Select(MapToDeviceDTO);
        }

        public async Task<DeviceDTO?> GetDeviceAsync(string deviceId, SwitchNo switchNo)
        {
            var device = await _deviceRepository.GetByDeviceIdAndSwitchAsync(deviceId, (int)switchNo);
            return device != null ? MapToDeviceDTO(device) : null;
        }

        public async Task<DeviceDTO?> GetDeviceAsync(string name)
        {
            var device = await _deviceRepository.GetDeviceByName(name);
            return device != null ? MapToDeviceDTO(device) : null;
        }

        public async Task<Device> CreateDeviceAsync(DeviceDTO deviceDTO)
        {
            // Check if device already exists
            var existingDevice = await _deviceRepository.GetByDeviceIdAndSwitchAsync(deviceDTO.DeviceId, (int)deviceDTO.SwitchNo);
            if (existingDevice != null)
            {
                throw new InvalidOperationException($"Device with ID {deviceDTO.DeviceId} and switch no. {(int)deviceDTO.SwitchNo} already exists");
            }

            var device = new Device
            {
                Name = deviceDTO.Name,
                DeviceId = deviceDTO.DeviceId,
                Url = deviceDTO.Url,
                SwitchNo = (int)deviceDTO.SwitchNo,
                Type = (int)deviceDTO.Type,
                Protocol = (int)deviceDTO.Protocol,
                LastSeen = DateTime.UtcNow,
                FwVersion = deviceDTO.FwVersion,
                CreatedAt = deviceDTO.CreatedAt,
                RawResponse = deviceDTO.RawResponse,
                IsOnline = false,
            };

            var createdDevice = await _deviceRepository.CreateAsync(device);
            _logger.LogInformation("Created new device: {0} ({1}) - SW.{2}", device.Name, device.DeviceId, (int)deviceDTO.SwitchNo);

            return createdDevice;
        }
        public async Task<Device> UpdateDeviceAsync(DeviceDTO deviceDTO)
        {
            var device = MapToDevice(deviceDTO);

            var updatedDevice = await _deviceRepository.UpdateAsync(device);
            _logger.LogInformation("Updated device: {0}-SW.{1} [{2}] ", device.DeviceId, (int)deviceDTO.SwitchNo, device.Name);

            return updatedDevice;
        }

        public async Task<bool> DeleteDeviceAsync(DeviceDTO deviceDTO)
        {
            var success = await _deviceRepository.DeleteAsync(deviceDTO.Id);
            if (success)
            {
                _logger.LogInformation("Deleted device: {0} ({1}) - SW.{2}", deviceDTO.Name, deviceDTO.DeviceId, (int)deviceDTO.SwitchNo);
            }

            return success;
        }

        private static DeviceDTO MapToDeviceDTO(Device device)
        {
            return new DeviceDTO
            {
                Id = device.Id,
                Name = device.Name,
                DeviceId = device.DeviceId,
                Url = device.Url,
                SwitchNo = (SwitchNo)device.SwitchNo,
                Type = (DeviceType)device.Type,
                Protocol = (DeviceProtocolType)device.Protocol,
                IsOnline = device.IsOnline,
                LastSeen = device.LastSeen,
                FwVersion = device.FwVersion,
                CreatedAt = device.CreatedAt,
                RawResponse = device.RawResponse,
            };
        }
        private static Device MapToDevice(DeviceDTO deviceDto)
        {
            return new Device
            {
                Id = deviceDto.Id,
                Name = deviceDto.Name,
                DeviceId = deviceDto.DeviceId,
                Url = deviceDto.Url,
                SwitchNo = (int)deviceDto.SwitchNo,
                Type = (int)deviceDto.Type,
                Protocol = (int)deviceDto.Protocol,
                IsOnline = deviceDto.IsOnline,
                LastSeen = deviceDto.LastSeen,
                FwVersion = deviceDto.FwVersion,
                CreatedAt = deviceDto.CreatedAt,
                RawResponse = deviceDto.RawResponse,
            };
        }
    }
}

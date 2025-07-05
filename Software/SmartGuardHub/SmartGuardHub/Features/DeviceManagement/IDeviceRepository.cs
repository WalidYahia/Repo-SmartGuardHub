using SmartGuardHub.Protocols;

namespace SmartGuardHub.Features.DeviceManagement
{
    public interface IDeviceRepository
    {
        Task<IEnumerable<Device>> GetAllAsync();
        Task<Device?> GetByIdAsync(int id);
        Task<Device?> GetDeviceByName(string name);
        Task<Device?> GetByDeviceIdAndSwitchAsync(string deviceId, int switchNo);
        Task<IEnumerable<Device>> GetByDeviceIdAsync(string deviceId);
        Task<Device> CreateAsync(Device device);
        Task<Device> UpdateAsync(Device device);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<Device>> GetByProtocolAsync(DeviceProtocolType protocol);
        Task<IEnumerable<Device>> GetOnlineDevicesAsync();
        Task UpdateLastSeenAsync(int id);
    }
}

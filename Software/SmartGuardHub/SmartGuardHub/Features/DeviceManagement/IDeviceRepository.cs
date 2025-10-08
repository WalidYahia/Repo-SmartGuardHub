using SmartGuardHub.Protocols;

namespace SmartGuardHub.Features.DeviceManagement
{
    public interface IDeviceRepository
    {
        Task<IEnumerable<Sensor>> GetAllAsync();
        Task<Sensor?> GetByIdAsync(string id);
        Task<Sensor?> GetDeviceByName(string name);
        Task<Sensor?> GetByDeviceIdAndSwitchAsync(string deviceId, int switchNo);
        Task<IEnumerable<Sensor>> GetByDeviceIdAsync(string deviceId);
        Task<Sensor> CreateAsync(Sensor device);
        Task<Sensor> UpdateAsync(Sensor device);
        Task<bool> DeleteAsync(string id);
        Task<IEnumerable<Sensor>> GetByProtocolAsync(UnitProtocolType protocol);
        Task<IEnumerable<Sensor>> GetOnlineDevicesAsync();
        Task UpdateLastSeenAsync(string id);
    }
}

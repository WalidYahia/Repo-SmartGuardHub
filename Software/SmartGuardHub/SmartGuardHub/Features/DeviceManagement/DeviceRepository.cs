using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols;

namespace SmartGuardHub.Features.DeviceManagement
{
    public class DeviceRepository : IDeviceRepository
    {
        private readonly SmartGuardDbContext _context;

        public DeviceRepository(SmartGuardDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Device>> GetAllAsync()
        {
            return await _context.Devices
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<Device?> GetByIdAsync(int id)
        {
            return await _context.Devices
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Device> CreateAsync(Device device)
        {
            device.CreatedAt = SystemManager.TimeNow();
            device.LastSeen = SystemManager.TimeNow();

            _context.Devices.Add(device);
            await _context.SaveChangesAsync();
            return device;
        }

        public async Task<Device> UpdateAsync(Device device)
        {
            _context.Devices.Update(device);
            await _context.SaveChangesAsync();
            return device;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var device = await GetByIdAsync(id);
            if (device == null)
                return false;

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Device>> GetByProtocolAsync(DeviceProtocolType protocol)
        {
            return await _context.Devices
                .Where(d => d.Protocol == (int)protocol)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Device>> GetOnlineDevicesAsync()
        {
            return await _context.Devices
                .Where(d => d.IsOnline)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task UpdateLastSeenAsync(int id)
        {
            var device = await GetByIdAsync(id);
            if (device != null)
            {
                device.LastSeen = SystemManager.TimeNow();
                device.IsOnline = true;
                await UpdateAsync(device);
            }
        }

        public async Task<Device?> GetDeviceByName(string name)
        {
            return await _context.Devices
                .FirstOrDefaultAsync(d => d.Name == name);
        }

        public async Task<Device?> GetByDeviceIdAndSwitchAsync(string deviceId, int switchNo)
        {
            return await _context.Devices
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.SwitchNo == switchNo);
        }

        public async Task<IEnumerable<Device>> GetByDeviceIdAsync(string deviceId)
        {
            return await _context.Devices
                .Where(d => d.DeviceId == deviceId)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }
    }
}

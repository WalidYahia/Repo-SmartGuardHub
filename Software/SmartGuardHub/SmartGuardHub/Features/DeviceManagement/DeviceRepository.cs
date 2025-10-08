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

        public async Task<IEnumerable<Sensor>> GetAllAsync()
        {
            return await _context.Sensors
                .OrderByDescending(d => d.Name)
                .ToListAsync();
        }

        public async Task<Sensor?> GetByIdAsync(string id)
        {
            return await _context.Sensors
                .FirstOrDefaultAsync(d => d.SensorId == id);
        }

        public async Task<Sensor> CreateAsync(Sensor device)
        {
            device.CreatedAt = SystemManager.TimeNow();
            device.LastSeen = SystemManager.TimeNow();

            _context.Sensors.Add(device);
            await _context.SaveChangesAsync();
            return device;
        }

        public async Task<Sensor> UpdateAsync(Sensor device)
        {
            _context.Sensors.Update(device);
            await _context.SaveChangesAsync();
            return device;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var device = await GetByIdAsync(id);
            if (device == null)
                return false;

            _context.Sensors.Remove(device);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Sensor>> GetByProtocolAsync(UnitProtocolType protocol)
        {
            return await _context.Sensors
                .Where(d => d.Protocol == (int)protocol)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Sensor>> GetOnlineDevicesAsync()
        {
            return await _context.Sensors
                .Where(d => d.IsOnline)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task UpdateLastSeenAsync(string id)
        {
            var device = await GetByIdAsync(id);
            if (device != null)
            {
                device.LastSeen = SystemManager.TimeNow();
                device.IsOnline = true;
                await UpdateAsync(device);
            }
        }

        public async Task<Sensor?> GetDeviceByName(string name)
        {
            return await _context.Sensors
                .FirstOrDefaultAsync(d => d.Name == name);
        }

        public async Task<Sensor?> GetByDeviceIdAndSwitchAsync(string deviceId, int switchNo)
        {
            return await _context.Sensors
                .FirstOrDefaultAsync(d => d.UnitId == deviceId && d.SwitchNo == switchNo);
        }

        public async Task<IEnumerable<Sensor>> GetByDeviceIdAsync(string deviceId)
        {
            return await _context.Sensors
                .Where(d => d.UnitId == deviceId)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }
    }
}

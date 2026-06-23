using SmartGuardHub.Features.DeviceManagement;

namespace SmartGuardHub.Features.SensorConfiguration
{
    public interface ISensorReadingRepository
    {
        Task SaveAsync(SensorReadingRecord reading);
        Task<SensorReadingRecord?> GetLatestAsync(string sensorId);
        Task<Dictionary<string, SensorReadingRecord>> GetLatestBatchAsync(IEnumerable<string> sensorIds);
        Task<List<SensorReadingRecord>> GetUnsyncedAsync();
        Task UpdatePublishedAtAsync(int id, DateTime publishedAt);
        Task MarkSyncedAsync(int id, DateTime syncedAt);
    }
}

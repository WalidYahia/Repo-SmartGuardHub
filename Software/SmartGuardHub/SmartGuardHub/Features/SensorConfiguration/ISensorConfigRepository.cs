namespace SmartGuardHub.Features.SensorConfiguration
{
    public interface ISensorConfigRepository
    {
        Task<List<SensorConfig>> GetAllAsync();
        Task<bool> SaveAllAsync(List<SensorConfig> configs);
    }
}

using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserScenarios
{
    public interface IUserScenarioRepository
    {
        Task<List<UserScenario>> GetAllAsync();
        Task<List<UserScenario>> GetEnabledAsync();
        Task<UserScenario?> GetByIdAsync(string id);

        Task<bool> SaveAsync(UserScenario scenario, ConfigSource source);
        Task<bool> SaveAllAsync(List<UserScenario> scenarios, ConfigSource source, Guid configVersion = default);
        Task<bool> DeleteAsync(string id, ConfigSource source);
    }
}

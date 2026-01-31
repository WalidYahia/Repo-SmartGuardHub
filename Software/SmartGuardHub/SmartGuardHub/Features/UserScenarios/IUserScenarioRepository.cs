namespace SmartGuardHub.Features.UserScenarios
{
    public interface IUserScenarioRepository
    {
        Task<List<UserScenario>> GetAllAsync();
        Task<List<UserScenario>> GetEnabledAsync();
        Task<UserScenario?> GetByIdAsync(string id);

        Task<bool> SaveAsync(UserScenario scenario);
        Task<bool> DeleteAsync(string id);
    }
}

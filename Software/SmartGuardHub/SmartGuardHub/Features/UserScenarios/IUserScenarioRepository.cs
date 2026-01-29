namespace SmartGuardHub.Features.UserScenarios
{
    public interface IUserScenarioRepository
    {
        Task<List<UserScenario>> GetAllAsync();
        Task<List<UserScenario>> GetEnabledAsync();
        Task<UserScenario?> GetByIdAsync(string id);

        Task SaveAsync(UserScenario scenario);
        Task DeleteAsync(string id);
    }
}

namespace SmartGuardHub.Features.UserScenarios
{
    public class UserScenarioEnvelope
    {
        public Guid ConfigVersion { get; set; }
        public DateTime UpdateTime { get; set; }
        public List<UserScenario> Scenarios { get; set; } = [];
    }
}

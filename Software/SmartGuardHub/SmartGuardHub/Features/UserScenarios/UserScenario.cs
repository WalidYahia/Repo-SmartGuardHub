using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserScenarios
{
    public class UserScenario
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsEnabled { get; set; }

        public string TargetSensorId { get; set; }
        public SwitchOutletStatus Action { get; set; }

        public ScenarioLogic LogicOfConditions { get; set; }
        public List<UserScenarioCondition> Conditions { get; set; }
    }

    public class UserScenarioCondition
    {
        public ScenarioCondition Condition { get; set; }
        public int DurationInSeconds { get; set; }
        public TimeSpan Time { get; set; }
        public List<UserScenarioSensor> SensorsDependency { get; set; }
    }

    public class UserScenarioSensor
    {
        public string SensorId { get; set; }
        public UnitType SensorType { get; set; }
        public string Value { get; set; }
        public ScenarioOperator Operator { get; set; }
    }
}

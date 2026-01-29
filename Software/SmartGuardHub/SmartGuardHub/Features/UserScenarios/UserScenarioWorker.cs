using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.UserCommands;
using SmartGuardHub.Infrastructure;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserScenarios
{
    public class UserScenarioWorker : BackgroundService
    {
        private readonly IUserScenarioRepository _scenarioRepo;
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly Dictionary<string, DateTime> _lastExecution = new();

        public UserScenarioWorker(
            IUserScenarioRepository scenarioRepo,
            IServiceScopeFactory scopeFactory,
            ILogger<UserScenarioWorker> logger)
        {
            _scenarioRepo = scenarioRepo;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var scenarios = await _scenarioRepo.GetEnabledAsync();

                using var scope = _scopeFactory.CreateScope();
                var userCommandHandler = scope.ServiceProvider.GetRequiredService<UserCommandHandler>();
                var loggingService = scope.ServiceProvider.GetRequiredService<LoggingService>();

                foreach (var scenario in scenarios)
                {
                    try
                    {
                        if (!ShouldExecute(scenario))
                            continue;

                        var command = CreateCommand(scenario.TargetSensorId, scenario.Action);
                        await userCommandHandler.HandleApiUserCommand(command);

                        _lastExecution[scenario.Id] = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        await loggingService.LogErrorAsync(
                            LogMessageKey.UserScenario,
                            $"Failed to run scenario {scenario.Name}",
                            ex);
                    }
                }

                await Task.Delay(TimeSpan.FromMilliseconds(2500), stoppingToken);
            }
        }

        private JsonCommand CreateCommand(string sensorId, SwitchOutletStatus scenarioAction)
        {
            var jsonCommand = new JsonCommand();

            var commandPayload = new JsonCommandPayload
            {
                InstalledSensorId = sensorId
            };

            jsonCommand.CommandPayload = commandPayload;
            jsonCommand.JsonCommandType = (scenarioAction == SwitchOutletStatus.On) ?
                                            JsonCommandType.TurnOn : JsonCommandType.TurnOff;

            return jsonCommand;
        }

        private bool ShouldExecute(UserScenario scenario)
        {
            if (scenario.Conditions == null || scenario.Conditions.Count == 0)
                return false;

            var results = scenario.Conditions
                .Select(c => EvaluateCondition(
                    c,
                    scenario.TargetSensorId,
                    (SwitchOutletStatus)scenario.Action))
                .ToList();

            return scenario.LogicOfConditions == ScenarioLogic.And
                ? results.All(r => r)
                : results.Any(r => r);
        }

        private bool EvaluateCondition(UserScenarioCondition condition, string targetSensorId, SwitchOutletStatus action)
        {
            return condition.Condition switch
            {
                ScenarioCondition.OnTime => EvaluateTime(condition, targetSensorId, action),
                ScenarioCondition.Duration => EvaluateDuration(condition, targetSensorId),
                ScenarioCondition.OnOtherSensorValue => EvaluateSensor(condition),
                _ => false
            };
        }

        private bool EvaluateTime(UserScenarioCondition condition, string targetSensorId, SwitchOutletStatus action)
        {
            var state = SystemManager.InstalledSensors.FirstOrDefault(o => o.SensorId == targetSensorId);
            if (state == null) return false;

            var now = DateTime.Now.TimeOfDay;

            return
                Convert.ToInt32(state.LatestValue) != (int)action
                &&
                Math.Abs((now - condition.Time).TotalSeconds) < 5;
        }

        private bool EvaluateDuration(UserScenarioCondition condition, string targetSensorId)
        {
            var state = SystemManager.InstalledSensors.FirstOrDefault(o => o.SensorId == targetSensorId);
            if (state == null) return false;

            return
                Convert.ToInt32(state.LatestValue) == (int)SwitchOutletStatus.On
                &&
                (DateTime.Now - state.LastTimeValueSet).TotalSeconds >= condition.DurationInSeconds;
        }

        private bool EvaluateSensor(UserScenarioCondition condition)
        {
            foreach (var sensorCondition in condition.SensorsDependency)
            {
                var state = SystemManager.InstalledSensors.FirstOrDefault(o => o.SensorId == sensorCondition.SensorId);
                
                if (state == null)
                    return false;

                if (!Compare(
                        state.LatestValue.ToString(),
                        sensorCondition.Value,
                        sensorCondition.Operator,
                        sensorCondition.SensorType))
                    return false;
            }

            return true;
        }

        private bool Compare(
        string current,
        string expected,
        ScenarioOperator op,
        UnitType type)
        {
            return type switch
            {
                UnitType.SonoffMiniR3 =>
                    CompareBool(current, expected, op),

                UnitType.SonoffMiniR4M =>
                    CompareBool(current, expected, op),

                UnitType.Unknown =>
                    CompareDouble(current, expected, op),

                _ => current == expected
            };
        }

        private bool CompareBool(string c, string e, ScenarioOperator op)
        {
            var cv = c == "1" || c.Equals("true", StringComparison.OrdinalIgnoreCase);
            var ev = e == "1" || e.Equals("true", StringComparison.OrdinalIgnoreCase);

            return op == ScenarioOperator.Equals ? cv == ev : cv != ev;
        }

        private bool CompareDouble(string c, string e, ScenarioOperator op)
        {
            var cv = double.Parse(c);
            var ev = double.Parse(e);

            return op switch
            {
                ScenarioOperator.GreaterThan => cv > ev,
                ScenarioOperator.LessThan => cv < ev,
                ScenarioOperator.Equals => cv == ev,
                _ => false
            };
        }
    }
}

using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SensorConfiguration;
using SmartGuardHub.Features.UserCommands;
using SmartGuardHub.Infrastructure;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.UserScenarios
{
    public class UserScenarioWorker : BackgroundService
    {
        private readonly IUserScenarioRepository _scenarioRepo;
        private readonly ISensorReadingRepository _readingRepo;
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly Dictionary<string, DateTime> _lastExecution = new();

        public UserScenarioWorker(
            IUserScenarioRepository scenarioRepo,
            ISensorReadingRepository readingRepo,
            IServiceScopeFactory scopeFactory,
            ILogger<UserScenarioWorker> logger)
        {
            _scenarioRepo = scenarioRepo;
            _readingRepo  = readingRepo;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var scenarios = await _scenarioRepo.GetEnabledAsync();

                // Collect all sensor IDs referenced by enabled scenarios
                var sensorIds = scenarios
                    .SelectMany(s => s.Conditions ?? [])
                    .SelectMany(c => c.SensorsDependency ?? [])
                    .Select(d => d.SensorId)
                    .Concat(scenarios.Select(s => s.TargetSensorId))
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct();

                var latestReadings = await _readingRepo.GetLatestBatchAsync(sensorIds);

                using var scope = _scopeFactory.CreateScope();
                var userCommandHandler = scope.ServiceProvider.GetRequiredService<UserCommandHandler>();
                var loggingService     = scope.ServiceProvider.GetRequiredService<LoggingService>();

                foreach (var scenario in scenarios)
                {
                    try
                    {
                        if (!ShouldExecute(scenario, latestReadings))
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
            return new JsonCommand
            {
                CommandPayload  = new JsonCommandPayload { InstalledSensorId = sensorId },
                JsonCommandType = scenarioAction == SwitchOutletStatus.On
                    ? JsonCommandType.TurnOn
                    : JsonCommandType.TurnOff
            };
        }

        private bool ShouldExecute(
            UserScenario scenario,
            Dictionary<string, Features.DeviceManagement.SensorReadingRecord> readings)
        {
            if (scenario.Conditions == null || scenario.Conditions.Count == 0)
                return false;

            var results = scenario.Conditions
                .Select(c => EvaluateCondition(c, scenario.TargetSensorId, (SwitchOutletStatus)scenario.Action, readings))
                .ToList();

            return scenario.LogicOfConditions == ScenarioLogic.And
                ? results.All(r => r)
                : results.Any(r => r);
        }

        private bool EvaluateCondition(
            UserScenarioCondition condition,
            string targetSensorId,
            SwitchOutletStatus action,
            Dictionary<string, Features.DeviceManagement.SensorReadingRecord> readings)
        {
            return condition.Condition switch
            {
                ScenarioCondition.OnTime             => EvaluateTime(condition, targetSensorId, action, readings),
                ScenarioCondition.Duration           => EvaluateDuration(condition, targetSensorId, readings),
                ScenarioCondition.OnOtherSensorValue => EvaluateSensor(condition, readings),
                _                                    => false
            };
        }

        private bool EvaluateTime(
            UserScenarioCondition condition,
            string targetSensorId,
            SwitchOutletStatus action,
            Dictionary<string, Features.DeviceManagement.SensorReadingRecord> readings)
        {
            readings.TryGetValue(targetSensorId, out var state);
            var currentValue = Convert.ToInt32(SensorReadingJson.Deserialize(state?.Reading)?.Value ?? "0");
            var now = DateTime.Now.TimeOfDay;
            return currentValue != (int)action
                && Math.Abs((now - condition.Time).TotalSeconds) < 5;
        }

        private bool EvaluateDuration(
            UserScenarioCondition condition,
            string targetSensorId,
            Dictionary<string, Features.DeviceManagement.SensorReadingRecord> readings)
        {
            readings.TryGetValue(targetSensorId, out var state);
            if (state == null) return false;

            var value = SensorReadingJson.Deserialize(state.Reading)?.Value ?? "0";
            return Convert.ToInt32(value) == (int)SwitchOutletStatus.On
                && (DateTime.UtcNow - state.LogTime).TotalSeconds >= condition.DurationInSeconds;
        }

        private bool EvaluateSensor(
            UserScenarioCondition condition,
            Dictionary<string, Features.DeviceManagement.SensorReadingRecord> readings)
        {
            if (condition.SensorsDependency == null || condition.SensorsDependency.Count == 0)
                return false;

            foreach (var dep in condition.SensorsDependency)
            {
                readings.TryGetValue(dep.SensorId, out var state);
                if (state == null) return false;

                if (!Compare(SensorReadingJson.Deserialize(state.Reading)?.Value ?? "0", dep.Value, dep.Operator, dep.SensorType))
                    return false;
            }
            return true;
        }

        private bool Compare(string current, string expected, ScenarioOperator op, int sensorType)
        {
            return sensorType == (int)Enums.SensorType.SonOffMiniR3Swich
                ? CompareBool(current, expected, op)
                : CompareDouble(current, expected, op);
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
                ScenarioOperator.LessThan    => cv < ev,
                ScenarioOperator.Equals      => cv == ev,
                _                            => false
            };
        }
    }
}

using System.Text.Json;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.SensorConfiguration
{
    public class JsonSensorUnitDefinitionRepository : ISensorUnitDefinitionRepository
    {
        private readonly List<SensorUnitDefinition> _definitions;

        public JsonSensorUnitDefinitionRepository(IWebHostEnvironment env)
        {
            var path = Path.Combine(env.ContentRootPath, "sensorTypes.json");
            var json = File.ReadAllText(path);
            _definitions = JsonSerializer.Deserialize<List<SensorUnitDefinition>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }

        public List<SensorUnitDefinition> GetAll() => _definitions;

        public SensorUnitDefinition? GetByName(string name) =>
            _definitions.FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public SensorUnitDefinition? GetBySensorType(SensorType sensorType) =>
            _definitions.FirstOrDefault(d => d.SensorType == sensorType);
    }
}

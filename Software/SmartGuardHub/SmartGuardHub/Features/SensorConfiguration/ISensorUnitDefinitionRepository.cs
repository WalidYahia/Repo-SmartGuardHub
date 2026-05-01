using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Features.SensorConfiguration
{
    public interface ISensorUnitDefinitionRepository
    {
        List<SensorUnitDefinition> GetAll();
        SensorUnitDefinition? GetByName(string name);
        SensorUnitDefinition? GetBySensorType(SensorType sensorType);
    }
}

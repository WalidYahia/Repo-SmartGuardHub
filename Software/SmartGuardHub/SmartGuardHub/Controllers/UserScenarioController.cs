using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartGuardHub.Features.UserCommands;
using SmartGuardHub.Features.UserScenarios;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Network;
using SmartGuardHub.Protocols.MQTT;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserScenarioController : ControllerBase
    {
        private readonly IUserScenarioRepository _scenarioRepo;
        private readonly IMqttService _mqttService;

        public UserScenarioController(IUserScenarioRepository scenarioRepo, IMqttService mqttService)
        {
            _scenarioRepo = scenarioRepo;
            _mqttService = mqttService;
        }

        [HttpPost("saveUserScenario")]
        public async Task<IActionResult> SaveUserScenario([FromBody] UserScenario request)
        {
            if (request == null)
                return BadRequest("Scenario payload is required.");

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Scenario name is required.");

            if (string.IsNullOrWhiteSpace(request.TargetSensorId))
                return BadRequest("TargetSensorId is required.");

            // Ensure ID (important for updates & worker tracking)
            if (string.IsNullOrWhiteSpace(request.Id))
                request.Id = Guid.NewGuid().ToString("N");

            var result = await _scenarioRepo.SaveAsync(request);

            if (result)
            {
                var scenarios = await _scenarioRepo.GetAllAsync();

                _mqttService.PublishAsync(SystemManager.GetMqttTopicPath(MqttTopics.UserScenarios), scenarios, retainFlag: true);
            }

            return Ok(new
            {
                request.Id,
                Message = "User scenario saved successfully"
            });
        }

        [HttpGet("loadUserScenarios")]
        public async Task<IActionResult> LoadUserScenarios()
        {
            var scenarios = await _scenarioRepo.GetAllAsync();

            return Ok(scenarios);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserScenario(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Scenario Id is required.");

            var deleted = await _scenarioRepo.DeleteAsync(id);

            if (!deleted)
                return NotFound($"No scenario found with Id = {id}");

            return Ok(new { Message = $"User scenario {id} deleted successfully" });
        }
    }
}

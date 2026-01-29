using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartGuardHub.Features.UserScenarios;
using SmartGuardHub.Network;

namespace SmartGuardHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserScenarioController : ControllerBase
    {
        private readonly IUserScenarioRepository _scenarioRepo;

        public UserScenarioController(IUserScenarioRepository scenarioRepo)
        {
            _scenarioRepo = scenarioRepo;
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

            await _scenarioRepo.SaveAsync(request);

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
    }
}

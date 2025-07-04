using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SmartGuardHub.Protocols;

namespace SmartGuardHub.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly RestProtocol _restProto;
        public WeatherForecastController(ILogger<WeatherForecastController> logger, RestProtocol restProto)
        {
            _logger = logger;
            _restProto = restProto;
        }

        [HttpGet("on")]
        public object On()
        {
            var payload = new
            {
                deviceid = "10016ca843",
                data = new
                {
                    switches = new[]
                    {
                        new { @switch = "on", outlet = 0 },
                        new { @switch = "off", outlet = 1 },
                        new { @switch = "off", outlet = 2 },
                        new { @switch = "off", outlet = 3 }
                     }
                }
            };

            string jsonString = JsonConvert.SerializeObject(payload);


            return _restProto.SendCommandAsync("http://eWeLink_10016ca843:8081/zeroconf/switches", jsonString).Result.Content.ReadAsStringAsync().Result;


            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("off")]
        public object Off()
        {
            var payload = new
            {
                deviceid = "10016ca843",
                data = new
                {
                    switches = new[]
                    {
                        new { @switch = "off", outlet = 0 },
                        new { @switch = "off", outlet = 1 },
                        new { @switch = "off", outlet = 2 },
                        new { @switch = "off", outlet = 3 }
                     }
                }
            };

            string jsonString = JsonConvert.SerializeObject(payload);


            return _restProto.SendCommandAsync("http://eWeLink_10016ca843:8081/zeroconf/switches", jsonString).Result.Content.ReadAsStringAsync().Result;


            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("GetInfo")]
        public object GetInfo()
        {
            var payload = new
            {
                deviceid = "10016ca843",
                data = new
                {
                }
            };

            string jsonString = JsonConvert.SerializeObject(payload);


            return _restProto.SendCommandAsync("http://eWeLink_10016ca843:8081/zeroconf/info", jsonString).Result.Content.ReadAsStringAsync().Result;


            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

    }
}

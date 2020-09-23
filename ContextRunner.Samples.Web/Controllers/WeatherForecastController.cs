using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ContextRunner.Samples.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly IContextRunner _runner;

        public WeatherForecastController(IContextRunner runner = null)
        {
            _runner = runner ?? ActionContextRunner.Runner;
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            return await _runner.RunAction<IEnumerable<WeatherForecast>>(async context =>
            {
                context.Logger.Debug("Simulating fetching weather information...");
                context.State.SetParam("WeatherInfo", new
                {
                    Source = "National Weather Service",
                    Data = "{ \"thing\": \"value of thing\" }"
                });

                var rng = new Random();
                return Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = Summaries[rng.Next(Summaries.Length)]
                })
                .ToArray();
            }, "WeatherService");
        }

        [HttpGet]
        [Route("error")]
        public async Task<IEnumerable<WeatherForecast>> GetError()
        {
            return await _runner.RunAction<IEnumerable<WeatherForecast>>(async context =>
            {
                throw new Exception("Sample exception!");
            }, "WeatherService");
        }

        [HttpGet]
        [Route("error_caught")]
        public async Task<IEnumerable<WeatherForecast>> GetCaughtError()
        {
            try
            {
                return await _runner.RunAction<IEnumerable<WeatherForecast>>(async context =>
                {
                    throw new Exception("Sample exception!");
                }, "WeatherService");
            }
            catch (Exception e)
            {
                return new List<WeatherForecast>();
            }
        }

        [HttpGet]
        [Route("error_override")]
        public async Task<IEnumerable<WeatherForecast>> GetOverridenError()
        {
            try
            {
                return await _runner.CreateAndAppendToActionExceptions<IEnumerable<WeatherForecast>>(async context =>
                    {
                        throw new Exception("Sample exception!");
                    },
                    (ex, context) =>
                    {
                        ex.Data.Add("ContextExceptionHandled", true);
                
                        context.Logger.Information($"An error was thrown but it's expected... don't blow up!");
                        context.State.SetParam("IgnoredException", ex);

                        return ex;
                    },
                    "WeatherService");
            }
            catch (Exception e)
            {
                return new List<WeatherForecast>();
            }
        }

        [HttpGet]
        [Route("error_wo_context")]
        public async Task<IEnumerable<WeatherForecast>> GetErrorWithoutContext()
        {
            using var context = _runner.Create("WeatherService");
            throw new Exception("Sample exception!");
        }

        [HttpPost]
        public async Task<WeatherForecast> Post(WeatherForecast forcast)
        {
            return await _runner.RunAction(async context =>
            {
                context.Logger.Debug("Simulating saving weather information...");
                context.State.SetParam("WeatherInfo", new
                {
                    Source = "National Weather Service",
                    Data = "{ \"thing\": \"value of thing\" }"
                });
                
                context.State.AppendParam("5DayForcast", new
                {
                    Source = "National Weather Service",
                    Data = "{ \"day 1\": \"value of day 1\" }"
                });
                context.State.AppendParam("5DayForcast", new
                {
                    Source = "National Weather Service",
                    Data = "{ \"day 2\": \"value of day 2\" }"
                });
                context.State.AppendParam("5DayForcast", new
                {
                    Source = "National Weather Service",
                    Data = "{ \"day 3\": \"value of day 3\" }"
                });
                context.State.AppendParam("5DayForcast", new
                {
                    Source = "National Weather Service",
                    Data = "{ \"day 4\": \"value of day 4\" }"
                });
                context.State.AppendParam("5DayForcast", new
                {
                    Source = "National Weather Service",
                    Data = "{ \"day 5\": \"value of day 5\" }"
                });

                return forcast;
            }, "WeatherService");
        }
    }
}

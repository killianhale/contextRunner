using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace ContextRunner.Samples.Web.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        private readonly IContextRunner _runner;

        /// <summary>
        /// Constructor for DI
        /// </summary>
        /// <param name="runner"></param>
        public WeatherForecastController(IContextRunner? runner = null)
        {
            _runner = runner ?? ActionContextRunner.Runner;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            return _runner.CreateAndAppendToActionExceptions<IEnumerable<WeatherForecast>>(context =>
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
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("checkpoints")]
        public IEnumerable<WeatherForecast> GetWithCheckpoints()
        {
            return _runner.CreateAndAppendToActionExceptions<IEnumerable<WeatherForecast>>(context =>
            {
                context.Logger.Debug("Simulating fetching weather information...");
                context.State.SetParam("WeatherInfo", new
                {
                    Source = "National Weather Service",
                    Data = "{ \"thing\": \"value of thing\" }"
                });
                
                context.CreateCheckpoint("Checkpoint1");
                
                context.Logger.Debug("Now that we've reached the checkpoint, let's do another thing...");
                context.State.SetParam("ResultOfAnotherThing", new
                {
                    Source = "National Weather Service 2",
                    Data = "{ \"thing 2\": \"value of another thing\" }"
                });

                var rng = new Random();
                return Enumerable.Range(1, 5).Select(index => new WeatherForecast
                    {
                        Date = DateTime.Now.AddDays(index),
                        TemperatureC = rng.Next(-20, 55),
                        Summary = Summaries[rng.Next(Summaries.Length)]
                    })
                    .ToArray();
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpGet]
        [Route("error")]
        public IEnumerable<WeatherForecast> GetError()
        {
            return _runner.CreateAndAppendToActionExceptions<IEnumerable<WeatherForecast>>(_ =>
                throw new Exception("Sample exception!"));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpGet]
        [Route("error_caught")]
        public IEnumerable<WeatherForecast> GetCaughtError()
        {
            try
            {
                return _runner.CreateAndAppendToActionExceptions<IEnumerable<WeatherForecast>>(_ =>
                    throw new Exception("Sample exception!"));
            }
            catch (Exception)
            {
                return new List<WeatherForecast>();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpGet]
        [Route("error_override")]
        public IEnumerable<WeatherForecast> GetOverridenError()
        {
            try
            {
                return _runner.CreateAndAppendToActionExceptions<IEnumerable<WeatherForecast>>(
                    _ => throw new Exception("Sample exception!"),
                    (ex, context) =>
                    {
                        ex.Data.Add("ContextExceptionHandled", true);
                
                        context.Logger.Information($"An error was thrown but it's expected... don't blow up!");
                        context.State.SetParam("IgnoredException", ex);

                        return ex;
                    });
            }
            catch (Exception)
            {
                return new List<WeatherForecast>();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpGet]
        [Route("error_wo_context")]
        public IEnumerable<WeatherForecast> GetErrorWithoutContext()
        {
            using var context = _runner.Create();
            throw new Exception("Sample exception!");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="forcast"></param>
        /// <returns></returns>
        [HttpPost]
        public WeatherForecast Post(WeatherForecast forcast)
        {
            return _runner.CreateAndAppendToActionExceptions(context =>
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
            });
        }
    }
}

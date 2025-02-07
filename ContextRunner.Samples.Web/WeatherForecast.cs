using System;

namespace ContextRunner.Samples.Web
{
    /// <summary>
    /// Details of a weather forecast
    /// </summary>
    public class WeatherForecast
    {
        /// <summary>
        /// The date of the weather forecast
        /// </summary>
        public DateTime Date { get; init; }

        /// <summary>
        /// The temperature in Celsius
        /// </summary>
        public int TemperatureC { get; init; }

        /// <summary>
        /// The temperature in Fahrenheit
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        /// <summary>
        /// The forecast summary
        /// </summary>
        public required string Summary { get; init; }

        /// <summary>
        /// Get the forecast as a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Date} - {TemperatureF}\u00b0F/{TemperatureC}\u00b0C - {Summary}";
        }
    }
}

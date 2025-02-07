using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ContextRunner.Samples.Web.Models
{
    /// <summary>
    /// A request for a survey
    /// </summary>
    public class SurveyRequest
    {
        /// <summary>
        /// The survey ID
        /// </summary>
        public int SurveyId { get; set; }
        /// <summary>
        /// The survey response
        /// </summary>
        public required JObject ResponseBody { get; set; }
        /// <summary>
        /// The version of the survey
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// Any metadata on the survey
        /// </summary>
        public Dictionary<string, string>? Metadata { get; set; }
    }
}

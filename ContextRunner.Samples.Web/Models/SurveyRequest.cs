using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ContextRunner.Samples.Web.Models
{
    public class SurveyRequest
    {
        public JObject ResponseBody { get; set; }
        public int Version { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }
}

using System;
namespace ContextRunner.NLog
{
    public class NlogContextRunnerConfig
    {
        public bool AddSpacingToEntries { get; set; }
        public string[] SanitizedProperties { get; set; }
    }
}

using System;
namespace ContextRunner.Http
{
    public class ActionContextMiddlewareConfig
    {
        public ActionContextMiddlewareConfig()
        {
            PathPrefixWhitelist = "/api/";
        }

        public string PathPrefixWhitelist { get; set; }
    }
}

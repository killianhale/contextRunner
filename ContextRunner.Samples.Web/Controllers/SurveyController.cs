using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContextRunner.Samples.Web.Models;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ContextRunner.Samples.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SurveyController : ControllerBase
    {
        private readonly IContextRunner _runner;

        public SurveyController(IContextRunner runner = null)
        {
            _runner = runner; //?? ActionContextRunner.Runner;
        }

        [HttpPost]
        public async Task<SurveyRequest> Post(SurveyRequest survey)
        {
            return await _runner.RunAction(async context =>
            {
                context.Logger.Debug("Simulating saving survey information...");
                context.State.SetParam("Survey", survey);

                return survey;
            }, "SurveyService");
        }
    }
}

using System;
using ContextRunner.Samples.Web.Models;
using Microsoft.AspNetCore.Mvc;
using NLog;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ContextRunner.Samples.Web.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class SurveyController : ControllerBase
    {
        private readonly IContextRunner _runner;

        /// <summary>
        /// Constructor for DI
        /// </summary>
        /// <param name="runner"></param>
        public SurveyController(IContextRunner? runner = null)
        {
            _runner = runner ?? ActionContextRunner.Runner;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="survey"></param>
        /// <returns></returns>
        [HttpPost]
        public SurveyRequest Post(SurveyRequest survey)
        {
            return _runner.CreateAndAppendToActionExceptions(context =>
            {
                context.Logger.Debug("Simulating saving survey information...");
                context.State.SetParam("Survey", survey);

                var logger = LogManager.LogFactory.GetCurrentClassLogger();
                logger.Debug("This is a test");

                return survey;
            });
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="survey"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("deprecated")]
        [Obsolete("Obsolete")]
        public SurveyRequest Post2(SurveyRequest survey)
        {
            return _runner.RunAction(context =>
            {
                context.Logger.Debug("Simulating saving survey information...");
                context.State.SetParam("Survey", survey);

                var logger = LogManager.LogFactory.GetCurrentClassLogger();
                logger.Debug("This is a test w/ deprecated method");

                return survey;
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="survey"></param>
        /// <returns></returns>
        [HttpPut]
        public SurveyRequest Put(SurveyRequest survey)
        {
            using var context = _runner.Create();

            context.Logger.Debug("Pretending we're updating the survey information...");
            context.State.SetParam("Survey", survey);

            var logger = LogManager.LogFactory.GetCurrentClassLogger();
            logger.Debug("This is yet another test");

            return survey;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="survey"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("nested")]
        public SurveyRequest PostNested(SurveyRequest survey)
        {
            return _runner.CreateAndAppendToActionExceptions(context =>
            {
                context.Logger.Debug("Simulating saving survey information...");
                context.State.SetParam("Survey", survey);

                _runner.CreateAndAppendToActionExceptions(context2 =>
                {
                    context2.Logger.Debug("This is a nested wrapper...");
                    for (var x = 0; x < 5; x++)
                    {
                        var x1 = x;
                        
                        _runner.CreateAndAppendToActionExceptions(context3 =>
                        {
                            context3.Logger.Debug($"this is service {x1} inside of the nested wrapper...");
                            context3.State.SetParam($"Survey{x1}", survey);

                            return survey;
                        }, $"NestedService{x}", "nested");
                    }
                }, contextGroupName: "nested");

                return survey;
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="survey"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("inverted")]
        public SurveyRequest PostNestedInverted(SurveyRequest survey)
        {
            return _runner.CreateAndAppendToActionExceptions(context =>
            {
                context.Logger.Debug("Simulating saving survey information...");
                context.State.SetParam("Survey", survey);

                _runner.CreateAndAppendToActionExceptions(context2 =>
                {
                    context2.Logger.Debug("This is a nested wrapper...");
                    for (var x = 0; x < 5; x++)
                    {
                        var x1 = x;
                        
                        _runner.CreateAndAppendToActionExceptions(context3 =>
                        {
                            context3.Logger.Debug($"this is service {x1} inside of the nested wrapper...");
                            context3.State.SetParam($"Survey{x1}", survey);

                            return survey;
                        }, $"NestedService{x}");
                    }
                });

                return survey;
            }, contextGroupName: "nested");
        }
    }
}

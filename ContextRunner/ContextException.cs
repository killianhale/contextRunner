using System;
using ContextRunner.Base;

namespace ContextRunner
{
    public class ContextException : Exception
    {
        public ActionContext Context { get; private set; }

        public ContextException(ActionContext context, string message) : base(message)
        {
            Context = context;
        }

        public ContextException(ActionContext context, string message, Exception innerException) : base(message, innerException)
        {
            Context = context;
        }
    }
}

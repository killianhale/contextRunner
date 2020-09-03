using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ContextRunner.Base;

namespace ContextRunner
{
    public interface IContextRunner
    {
        void RunAction(Action<ActionContext> action, [CallerMemberName] string name = null, string contextGroupName = "default");
        Task RunAction(Func<ActionContext, Task> action, [CallerMemberName] string name = null, string contextGroupName = "default");
        Task<T> RunAction<T>(Func<ActionContext, Task<T>> action, [CallerMemberName] string name = null, string contextGroupName = "default");
    }
}
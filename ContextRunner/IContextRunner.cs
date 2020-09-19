using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ContextRunner.Base;

namespace ContextRunner
{
    public interface IContextRunner
    {
        void RunAction(Action<IActionContext> action, [CallerMemberName] string name = null, string contextGroupName = "default");
        Task RunAction(Func<IActionContext, Task> action, [CallerMemberName] string name = null, string contextGroupName = "default");
        Task<T> RunAction<T>(Func<IActionContext, Task<T>> action, [CallerMemberName] string name = null, string contextGroupName = "default");
    }
}
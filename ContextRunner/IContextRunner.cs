using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ContextRunner.Base;

namespace ContextRunner
{
    public interface IContextRunner : IDisposable
    {
        IActionContext Create([CallerMemberName] string name = null, string contextGroupName = "default");

        [Obsolete("Please use CreateAndWrapActionExceptions as its use is clearer.", false)]
        void RunAction(Action<IActionContext> action, [CallerMemberName] string name = null,
            string contextGroupName = "default");

        [Obsolete("Please use CreateAndWrapActionExceptions as its use is clearer.", false)]
        public T RunAction<T>(Func<IActionContext, T> action, [CallerMemberName] string name = null,
            string contextGroupName = "default");

        [Obsolete("Please use CreateAndAppendToActionExceptions as its use is clearer.", false)]
        Task RunAction(Func<IActionContext, Task> action, [CallerMemberName] string name = null,
            string contextGroupName = "default");

        [Obsolete("Please use CreateAndAppendToActionExceptions as its use is clearer.", false)]
        Task<T> RunAction<T>(Func<IActionContext, Task<T>> action, [CallerMemberName] string name = null,
            string contextGroupName = "default");

        void CreateAndAppendToActionExceptions(Action<IActionContext> action, [CallerMemberName] string name = null,
            string contextGroupName = "default");

        T CreateAndAppendToActionExceptions<T>(Func<IActionContext, T> action,
            [CallerMemberName] string name = null, string contextGroupName = "default");

        Task CreateAndAppendToActionExceptions(Func<IActionContext, Task> action, [CallerMemberName] string name = null,
            string contextGroupName = "default");

        Task<T> CreateAndAppendToActionExceptions<T>(Func<IActionContext, Task<T>> action,
            [CallerMemberName] string name = null, string contextGroupName = "default");
    }
}
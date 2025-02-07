using System.Runtime.CompilerServices;
using ContextRunner.Base;

namespace ContextRunner
{
    public interface IContextRunner : IDisposable
    {
        public IActionContext Create([CallerMemberName] string? name = null, string contextGroupName = "default");

        void CreateAndAppendToActionExceptions(Action<IActionContext> action,
            [CallerMemberName] string? name = null, string contextGroupName = "default");
        T CreateAndAppendToActionExceptions<T>(Func<IActionContext, T> action, [CallerMemberName] string? name = null,
            string contextGroupName = "default");
        Task CreateAndAppendToActionExceptionsAsync(Func<IActionContext, Task> action, [CallerMemberName] string? name = null,
            string contextGroupName = "default");
        Task<T> CreateAndAppendToActionExceptionsAsync<T>(Func<IActionContext, Task<T>> action, [CallerMemberName] string? name = null,
            string contextGroupName = "default");

        void CreateAndAppendToActionExceptions(Action<IActionContext> action, Func<Exception, IActionContext, Exception>? errorHandlingOverride, [CallerMemberName]string? name = null, string contextGroupName = "default");
        T CreateAndAppendToActionExceptions<T>(Func<IActionContext, T> action, Func<Exception, IActionContext, Exception>? errorHandlingOverride, [CallerMemberName]string? name = null, string contextGroupName = "default");
        Task CreateAndAppendToActionExceptionsAsync(Func<IActionContext, Task> action, Func<Exception, IActionContext, Exception>? errorHandlingOverride, [CallerMemberName]string? name = null, string contextGroupName = "default");
        Task<T> CreateAndAppendToActionExceptionsAsync<T>(Func<IActionContext, Task<T>> action, Func<Exception, IActionContext, Exception>? errorHandlingOverride, [CallerMemberName]string? name = null, string contextGroupName = "default");
    }
}
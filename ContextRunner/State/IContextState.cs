using System.Collections.Generic;

namespace ContextRunner.State
{
    public interface IContextState
    {
        IReadOnlyDictionary<string, object> Params { get; }
        T GetParam<T>(string name) where T : class;
        void SetParam<T>(string name, T value) where T : class;
        void AppendParam<T>(string name, T value) where T : class;
        T RemoveParam<T>(string name) where T : class;
        bool RemoveParam(string name);
        void Clear();
    }
}
namespace ContextRunner.State
{
    public interface IContextState
    {
        IReadOnlyDictionary<string, object?> Params { get; }
        IEnumerable<ISanitizer> Sanitizers { get; }
        bool ContainsKey(string name);
        T? GetParam<T>(string name) where T : class;
        object? GetParam(string name);
        void SetParam<T>(string name, T? value) where T : class;
        void AppendParam<T>(string name, T? value) where T : class;
        T? RemoveParam<T>(string name) where T : class;
        bool RemoveParam(string name);
        void Clear();
    }
}
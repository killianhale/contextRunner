using System.Collections.ObjectModel;

namespace ContextRunner.State
{
    public class ContextState : IContextState
    {
        private readonly ConcurrentDictionary<string, object?> _params;

        public ContextState(IEnumerable<ISanitizer>? sanitizers = null)
        {
            _params = new ConcurrentDictionary<string, object?>();

            Sanitizers = sanitizers ?? [];
        }

        public IReadOnlyDictionary<string, object?> Params => new ReadOnlyDictionary<string, object?>(_params);

        public IEnumerable<ISanitizer> Sanitizers { get; }

        public bool ContainsKey(string name)
        {
            return _params.ContainsKey(name);
        }

        public T? GetParam<T>(string name) where T : class
        {
            return _params.GetOrAdd(name, (T?)null) as T;
        }

        public object? GetParam(string name)
        {
            return _params.GetOrAdd(name, "");
        }

        public void SetParam<T>(string name, T? value) where T : class
        {
            object? sanitized = value;

            foreach (var sanitizer in Sanitizers)
            {
                sanitized = sanitizer.Sanitize(new KeyValuePair<string, object?>(name, value));
            }

            _params[name] = sanitized;
        }

        public void AppendParam<T>(string name, T? value) where T : class
        {
            object? sanitized = value;

            foreach (var sanitizer in Sanitizers)
            {
                sanitized = sanitizer.Sanitize(new KeyValuePair<string, object?>(name, value));
            }

            var list = _params.GetOrAdd(name, new ConcurrentBag<object?>()) as ConcurrentBag<object?>;
            list?.Add(sanitized);
        }

        public T? RemoveParam<T>(string name) where T : class
        {
            var success = _params.TryRemove(name, out object? val);

            return success ? val as T : null;
        }

        public bool RemoveParam(string name)
        {
            var success = _params.TryRemove(name, out _);

            return success;
        }

        public void Clear()
        {
            _params.Clear();
        }
    }
}

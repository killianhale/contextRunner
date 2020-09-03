using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ContextRunner.State
{
    public class ContextState
    {
        private readonly ConcurrentDictionary<string, object> _params;

        protected IEnumerable<ISanitizer> Sanitizers { get; set; }

        public ContextState(IEnumerable<ISanitizer> sanitizers = null)
        {
            _params = new ConcurrentDictionary<string, object>();

            Sanitizers = sanitizers ?? new List<ISanitizer>();
        }

        public IReadOnlyDictionary<string, object> Params
        {
            get
            {
                return new ReadOnlyDictionary<string, object>(_params);
            }
        }

        public T GetParam<T>(string name) where T : class
        {
            return _params.GetOrAdd(name, null) as T;
        }

        public void SetParam<T>(string name, T value) where T : class
        {
            object sanitized = value;

            foreach (var sanitizer in Sanitizers)
            {
                sanitized = sanitizer.Sanitize(new KeyValuePair<string, object>(name, value));
            }

            _params[name] = sanitized;
        }

        public void AppendParam<T>(string name, T value) where T : class
        {
            object sanitized = value;

            foreach (var sanitizer in Sanitizers)
            {
                sanitized = sanitizer.Sanitize(new KeyValuePair<string, object>(name, value));
            }

            var list = _params.GetOrAdd(name, new ConcurrentBag<object>()) as ConcurrentBag<object>;
            list?.Add(sanitized);
        }

        public T RemoveParam<T>(string name) where T : class
        {
            var success = _params.TryRemove(name, out object val);

            return success ? val as T : null;
        }

        public bool RemoveParam(string name)
        {
            var success = _params.TryRemove(name, out _);

            return success;
        }
    }
}

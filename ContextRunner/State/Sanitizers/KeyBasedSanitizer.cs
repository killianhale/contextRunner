using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace ContextRunner.State.Sanitizers
{
    public class KeyBasedSanitizer : ISanitizer
    {
        private readonly IEnumerable<string> _sanitizedKeys;

        public KeyBasedSanitizer(IEnumerable<string> sanitizedKeys)
        {
            _sanitizedKeys = sanitizedKeys;
        }

        public object Sanitize(KeyValuePair<string, object> contextParam)
        {
            if (contextParam.Key.ToLower() == "request")
            {
                return contextParam.Value;
            }

            return SanitizeParam(contextParam.Key, contextParam.Value);
        }

        private bool ShouldBeSanitized(string propName)
        {
            var key = propName
                   .Replace("-", string.Empty)
                   .Replace("_", string.Empty)
                   .ToLower();

            return _sanitizedKeys.Contains(key);
        }

        private object SanitizeParam(string propName, object obj)
        {
            if (propName != null && ShouldBeSanitized(propName))
            {
                return null;
            }

            var type = obj?.GetType();

            if (type == null || type.IsPrimitive || type.IsAssignableFrom(typeof(string)))
            {
                return obj;
            }
            else if (obj is ExpandoObject)
            {
                return GetDictionary((IDictionary<string, object>)obj);
            }
            else if(obj is IDictionary<string, object>)
            {
                return GetDictionary(obj as IDictionary<string, object>);
            }
            else if(obj is System.Collections.IEnumerable)
            {
                var enumerable = obj as System.Collections.IEnumerable;

                var result = new List<object>(enumerable.Cast<object>())
                    .Select(item => SanitizeParam(null, item))
                    .ToList();

                return result;
            }
            else if(obj is Exception)
            {
                return GetException(type, obj as Exception);
            }
            else if (type.IsClass)
            {
                return GetObject(type, obj);
            }
            else
            {
                return obj;
            }
        }

        private object GetException(Type type, Exception ex)
        {
            var expando = new ExpandoObject();
            var dictionary = (IDictionary<string, object>)expando;

            var filter = new[] { "Data", "TargetSite" };

            type.GetProperties()
                .Where(prop => !filter.Contains(prop.Name))
                .Where(prop => !ShouldBeSanitized(prop.Name))
                .Where(p => p.GetIndexParameters().Length == 0)
                .ToList()
                .ForEach(prop =>
                {
                    var val = prop.GetValue(ex);

                    dictionary.Add(prop.Name, val);
                });

            var data = new Dictionary<string, object>();

            foreach (var key in ex.Data.Keys)
            {
                var val = ex.Data[key];

                if(key is string)
                {
                    val = SanitizeParam((string)key, ex.Data[key]);

                    data.Add((string)key, val);
                }
            }

            if(data.Any())
            {
                dictionary.Add("Data", data);
            }

            return expando;
        }

        private object GetObject(Type type, object obj)
        {
            var expando = new ExpandoObject();
            var dictionary = (IDictionary<string, object>)expando;

            var props = type.GetProperties()
                    .Where(p => p.GetIndexParameters().Length == 0)
                    .ToList();

            foreach (var property in props)
            {
                var val = SanitizeParam(property.Name, property.GetValue(obj));

                dictionary[property.Name] = val;
            }

            return expando;
        }

        private object GetDictionary(IDictionary<string, object> dictionary)
        {
            return dictionary.Keys
                .Where(key => !_sanitizedKeys.Contains(key))
                .ToDictionary(
                    key => key,
                    key =>
                    {

                        var type = dictionary[key]?.GetType();

                        var result = type == null || type.IsPrimitive || type.IsAssignableFrom(typeof(string))
                            ? dictionary[key]
                            : SanitizeParam(key, dictionary[key]);

                        return result;
                    }
                );
        }
    }
}

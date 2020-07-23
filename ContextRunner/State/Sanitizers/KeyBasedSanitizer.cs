using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Newtonsoft.Json.Linq;

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

            object lookupObj = obj;

            if (obj is JArray)
            {
                var token = obj as JToken;

                var bodyArray = token.ToArray().Select(item =>
                {
                    return item.ToObject<Dictionary<string, object>>();
                }).ToArray();

                lookupObj = bodyArray;
            }
            else if (obj is JObject)
            {
                var token = obj as JToken;

                var bodyObject = token.ToObject<Dictionary<string, object>>();

                lookupObj = bodyObject;
            }

            var type = lookupObj?.GetType();

            if (type == null || type.IsPrimitive || type.IsAssignableFrom(typeof(string)))
            {
                return lookupObj;
            }
            else if (lookupObj is ExpandoObject)
            {
                return GetDictionary((IDictionary<string, object>)lookupObj);
            }
            else if (lookupObj is IReadOnlyDictionary<string, object>)
            {
                var roDict = lookupObj as IReadOnlyDictionary<string, object>;

                var dict = roDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                return GetDictionary(dict);
            }
            else if (lookupObj is IDictionary<string, object>)
            {
                return GetDictionary(lookupObj as IDictionary<string, object>);
            }
            else if(lookupObj is System.Collections.IEnumerable)
            {
                var enumerable = lookupObj as System.Collections.IEnumerable;

                var result = new List<object>(enumerable.Cast<object>())
                    .Select(item => SanitizeParam(null, item))
                    .ToList();

                return result;
            }
            else if(lookupObj is Exception)
            {
                return GetException(type, lookupObj as Exception);
            }
            else if (type.IsClass)
            {
                return GetObject(type, lookupObj);
            }
            else
            {
                return lookupObj;
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

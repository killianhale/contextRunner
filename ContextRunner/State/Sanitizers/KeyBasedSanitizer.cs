using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ContextRunner.State.Sanitizers
{
    public class KeyBasedSanitizer : ISanitizer
    {
        private readonly IEnumerable<string> _sanitizedKeys;
        private readonly int _maxDepth;

        public KeyBasedSanitizer(IEnumerable<string> sanitizedKeys, int maxDepth = 10)
        {
            _sanitizedKeys = sanitizedKeys;
            _maxDepth = maxDepth;
        }

        public dynamic Sanitize(KeyValuePair<string, object> contextParam)
        {
            if (contextParam.Key.ToLower() == "request")
            {
                return contextParam.Value;
            }

            return SanitizeParam(contextParam.Key, contextParam.Value, 0);
        }

        private bool ShouldBeSanitized(string propName)
        {
            var key = propName
                   .Replace("-", string.Empty)
                   .Replace("_", string.Empty)
                   .ToLower();

            return _sanitizedKeys.Contains(key);
        }

        private object SanitizeParam(string propName, object obj, int level)
        {
            if (level == _maxDepth) return "~truncated~";
            
            if (propName != null && ShouldBeSanitized(propName))
            {
                return null;
            }

            var lookupObj = obj;


            if (obj is JValue jVal)
            {
                return jVal.Value;
            }
            else if (obj is JArray jArray)
            {
                var token = (JToken) obj;
                
                var bodyArray = token.ToArray().Select(item => jArray.Children().FirstOrDefault()?.Type == JTokenType.Object
                    ? item.ToObject<Dictionary<string, object>>()
                    : item as object).ToArray();

                lookupObj = bodyArray;
            }
            else if (obj is JObject)
            {
                var token = (JToken) obj;

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
                return GetDictionary((IDictionary<string, object>)lookupObj, level);
            }
            else if (lookupObj is IReadOnlyDictionary<string, object> roDict)
            {
                var dict = roDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                return GetDictionary(dict, level);
            }
            else if (lookupObj is IDictionary<string, object> dictionary)
            {
                return GetDictionary(dictionary, level);
            }
            else if(lookupObj is IEnumerable enumerable)
            {
                var result = new List<object>(enumerable.Cast<object>())
                    .Select(item => SanitizeParam(null, item, level + 1))
                    .ToList();

                return result;
            }
            else if(lookupObj is Exception exception)
            {
                return GetException(type, exception, level);
            }
            else if (type.IsClass)
            {
                return GetObject(type, lookupObj, level);
            }
            else
            {
                return lookupObj;
            }
        }

        private object GetException(Type type, Exception ex, int level)
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
                    val = SanitizeParam((string)key, ex.Data[key], level + 1);

                    data.Add((string)key, val);
                }
            }

            if(data.Any())
            {
                dictionary.Add("Data", data);
            }

            return expando;
        }

        private object GetObject(Type type, object obj, int level)
        {
            var expando = new ExpandoObject();
            var dictionary = (IDictionary<string, object>)expando;

            var props = type.GetProperties()
                    .Where(p => p.GetIndexParameters().Length == 0)
                    .ToList();

            foreach (var property in props)
            {
                var val = SanitizeParam(property.Name, property.GetValue(obj), level + 1);

                dictionary[property.Name] = val;
            }

            return expando;
        }

        private object GetDictionary(IDictionary<string, object> dictionary, int level)
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
                            : SanitizeParam(key, dictionary[key], level + 1);

                        return result;
                    }
                );
        }
    }
}

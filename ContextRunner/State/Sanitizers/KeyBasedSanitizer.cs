using System.Collections;
using System.Dynamic;
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

        public dynamic? Sanitize(KeyValuePair<string, object?> contextParam)
        {
            return contextParam.Key.Equals("request", StringComparison.CurrentCultureIgnoreCase) 
                ? contextParam.Value 
                : SanitizeParam(contextParam.Key, contextParam.Value, 0);
        }

        private bool ShouldBeSanitized(string propName)
        {
            var key = propName
                   .Replace("-", string.Empty)
                   .Replace("_", string.Empty)
                   .ToLower();

            return _sanitizedKeys.Contains(key);
        }

        private object? SanitizeParam(string? propName, object? obj, int level)
        {
            if(obj == null) return "null";
            if (level == _maxDepth) return "~truncated~";
            
            if (propName != null && ShouldBeSanitized(propName))
            {
                return null;
            }

            var lookupObj = obj;


            switch (obj)
            {
                case JValue jVal:
                    return jVal.Value;
                case JArray jArray:
                {
                    var token = (JToken) obj;

                    var bodyArray = token.ToArray().Select(item =>
                        jArray.Children().FirstOrDefault()?.Type == JTokenType.Object
                            ? item.ToObject<Dictionary<string, object>>()
                            : item as object
                    ).ToArray();

                    lookupObj = bodyArray;
                    break;
                }
                case JObject:
                {
                    var token = (JToken) obj;

                    var bodyObject = token.ToObject<Dictionary<string, object>>();

                    lookupObj = bodyObject;
                    break;
                }
            }

            var type = lookupObj?.GetType();

            if (type == null || type.IsPrimitive || type.IsAssignableFrom(typeof(string)))
            {
                return lookupObj;
            }
            
            switch (lookupObj)
            {
                case ExpandoObject expandoObject:
                    return GetDictionary(expandoObject, level);
                case IReadOnlyDictionary<string, object?> roDict:
                {
                    var dict = roDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    return GetDictionary(dict, level);
                }
                case IDictionary<string, object?> dictionary:
                    return GetDictionary(dictionary, level);
                case IEnumerable enumerable:
                {
                    var result = new List<object>(enumerable.Cast<object>())
                        .Select(item => SanitizeParam(null, item, level + 1))
                        .ToList();

                    return result;
                }
                case Exception exception:
                    return GetException(type, exception, level);
                default:
                {
                    return type.IsClass ? GetObject(type, lookupObj, level) : lookupObj;
                }
            }
        }

        private dynamic GetException(Type type, Exception ex, int level)
        {
            var expando = new ExpandoObject();
            var dictionary = (IDictionary<string, object?>)expando;

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

            var data = new Dictionary<string, object?>();

            foreach (var key in ex.Data.Keys)
            {
                if (key is not string stringKey) continue;
                
                var val = SanitizeParam(stringKey, ex.Data[stringKey], level + 1);

                data.Add(stringKey, val);
            }

            if(data.Count != 0)
            {
                dictionary.Add("Data", data);
            }

            return expando;
        }

        private object GetObject(Type type, object? obj, int level)
        {
            var expando = new ExpandoObject();
            var dictionary = (IDictionary<string, object?>)expando;

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

        private dynamic GetDictionary(IDictionary<string, object?> dictionary, int level)
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

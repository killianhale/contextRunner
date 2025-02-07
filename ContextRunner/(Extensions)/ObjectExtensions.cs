using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace ContextRunner
{
    public static class ObjectExtensions
    {
        public static dynamic ToShallowObject(this object obj, int maxDepth = 2)
        {
            return GetObject(0, maxDepth, obj.GetType(), obj);
        }
        
        private static dynamic GetObject(int level, int maxDepth, Type type, object? obj)
        {
            if (obj == null) return "null";
            if (level == maxDepth) return "~truncated~";
            
            var expando = new ExpandoObject();
            ICollection<KeyValuePair<string, object?>> collection = expando;

            var props = type.GetProperties()
                .Where(p => p.GetIndexParameters().Length == 0)
                .ToList();

            foreach (var property in props)
            {
                var val = GetObject(
                    level + 1,
                    maxDepth,
                    property.PropertyType,
                    property.GetValue(obj)
                );

                collection.Add(new KeyValuePair<string, object?>(property.Name, val));
            }

            return expando;
        }
    }
}
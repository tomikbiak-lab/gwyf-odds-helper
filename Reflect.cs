using System;
using System.Collections;
using System.Reflection;

namespace OddsHelper
{
    internal static class Reflect
    {
        internal const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static FieldInfo Field(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var f = t.GetField(name, Flags | BindingFlags.Static);
                if (f != null)
                    return f;
            }
            return null;
        }

        internal static T Get<T>(object obj, string field, T fallback)
        {
            if (obj == null)
                return fallback;
            var f = Field(obj.GetType(), field);
            if (f == null)
                return fallback;
            return f.GetValue(obj) is T t ? t : fallback;
        }

        internal static T GetStatic<T>(Type type, string field, T fallback)
        {
            var f = Field(type, field);
            return f != null && f.GetValue(null) is T t ? t : fallback;
        }

        internal static void Set(object obj, string field, object value) => Field(obj.GetType(), field)?.SetValue(obj, value);

        internal static object Call(object obj, string method, params object[] args)
        {
            for (var t = obj.GetType(); t != null; t = t.BaseType)
            {
                var m = t.GetMethod(method, Flags | BindingFlags.DeclaredOnly);
                if (m != null)
                    return m.Invoke(obj, args);
            }
            return null;
        }

        internal static object Prop(object obj, string name)
        {
            for (var t = obj.GetType(); t != null; t = t.BaseType)
            {
                var p = t.GetProperty(name, Flags | BindingFlags.DeclaredOnly);
                if (p != null)
                    return p.GetValue(obj);
            }
            return null;
        }

        internal static int Count(object collection) => collection is ICollection c ? c.Count : 0;
    }
}

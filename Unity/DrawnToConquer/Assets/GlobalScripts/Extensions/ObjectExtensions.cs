using System;
using System.Linq;

namespace Assets.Scripts.Extensions
{

    public static class ObjectExtensions
    {
        public static T Also<T>(this T obj, Action<T> action)
        {
            action(obj);
            return obj;
        }

        public static bool IsOneOf<T>(this T obj, params T[] values)
        {
            return values.Contains(obj);
        }

    }
}

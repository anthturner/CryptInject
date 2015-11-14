using System;
using System.Reflection;

namespace CryptInject
{
    internal static class SecurityExtensions
    {
        internal static object GetNullValue(this PropertyInfo property)
        {
            return CastToProperty(null, property);
        }

        internal static void NullProperty<T>(this T obj, string propertyName)
        {
            var propertyInfo = typeof (T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (propertyInfo != null)
                NullProperty(propertyInfo, obj);
        }

        internal static void NullProperty<T>(this PropertyInfo property, T obj, int index = -1)
        {
            if (property.PropertyType.IsArray && index == -1)
            {
                var arrayLen = ((Array) property.GetValue(obj)).Length;
                for (var i = 0; i < arrayLen; i++)
                    NullProperty(property, obj, i);
            }

            if (property.PropertyType == typeof (string))
            {
                //if (index > -1)
                //{
                //    var str = property.GetValue(obj, new object[] {index}) as string;
                //    str.DestroyString();
                //}
            }
            else
            {
                if (index > -1)
                {
                    property.SetValue(obj, CastToProperty(null, property), new object[] { index });
                }
                else
                {
                    property.SetValue(obj, CastToProperty(null, property));
                }
            }

            // todo: config value for turning this on and off
            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();
        }

        internal static object CastToProperty(this object value, PropertyInfo property)
        {
            if (value == null)
            {
                if (property.PropertyType.IsValueType)
                    return Activator.CreateInstance(property.PropertyType);
                else
                    return null;
            }
            return value;
        }

        internal static void DestroyString(this string str)
        {
            return; // stub out
            unsafe
            {
                fixed (char* ptr = str)
                {
                    for (int i = 0; i < str.Length; i++)
                        ptr[i] = '\0';
                }
            }
        }
    }
}

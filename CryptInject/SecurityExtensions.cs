using System;
using System.Collections.Generic;
using System.Reflection;

namespace CryptInject
{
    public static class SecurityExtensions
    {
        internal static object DataDisplayWindowsLock = new object();
        internal static List<IntPtr> DataDisplayWindows { get; private set; }

        static SecurityExtensions()
        {
            DataDisplayWindows = new List<IntPtr>();
        }

        public static void MarkAsDataDisplayWindow(this IntPtr windowPtr)
        {
            lock (DataDisplayWindowsLock)
            {
                if (DataDisplayWindows.Contains(windowPtr))
                    return;
                DataDisplayWindows.Add(windowPtr);
            }
        }

        internal static void RunOnAllDataDisplayWindows(Action<IntPtr> action)
        {
            lock (DataDisplayWindowsLock)
            {
                foreach (var wnd in DataDisplayWindows)
                {
                    action.Invoke(wnd);
                }
            }
        }

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

        internal static void NullField<T>(this T obj, string fieldName)
        {
            var fieldInfo = typeof(T).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo != null)
                NullField(fieldInfo, obj);
        }

        internal static void NullProperty<T>(this PropertyInfo property, T obj, int index = -1)
        {
            if (property.PropertyType.IsArray && index == -1)
            {
                var arrayLen = ((Array) property.GetValue(obj)).Length;
                for (var i = 0; i < arrayLen; i++)
                    NullProperty(property, obj, i);
            }

            if (index > -1)
            {
                property.SetValue(obj, CastToProperty(null, property), new object[] { index });
            }
            else
            {
                property.SetValue(obj, CastToProperty(null, property));
            }

            // todo: config value for turning this on and off
            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();
        }

        internal static void NullField<T>(this FieldInfo field, T obj)
        {
            if (field.FieldType.IsArray)
            {
                var array = (Array)field.GetValue(obj);
                var arrayLen = ((Array)field.GetValue(obj)).Length;
                for (var i = 0; i < arrayLen; i++)
                {
                    array.SetValue(CastToType(null, array.GetValue(i).GetType()), i);
                }
            }
            
            field.SetValue(obj, CastToField(null, field));

            // todo: config value for turning this on and off
            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();
        }

        internal static object CastToProperty(this object value, PropertyInfo property)
        {
            return CastToType(value, property.PropertyType);
        }

        internal static object CastToField(this object value, FieldInfo field)
        {
            return CastToType(value, field.FieldType);
        }

        internal static object CastToType(this object value, Type type)
        {
            if (value == null)
            {
                if (type.IsValueType)
                    return Activator.CreateInstance(type);
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

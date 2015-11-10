using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace CryptInject.Proxy
{
    internal sealed class EncryptedProperty
    {
        internal string KeyAlias { get; private set; }

        internal PropertyInfo Original { get; private set; }
        internal PropertyInfo Cache { get; private set; }
        internal PropertyInfo Backing { get; private set; }

        internal List<WeakReference> Instantiated { get; private set; }

        internal string Name { get; private set; }
        internal bool HasCache { get { return Cache != null; } }

        internal EncryptedProperty(PropertyInfo originalProperty)
        {
            Name = originalProperty.Name;
            Original = originalProperty;
            Backing = originalProperty.DeclaringType.GetProperty(DataStorageMixinFactory.BACKING_PROPERTY_PREFIX + Name);
            Cache = originalProperty.DeclaringType.GetProperty(DataStorageMixinFactory.CACHE_PROPERTY_PREFIX + Name);
            KeyAlias = originalProperty.GetCustomAttribute<EncryptableAttribute>().KeyAlias;
            Instantiated = new List<WeakReference>();
        }

        internal object GetCacheValue(object obj)
        {
            if (!HasCache)
                return null;
            return GetCacheProperty(obj).GetValue(obj, null);
        }
        internal void SetCacheValue(object obj, object value)
        {
            if (!HasCache)
                return;
            GetCacheProperty(obj).SetValue(obj, value, null);
        }
        internal void ClearCacheValue(object obj)
        {
            lock (Instantiated)
            {
                foreach (var instance in Instantiated)
                {
                    if (instance.Target is string)
                    {
                        ((string) instance.Target).DestroyString();
                    }
                    else
                    {
                        // todo: Destroy reference
                        //var gcAlloc = GCHandle.Alloc(instance.Target, GCHandleType.Weak);
                        //var size = Marshal.SizeOf(instance.Target);

                        //unsafe
                        //{
                        //    var gcPtr = (int*) ((IntPtr) gcAlloc).ToPointer();
                        //    for (int i = 0; i < size; i++)
                        //    {
                        //        gcPtr[i] = 0;
                        //    }
                        //}
                    }
                }
                Instantiated.Clear();

                if (!HasCache)
                    return;
                var cacheProperty = GetCacheProperty(obj);
                cacheProperty.NullProperty(obj);
            }
        }

        internal byte[] GetBackingValue(object obj)
        {
            return (byte[])GetBackingProperty(obj).GetValue(obj, null);
        }
        internal void SetBackingValue(object obj, byte[] value)
        {
            GetBackingProperty(obj).SetValue(obj, value, null);
        }

        private PropertyInfo GetBackingProperty(object obj)
        {
            // Handle deserialization cases where the CLR won't unify identical types (mumble mumble)
            if (obj.GetType().Equals(Backing.DeclaringType))
                return Backing;
            else
                return obj.GetType().GetProperty(Backing.Name, Backing.PropertyType);
        }

        private PropertyInfo GetCacheProperty(object obj)
        {
            // Handle deserialization cases where the CLR won't unify identical types (mumble mumble)
            if (obj.GetType().Equals(Cache.DeclaringType))
                return Cache;
            else
                return obj.GetType().GetProperty(Cache.Name, Cache.PropertyType);
        }
    }
}

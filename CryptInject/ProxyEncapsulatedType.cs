using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Castle.Core.Internal;
using Castle.DynamicProxy;

namespace CryptInject
{
    internal abstract class ProxyEncapsulatedType
    {
        internal Type OriginalType { get; set; }
        internal abstract Type GetProxyType();
        internal abstract Type GetStorageMixinType();
        internal abstract object GetValue(object proxyObject, string propertyName);
        internal abstract void SetValue(object proxyObject, string propertyName, object value);
        internal abstract void ClearSensitiveData(object proxyObject);
        internal abstract void ClearSensitiveData(string keyAlias);
        internal abstract void ClearSensitiveData(object proxyObject, string keyAlias);
        internal abstract bool IsManagingProperty(string propertyName);
        internal abstract void CleanupDeadReferences();
    }

    internal sealed class ProxyEncapsulatedType<T> : ProxyEncapsulatedType where T : class
    {
        private static ProxyGenerator Generator { get; set; }
        
        private Type DataStorageMixin { get; set; }
        private Dictionary<string, PropertyProxies> Properties { get; set; }
        internal EncryptionProxyConfiguration Configuration { get; private set; }
        internal List<ProxyInstanceDetail> Instances { get; private set; }
        
        internal ProxyEncapsulatedType(EncryptionProxyConfiguration configuration = null)
        {
            Configuration = configuration ?? new EncryptionProxyConfiguration();
            if (Generator == null) Generator = new ProxyGenerator();
            OriginalType = typeof(T);
            Instances = new List<ProxyInstanceDetail>();
            DataStorageMixin = DataStorageMixinFactory.Generate<T>().GetType();

            var sampleType = GenerateProxy();
            Properties = DataStorageMixinFactory.GetEncryptionEligibleProperties(sampleType.GetType()).ToDictionary(propKey => propKey.Name, propValue => new PropertyProxies(propValue));
        }

        internal T Create()
        {
            CleanupDeadReferences();
            var instance = GenerateProxy();
            Instances.Add(new ProxyInstanceDetail(this, new WeakReference(instance)));
            return instance;
        }

        internal override bool IsManagingProperty(string propertyName)
        {
            return Properties.ContainsKey(propertyName);
        }

        internal override Type GetProxyType()
        {
            return GenerateProxy().GetType();
        }

        internal override Type GetStorageMixinType()
        {
            return DataStorageMixin;
        }

        internal override object GetValue(object proxyObject, string propertyName)
        {
            var property = GetProperty(propertyName);

            var encryptedValue = property.GetBackingValue(proxyObject);
            if (encryptedValue == null) // user doesn't have access or there's no data anyways
                return CastReturnValue(property.Original, null);

            object cacheValue = null;
            if (property.HasCache)
            {
                cacheValue = property.GetCacheValue(proxyObject);
                if (cacheValue == null)
                {
                    cacheValue = Configuration.AccessValue(property.Original, encryptedValue);
                    property.SetCacheValue(proxyObject, cacheValue);
                    
                }
                else if (!cacheValue.Equals(CastReturnValue(property.Original, null))) // todo: this is always false, we need to do better or reads on non-primitives will be slow
                {
                    var encryptedCachedObject = Configuration.MutateValue(property.Original, cacheValue);
                    property.SetBackingValue(proxyObject, encryptedCachedObject);
                }
            }
            else
                cacheValue = CastReturnValue(property.Original, Configuration.AccessValue(property.Original, encryptedValue));

            return cacheValue;
        }

        internal override void SetValue(object proxyObject, string propertyName, object value)
        {
            var property = GetProperty(propertyName);

            var mutatedValue = Configuration.MutateValue(property.Original, value);
            if (mutatedValue != null)
            {
                if (property.HasCache)
                    property.Cache.SetValue(proxyObject, value);

                property.Backing.SetValue(proxyObject, mutatedValue);
            }
        }

        internal override void ClearSensitiveData(object proxyObject)
        {
            CleanupDeadReferences();
            Properties.Where(p => p.Value.HasCache)
                .ForEach(p => p.Value.SetCacheValue(proxyObject, CastReturnValue(p.Value.Original, null)));
        }

        internal override void ClearSensitiveData(string keyAlias)
        {
            CleanupDeadReferences();
            Instances.ForEach(i => ClearSensitiveData(i.Reference.Target, keyAlias));
        }

        internal override void ClearSensitiveData(object proxyObject, string keyAlias)
        {
            Properties.Where(p => p.Value.HasCache && p.Value.KeyAlias.Equals(keyAlias))
                .ForEach(p => p.Value.SetCacheValue(proxyObject, CastReturnValue(p.Value.Original, null)));
        }

        internal override void CleanupDeadReferences()
        {
            Instances.RemoveAll(i => !i.IsAlive);
        }

        private T GenerateProxy()
        {
            var options = new ProxyGenerationOptions(new CryptInjectHook());
            //var options = new ProxyGenerationOptions();
            options.AddMixinInstance(Activator.CreateInstance(DataStorageMixin));
            return Generator.CreateClassProxy<T>(options, new EncryptedDataStorageInterceptor());
        }

        private object CastReturnValue(PropertyInfo property, object value)
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

        private PropertyProxies GetProperty(string propertyName)
        {
            if (!Properties.ContainsKey(propertyName))
                throw new KeyNotFoundException("No property of that name registered.");
            return Properties[propertyName];
        }

        internal sealed class PropertyProxies
        {
            internal string KeyAlias { get; private set; }

            internal PropertyInfo Original { get; private set; }
            internal PropertyInfo Cache { get; private set; }
            internal PropertyInfo Backing { get; private set; }

            internal string Name { get; private set; }
            internal bool HasCache { get { return Cache != null; } }

            internal PropertyProxies(PropertyInfo originalProperty)
            {
                Name = originalProperty.Name;
                Original = originalProperty;
                Backing = originalProperty.DeclaringType.GetProperty(DataStorageMixinFactory.BACKING_PROPERTY_PREFIX + Name);
                Cache = originalProperty.DeclaringType.GetProperty(DataStorageMixinFactory.CACHE_PROPERTY_PREFIX + Name);
                KeyAlias = originalProperty.GetCustomAttribute<EncryptableAttribute>().KeyAlias;
            }

            internal object GetCacheValue(object obj)
            {
                return GetCacheProperty(obj).GetValue(obj, null);
            }
            internal void SetCacheValue(object obj, object value)
            {
                GetCacheProperty(obj).SetValue(obj, value, null);
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

        internal sealed class ProxyInstanceDetail
        {
            internal ProxyEncapsulatedType ProxyType { get; private set; }
            internal WeakReference Reference { get; private set; }
            internal bool IsAlive { get { return Reference.IsAlive; } }

            internal ProxyInstanceDetail(ProxyEncapsulatedType proxyType, WeakReference reference)
            {
                ProxyType = proxyType;
                Reference = reference;
            }

            internal bool References(object obj)
            {
                return (IsAlive && ReferenceEquals(obj, Reference.Target));
            }
        }
    }
    
    [DataContract]
    [Serializable]
    internal class CryptInjectHook : AllMethodsHook
    {
        public override bool ShouldInterceptMethod(Type type, MethodInfo methodInfo)
        {
            if (!methodInfo.Name.StartsWith("get_") && !methodInfo.Name.StartsWith("set_"))
                return false;

            if (methodInfo.Name.Substring(4).StartsWith(DataStorageMixinFactory.BACKING_PROPERTY_PREFIX))
                return false;

            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using CryptInject.Keys;

namespace CryptInject.Proxy
{
    internal class EncryptedInstance
    {
        internal EncryptedType EncryptedType { get; private set; }
        internal Keyring InstanceKeyring { get; private set; }
        internal WeakReference Reference { get; private set; }

        internal bool IsAlive
        {
            get { return Reference.IsAlive; }
        }

        internal bool References(object obj)
        {
            return (IsAlive && ReferenceEquals(obj, Reference.Target));
        }

        internal EncryptedInstance(EncryptedType type, object instance)
        {
            EncryptedType = type;
            Reference = new WeakReference(instance);
            InstanceKeyring = new Keyring();
        }
        
        internal object GetValue(string propertyName)
        {
            var property = EncryptedType.Properties[propertyName];

            var encryptedValue = property.GetBackingValue(Reference.Target);
            if (encryptedValue == null) // user doesn't have access or there's no data anyways
                return property.Original.GetNullValue();

            object cacheValue;
            if (property.HasCache)
            {
                cacheValue = property.GetCacheValue(Reference.Target);
                if (cacheValue == null)
                {
                    cacheValue = EncryptedType.Configuration.AccessValue(this, property.Original, encryptedValue);
                    property.SetCacheValue(Reference.Target, cacheValue);

                }
                else if (!cacheValue.Equals(property.Original.GetNullValue()))
                    // todo: this is always false, we need to do better or reads on non-primitives will be slow
                {
                    var encryptedCachedObject = EncryptedType.Configuration.MutateValue(this, property.Original,
                        cacheValue);
                    property.SetBackingValue(Reference.Target, encryptedCachedObject);
                }
            }
            else
            {
                cacheValue = EncryptedType.Configuration.AccessValue(this, property.Original, encryptedValue).CastToProperty(property.Original);
                property.Instantiated.Add(new WeakReference(cacheValue));
            }

            return cacheValue;
        }

        internal void SetValue(string propertyName, object value)
        {
            var property = EncryptedType.Properties[propertyName];

            var mutatedValue = EncryptedType.Configuration.MutateValue(this, property.Original, value);
            if (mutatedValue != null)
            {
                if (property.HasCache)
                    property.Cache.SetValue(Reference.Target, value);

                property.Backing.SetValue(Reference.Target, mutatedValue);
            }
        }

        internal void UpdateFromKeyringScopes()
        {
            var effectiveKeyring = Reference.Target.GetReadOnlyUnifiedKeyring();

            foreach (var property in EncryptedType.Properties.Values)
            {
                if (!effectiveKeyring.HasKey(property.KeyAlias) || effectiveKeyring[property.KeyAlias].Locked)
                    property.ClearCacheValue(Reference.Target);
            }
        }
    }
}

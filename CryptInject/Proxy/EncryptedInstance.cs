using System;
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
            // todo: boxing -- needs to be fixed for performance reasons
            var property = EncryptedType.Properties[propertyName];

            var encryptedValue = property.GetBackingValue(Reference.Target);
            if (encryptedValue == null) // user doesn't have access or there's no data anyways
                return property.Original.GetNullValue();

            object cacheValue;
            if (property.HasCache)
            {
                cacheValue = property.GetCacheValue(Reference.Target);
                
                if (cacheValue == property.Original.GetNullValue() || (cacheValue != null && cacheValue.Equals(property.Original.GetNullValue())))
                {
                    cacheValue = EncryptedType.Configuration.AccessValue(this, property.Original, encryptedValue);
                    property.SetCacheValue(Reference.Target, cacheValue);
                }
                else if (!property.Original.PropertyType.IsValueType && property.Original.PropertyType != typeof(string)) // reference type
                {
                    // update backing value just in case the referenced object's contents changed
                    // todo: make this into a tunable option for cacheback frequency
                    var encryptedCachedObject = EncryptedType.Configuration.MutateValue(this, property.Original, cacheValue);
                    property.SetBackingValue(Reference.Target, encryptedCachedObject);
                }

                if (EncryptedType.Configuration.IsPeriodicallyAccessibleKey(this, property.Original))
                {
                    cacheValue = EncryptedType.Configuration.AccessValue(this, property.Original, encryptedValue);
                }
            }
            else
            {
                // this will never be hit under current config (everything has cache)
                cacheValue = EncryptedType.Configuration.AccessValue(this, property.Original, encryptedValue).CastToProperty(property.Original);
                property.Instantiated.Add(new WeakReference(cacheValue));
            }

            return cacheValue ?? (cacheValue = property.Original.GetNullValue());
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

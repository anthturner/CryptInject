using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using CryptInject.Keys;
using CryptInject.Proxy;

namespace CryptInject
{
    public sealed class EncryptionProxyConfiguration
    {
        public delegate byte[] ProxySerializeFunctionDelegate(PropertyInfo property, object serializableObject);
        public delegate object ProxyDeserializeFunctionDelegate(PropertyInfo property, byte[] serializedObjectData);

        /// <summary>
        /// Whether or not to throw an Exception when data cannot be accessed during a get. Otherwise, default(T) will be returned.
        /// </summary>
        public bool ThrowExceptionOnAccessorFailure { get; set; }

        /// <summary>
        /// Whether or not to throw an Exception when data cannot be changed during a set.
        /// </summary>
        public bool ThrowExceptionOnMutatorFailure { get; set; }

        private ProxySerializeFunctionDelegate ProxySerializeFunction { get; set; }
        private ProxyDeserializeFunctionDelegate ProxyDeserializeFunction { get; set; }

        private Dictionary<string, string> PropertyKeyNameCache { get; set; }

        private static ProxySerializeFunctionDelegate DefaultProxySerializeFunction = (property, serializableObject) =>
        {
            var bf = new BinaryFormatter();
            var ms = new MemoryStream();
            bf.Serialize(ms, serializableObject);
            ms.Seek(0, SeekOrigin.Begin);
            return ms.ToArray();
        };

        private static ProxyDeserializeFunctionDelegate DefaultProxyDeserializeFunction = (property, bytes) =>
        {
            var bf = new BinaryFormatter();
            var ms = new MemoryStream(bytes);
            ms.Seek(0, SeekOrigin.Begin);
            return bf.Deserialize(ms);
        };

        public EncryptionProxyConfiguration(ProxySerializeFunctionDelegate serializeFunction = null, ProxyDeserializeFunctionDelegate deserializeFunction = null)
        {
            if (serializeFunction == null)
                serializeFunction = DefaultProxySerializeFunction;
            if (deserializeFunction == null)
                deserializeFunction = DefaultProxyDeserializeFunction;

            ProxySerializeFunction = serializeFunction;
            ProxyDeserializeFunction = deserializeFunction;

            PropertyKeyNameCache = new Dictionary<string, string>();
            ThrowExceptionOnAccessorFailure = false;
            ThrowExceptionOnMutatorFailure = false;
        }

        #region Cryptography Invocation
        internal object AccessValue(EncryptedInstance instance, PropertyInfo property, byte[] bytes)
        {
            if (bytes != null && bytes.Length > 0)
            {
                var keyDescriptor = GetEncryptionKey(instance, property);
                if (keyDescriptor != null && !keyDescriptor.Locked)
                {
                    byte[] decrypted;
                    try
                    {
                        decrypted = keyDescriptor.Decrypt(property, bytes);
                    }
                    catch (Exception ex)
                    {
                        if (ThrowExceptionOnAccessorFailure)
                            throw new UnauthorizedAccessException("Cannot read value from encrypted memory.", ex);
                        else
                            return null;
                    }
                    return ProxyDeserializeFunction(property, decrypted);
                }
                else if (ThrowExceptionOnAccessorFailure)
                    throw new UnauthorizedAccessException("No key data loaded for required name '" + GetEncryptionKeyName(property) + "' or key is locked.");
            }

            return null;
        }

        internal byte[] MutateValue(EncryptedInstance instance, PropertyInfo property, object obj)
        {
            if (ProxySerializeFunction == null)
                throw new Exception("ProxySerializeFunction not bound.");

            if (obj != null)
            {
                var keyDescriptor = GetEncryptionKey(instance, property);
                if (keyDescriptor != null && !keyDescriptor.Locked)
                {
                    var serialized = ProxySerializeFunction(property, obj);
                    try
                    {
                        return keyDescriptor.Encrypt(property, serialized);
                    }
                    catch (Exception ex)
                    {
                        if (ThrowExceptionOnMutatorFailure)
                            throw new UnauthorizedAccessException("Cannot set value to encrypted memory.", ex);
                        else
                            return null;
                    }
                }
                else if (ThrowExceptionOnMutatorFailure)
                    throw new UnauthorizedAccessException("No key data loaded for required name '" + GetEncryptionKeyName(property) + "' or key is locked.");
            }

            return null;
        }

        internal bool IsPeriodicallyAccessibleKey(EncryptedInstance instance, PropertyInfo property)
        {
            var keyDescriptor = GetEncryptionKey(instance, property);
            if (keyDescriptor == null)
            {
                throw new UnauthorizedAccessException("Key '" + GetEncryptionKeyName(property) + "' not loaded");
            }
            return keyDescriptor.KeyData.IsPeriodicallyAccessibleKey(property);
        }

        private KeyDescriptor GetEncryptionKey(EncryptedInstance instance, PropertyInfo propertyInfo)
        {
            var keyAlias = GetEncryptionKeyName(propertyInfo);
            var unifiedKeyring = instance.Reference.Target.GetReadOnlyUnifiedKeyring();
            if (keyAlias != null && unifiedKeyring.HasKey(keyAlias))
                return unifiedKeyring.Keys.FirstOrDefault(k => k.Name == keyAlias);
            return null;
        }

        private string GetEncryptionKeyName(PropertyInfo propertyInfo)
        {
            if (PropertyKeyNameCache.ContainsKey(propertyInfo.Name))
            {
                return PropertyKeyNameCache[propertyInfo.Name];
            }

            var encryptionAttribute = propertyInfo.GetCustomAttribute<EncryptableAttribute>();
            if (encryptionAttribute == null)
                return null;

            PropertyKeyNameCache.Add(propertyInfo.Name, encryptionAttribute.KeyAlias);

            return encryptionAttribute.KeyAlias;
        }
        #endregion
    }
}

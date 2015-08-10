using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using CryptInject.Keys;

namespace CryptInject
{
    public sealed class EncryptionProxyConfiguration
    {
        public delegate byte[] ProxySerializeFunctionDelegate(PropertyInfo property, object serializableObject);
        public delegate object ProxyDeserializeFunctionDelegate(PropertyInfo property, byte[] serializedObjectData);

        /// <summary>
        /// Function to invoke when the proxy is serializing a property for encryption (default uses BinaryFormatter)
        /// </summary>
        public event ProxySerializeFunctionDelegate ProxySerializeFunction;

        /// <summary>
        /// Function to invoke when the proxy is deserializing a property after decryption (default uses BinaryFormatter)
        /// </summary>
        public event ProxyDeserializeFunctionDelegate ProxyDeserializeFunction;

        /// <summary>
        /// Whether or not to throw an Exception when data cannot be accessed during a get. Otherwise, default(T) will be returned.
        /// </summary>
        public bool ThrowExceptionOnAccessorFailure { get; set; }

        /// <summary>
        /// Whether or not to throw an Exception when data cannot be changed during a set.
        /// </summary>
        public bool ThrowExceptionOnMutatorFailure { get; set; }

        public EncryptionProxyConfiguration()
        {
            ThrowExceptionOnAccessorFailure = false;
            ThrowExceptionOnMutatorFailure = false;

            ProxySerializeFunction += (property, serializableObject) =>
            {
                var bf = new BinaryFormatter();
                var ms = new MemoryStream();
                bf.Serialize(ms, serializableObject);
                ms.Seek(0, SeekOrigin.Begin);
                return ms.ToArray();
            };
            ProxyDeserializeFunction += (property, bytes) =>
            {
                var bf = new BinaryFormatter();
                var ms = new MemoryStream(bytes);
                ms.Seek(0, SeekOrigin.Begin);
                return bf.Deserialize(ms);
            };
        }

        #region Cryptography Invocation
        internal object AccessValue(PropertyInfo property, byte[] bytes)
        {
            if (ProxySerializeFunction == null)
                throw new Exception("ProxyDeserializeFunction not bound.");

            if (bytes != null && bytes.Length > 0)
            {
                var keyDescriptor = GetEncryptionKey(property);
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

        internal byte[] MutateValue(PropertyInfo property, object obj)
        {
            if (ProxySerializeFunction == null)
                throw new Exception("ProxySerializeFunction not bound.");

            if (obj != null)
            {
                var keyDescriptor = GetEncryptionKey(property);
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

        private static KeyDescriptor GetEncryptionKey(PropertyInfo propertyInfo)
        {
            var keyAlias = GetEncryptionKeyName(propertyInfo);
            if (keyAlias != null && EncryptionManager.Keyring.HasKey(keyAlias))
                return EncryptionManager.Keyring.Keys.FirstOrDefault(k => k.Name == keyAlias);
            return null;
        }

        private static string GetEncryptionKeyName(PropertyInfo propertyInfo)
        {
            var encryptionAttribute = propertyInfo.GetCustomAttribute<EncryptableAttribute>();
            if (encryptionAttribute == null)
                return null;
            return encryptionAttribute.KeyAlias;
        }
        #endregion
    }
}

using System;

namespace CryptInject
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class EncryptableAttribute : Attribute
    {
        internal string KeyAlias { get; set; }

        /// <summary>
        /// Marks this property as Encryptable by a proxy; this property must also be virtual.
        /// </summary>
        /// <param name="keyAlias">Key alias to use for encryption/decryption</param>
        public EncryptableAttribute(string keyAlias)
        {
            KeyAlias = keyAlias;
        }
    }
}

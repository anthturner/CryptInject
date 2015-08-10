using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace CryptInject.Keys.Builtin
{
    public sealed class HmacEncryptionKey : EncryptionKey
    {
        public enum HmacCipher
        {
            MD5,
            SHA1,
            SHA256,
            SHA384,
            SHA512
        }

        private HmacCipher Cipher { get; set; }

        /// <summary>
        /// Create/Load a data signing key based on HMAC ciphers
        /// </summary>
        /// <param name="key">Encryption key</param>
        /// <param name="cipher">HMAC cipher to use</param>
        /// <param name="chainedInnerKey">Key operation to run prior to this key</param>
        public HmacEncryptionKey(byte[] key, HmacCipher cipher = HmacCipher.SHA1, EncryptionKey chainedInnerKey = null) : base(key, chainedInnerKey)
        {
            Cipher = cipher;
        }

        public HmacEncryptionKey() : base(new byte[0], null) { }

        protected override byte[] Encrypt(PropertyInfo property, byte[] key, byte[] bytes)
        {
            using (var alg = GetAlgorithm())
            {
                alg.Key = key;
                var signature = alg.ComputeHash(bytes);
                
                var returnBytes = new List<byte>(2 + signature.Length + bytes.Length);
                returnBytes.AddRange(BitConverter.GetBytes((short) signature.Length));
                returnBytes.AddRange(signature);
                returnBytes.AddRange(bytes);

                return returnBytes.ToArray();
            }
        }

        protected override byte[] Decrypt(PropertyInfo property, byte[] key, byte[] bytes)
        {
            var signatureLength = BitConverter.ToInt16(bytes, 0);
            var signature = bytes.Skip(2).Take(signatureLength).ToArray();
            var data = bytes.Skip(2 + signatureLength).ToArray();

            using (var alg = GetAlgorithm())
            {
                alg.Key = key;
                if (alg.ComputeHash(data).SequenceEqual(signature))
                    return data;
            }
            return null;
        }

        protected override byte[] ExportData { get { return new byte[] { (byte)Cipher }; } set { Cipher = (HmacCipher)value[0]; } }

        private HMAC GetAlgorithm()
        {
            switch (Cipher)
            {
                case HmacCipher.MD5:
                    return new HMACMD5();
                case HmacCipher.SHA1:
                    return new HMACSHA1();
                case HmacCipher.SHA256:
                    return new HMACSHA256();
                case HmacCipher.SHA384:
                    return new HMACSHA384();
                case HmacCipher.SHA512:
                    return new HMACSHA512();
            }
            return null;
        }
    }
}

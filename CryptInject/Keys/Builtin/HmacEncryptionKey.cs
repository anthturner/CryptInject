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

                return CreateBinaryFrame(signature, bytes);
            }
        }

        protected override byte[] Decrypt(PropertyInfo property, byte[] key, byte[] bytes)
        {
            var frame = ExtractBinaryFrame(bytes);
            var signature = frame[0];
            var data = frame[1];
            
            using (var alg = GetAlgorithm())
            {
                alg.Key = key;
                if (alg.ComputeHash(data).SequenceEqual(signature))
                    return data;
            }
            return null;
        }

        protected override byte[] ExportData { get { return new byte[] { (byte)Cipher }; } set { Cipher = (HmacCipher)value[0]; } }
        protected override bool IsPeriodicallyAccessibleKey()
        {
            return false;
        }

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

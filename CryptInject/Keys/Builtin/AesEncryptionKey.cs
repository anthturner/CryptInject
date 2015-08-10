using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace CryptInject.Keys.Builtin
{
    public sealed class AesEncryptionKey : EncryptionKey
    {
        private RijndaelManaged Rijndael { get; set; }

        /// <summary>
        /// Create/Load an encryption key based around AES-256. Key must be 48 bytes in length.
        /// </summary>
        /// <param name="key">Encryption Key (32B) + IV (16B)</param>
        /// <param name="chainedInnerKey">Key operation to run prior to this key</param>
        public AesEncryptionKey(byte[] key, EncryptionKey chainedInnerKey = null) : base(key, chainedInnerKey)
        {
            Rijndael = new RijndaelManaged();
            Rijndael.BlockSize = 128; // Force adherence to AES
            try
            {
                Transform(new byte[] { 0, 1, 2, 3, 4 }, Rijndael.CreateEncryptor(GetKey(key), GetIV(key)));
            }
            catch (Exception ex)
            {
                throw new Exception("AES encryption test unsuccessful", ex);
            }
        }

        public AesEncryptionKey() : base(new byte[0], null)
        {
            Rijndael = new RijndaelManaged();
            Rijndael.BlockSize = 128; // Force adherence to AES
        }

        /// <summary>
        /// Create a new key based around AES-256.
        /// </summary>
        /// <param name="chainedInnerKey">Key operation to run prior to this key</param>
        /// <returns>New AES-256 key</returns>
        public static AesEncryptionKey Create(EncryptionKey chainedInnerKey = null)
        {
            var rijndael = new RijndaelManaged();
            rijndael.BlockSize = 128;
            rijndael.GenerateKey();
            rijndael.GenerateIV();

            var keyData = new List<byte>();
            keyData.AddRange(BitConverter.GetBytes((short)rijndael.Key.Length));
            keyData.AddRange(rijndael.Key);
            keyData.AddRange(BitConverter.GetBytes((short)rijndael.IV.Length));
            keyData.AddRange(rijndael.IV);

            return new AesEncryptionKey(keyData.ToArray(), chainedInnerKey);
        }

        protected override byte[] Encrypt(PropertyInfo property, byte[] key, byte[] bytes)
        {
            return Transform(bytes, Rijndael.CreateEncryptor(GetKey(key), GetIV(key)));
        }

        protected override byte[] Decrypt(PropertyInfo property, byte[] key, byte[] bytes)
        {
            return Transform(bytes, Rijndael.CreateDecryptor(GetKey(key), GetIV(key)));
        }

        protected override byte[] ExportData { get {return new byte[0];} set {} }

        private byte[] Transform(byte[] buffer, ICryptoTransform transform)
        {
            var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, transform, CryptoStreamMode.Write))
            {
                cs.Write(buffer, 0, buffer.Length);
            }
            return ms.ToArray();
        }

        private byte[] GetKey(byte[] keyData)
        {
            var keyLen = BitConverter.ToInt16(keyData, 0);
            return keyData.Skip(2).Take(keyLen).ToArray();
        }
        private byte[] GetIV(byte[] keyData)
        {
            var keyLen = BitConverter.ToInt16(keyData, 0);
            var ivLen = BitConverter.ToInt16(keyData, 2 + keyLen);
            return keyData.Skip(2 + keyLen + 2).Take(ivLen).ToArray();
        }
    }
}

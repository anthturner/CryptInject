using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace CryptInject.Keys.Builtin
{
    public sealed class TripleDesEncryptionKey : EncryptionKey
    {
        private TripleDES TripleDes { get; set; }
        public CipherMode CipherMode { get; private set; }
        
        /// <summary>
        /// Create/Load an encryption key based around 3DES (Triple-DES).
        /// </summary>
        /// <param name="key">Encryption key</param>
        /// <param name="cipherMode">Cipher mode to use (advanced users only)</param>
        /// <param name="chainedInnerKey">Key operation to run prior to this key</param>
        public TripleDesEncryptionKey(byte[] key, CipherMode cipherMode = CipherMode.CBC, EncryptionKey chainedInnerKey = null) : base(key, chainedInnerKey)
        {
            TripleDes = new TripleDESCryptoServiceProvider();
            TripleDes.Mode = cipherMode;
            TripleDes.Padding = PaddingMode.PKCS7;
            try
            {
                Transform(new byte[]{0,1,2,3,4}, TripleDes.CreateEncryptor(GetKey(key), GetIV(key)));
            }
            catch (Exception ex)
            {
                throw new Exception("3DES encryption test unsuccessful", ex);
            }
        }

        public TripleDesEncryptionKey() : base(new byte[0], null) { TripleDes = new TripleDESCryptoServiceProvider(); }

        /// <summary>
        /// Create a new encryption key based around 3DES (Triple-DES)
        /// </summary>
        /// <param name="blockSize">Block size to use for key</param>
        /// <param name="cipherMode">Cipher mode to use (advanced users only)</param>
        /// <param name="chainedInnerKey">Key operation to run prior to this key</param>
        /// <returns>New 3DES key</returns>
        public static TripleDesEncryptionKey Create(int blockSize = 64, CipherMode cipherMode = CipherMode.CBC, EncryptionKey chainedInnerKey = null)
        {
            var tripleDes = new TripleDESCryptoServiceProvider();
            tripleDes.Mode = cipherMode;
            tripleDes.Padding = PaddingMode.PKCS7;
            tripleDes.BlockSize = blockSize;
            tripleDes.GenerateKey();
            tripleDes.GenerateIV();

            var keyData = new List<byte>();
            keyData.AddRange(BitConverter.GetBytes((short)tripleDes.Key.Length));
            keyData.AddRange(tripleDes.Key);
            keyData.AddRange(BitConverter.GetBytes((short)tripleDes.IV.Length));
            keyData.AddRange(tripleDes.IV);

            return new TripleDesEncryptionKey(keyData.ToArray(), cipherMode, chainedInnerKey);
        }

        protected override byte[] Encrypt(PropertyInfo property, byte[] key, byte[] bytes)
        {
            return Transform(bytes, TripleDes.CreateEncryptor(GetKey(key), GetIV(key)));
        }

        protected override byte[] Decrypt(PropertyInfo property, byte[] key, byte[] bytes)
        {
            return Transform(bytes, TripleDes.CreateDecryptor(GetKey(key), GetIV(key)));
        }

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

        protected override byte[] ExportData
        {
            get
            {
                return new byte[] {(byte) TripleDes.Mode};
            }
            set
            {
                TripleDes.Mode = (CipherMode) value[0];
            }
        }
    }
}

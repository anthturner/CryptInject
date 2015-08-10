using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace CryptInject.Keys
{
    public abstract class EncryptionKey
    {
        private bool Disposed { get; set; }
        private byte[] Key { get; set; }
        private int Pad { get; set; }

        public EncryptionKey ChainedInnerKey { get; private set; }

        protected EncryptionKey(byte[] key, EncryptionKey chainedInnerKey)
        {
            ResetKey(key);
            ChainedInnerKey = chainedInnerKey;
        }

        protected abstract byte[] Encrypt(PropertyInfo property, byte[] key, byte[] bytes);
        protected abstract byte[] Decrypt(PropertyInfo property, byte[] key, byte[] bytes);
        protected abstract byte[] ExportData { get; set; }

        internal byte[] Encrypt(PropertyInfo property, byte[] bytes)
        {
            var innerProcessedData = ChainedInnerKey != null ? ChainedInnerKey.Encrypt(property, bytes) : bytes;

            var unlockedKey = GetUnlockedKey();
            var result = Encrypt(property, unlockedKey, innerProcessedData);
            Zero(unlockedKey);
            return result;
        }

        internal byte[] Decrypt(PropertyInfo property, byte[] bytes)
        {
            var unlockedKey = GetUnlockedKey();
            var unlockedData = Decrypt(property, unlockedKey, bytes);

            var result = ChainedInnerKey != null ? ChainedInnerKey.Decrypt(property, unlockedData) : unlockedData;

            Zero(unlockedKey);
            return result;
        }

        protected void ResetKey(byte[] key)
        {
            if (key == null || key.Length == 0)
            {
                Key = new byte[0];
                return;
            }

            if (key.Length%16 != 0)
                Pad = (16 - (key.Length%16));
            else
                Pad = 0;

            Key = new byte[key.Length + Pad];
            key.CopyTo(Key, 0);
            ProtectedMemory.Protect(Key, MemoryProtectionScope.SameProcess);
        }

        private byte[] GetUnlockedKey()
        {
            if (Disposed)
                throw new ObjectDisposedException("Key has been disposed");
            lock (Key)
            {
                var newKey = new byte[Key.Length - Pad];
                ProtectedMemory.Unprotect(Key, MemoryProtectionScope.SameProcess);
                Array.ConstrainedCopy(Key, 0, newKey, 0, Key.Length - Pad);
                ProtectedMemory.Protect(Key, MemoryProtectionScope.SameProcess);
                return newKey;
            }
        }

        internal byte[] Export()
        {
            var bytes = new List<byte>();
            var type = Encoding.ASCII.GetBytes(GetType().FullName);
            bytes.AddRange(BitConverter.GetBytes((short)type.Length)); // todo: maybe change to maxlen=256 so we can save a byte
            bytes.AddRange(type);
            
            var state = ExportData;
            if (state == null)
                bytes.Add(0);
            else
            {
                bytes.AddRange(BitConverter.GetBytes(state.Length));
                bytes.AddRange(state);
            }
            
            var key = GetUnlockedKey();
            bytes.AddRange(BitConverter.GetBytes(key.Length));
            bytes.AddRange(key);

            if (ChainedInnerKey != null)
                bytes.AddRange(ChainedInnerKey.Export());

            var arr = bytes.ToArray();
            bytes.Clear();
            return arr;
        }

        internal static EncryptionKey Import(byte[] keyData, int position)
        {
            var typeLen = BitConverter.ToInt16(keyData, position);
            var type = Encoding.ASCII.GetString(keyData.Skip(position + 2).Take(typeLen).ToArray());
            var stateLen = BitConverter.ToInt32(keyData, position + 2 + typeLen);
            var state = keyData.Skip(position + 2 + typeLen + 4).Take(stateLen).ToArray();
            var keyLen = BitConverter.ToInt32(keyData, position + 2 + typeLen + 4 + stateLen);
            var key = keyData.Skip(position + 2 + typeLen + 4 + stateLen + 4).Take(keyLen).ToArray();

            EncryptionKey innerKey = null;
            if (position + 2 + typeLen + 4 + stateLen + 4 + keyLen < keyData.Length)
            {
                innerKey = Import(keyData, position + 2 + typeLen + 4 + stateLen + 4 + keyLen);
            }

            // no inner key
            var encryptionKey = (EncryptionKey) Activator.CreateInstance(Type.GetType(type));
            encryptionKey.ExportData = state;
            encryptionKey.ResetKey(key);
            encryptionKey.ChainedInnerKey = innerKey;
            return encryptionKey;
        }

        /// <summary>
        /// Securely wipe the key material and dispose of the key
        /// </summary>
        public void Dispose()
        {
            if (!Disposed)
            {
                Zero(Key);
                Disposed = true;
            }
        }

        private void Zero(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0x00;
            }
        }
    }
}

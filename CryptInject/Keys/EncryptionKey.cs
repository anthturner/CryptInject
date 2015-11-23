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
        [Flags]
        public enum KeyAppliesTo : byte
        {
            Encryption = 0,
            Decryption = 1,
            Both = Encryption | Decryption
        }

        private bool Disposed { get; set; }
        private byte[] Key { get; set; }
        private int Pad { get; set; }

        public EncryptionKey ChainedInnerKey { get; private set; }

        protected EncryptionKey(byte[] key, EncryptionKey chainedInnerKey)
        {
            SetKey(key);
            ChainedInnerKey = chainedInnerKey;
        }

        protected abstract byte[] Encrypt(PropertyInfo property, byte[] key, byte[] bytes);
        protected abstract byte[] Decrypt(PropertyInfo property, byte[] key, byte[] bytes);
        protected abstract byte[] ExportData { get; set; }
        protected abstract bool IsPeriodicallyAccessibleKey();

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

        internal bool IsPeriodicallyAccessibleKey(PropertyInfo property)
        {
            if (ChainedInnerKey != null)
                return ChainedInnerKey.IsPeriodicallyAccessibleKey(property);
            return IsPeriodicallyAccessibleKey();
        }

        /// <summary>
        /// Set the key material to a given byte array
        /// </summary>
        /// <param name="key">Key material</param>
        protected void SetKey(byte[] key)
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
            encryptionKey.SetKey(key);
            encryptionKey.ChainedInnerKey = innerKey;
            return encryptionKey;
        }

        /// <summary>
        /// Extract a frame that is stored in the key
        /// </summary>
        /// <param name="frame">Byte array frame</param>
        /// <returns>List of fields in the frame</returns>
        protected static List<byte[]> ExtractBinaryFrame(byte[] frame)
        {
            var fields = new List<byte[]>();
            var position = 1;
            while (position < frame.Length-1)
            {
                int length = 0;
                switch ((int)frame[0])
                {
                    case 1: // byte
                        length = (int)frame[position];
                        break;
                    case 2: // short
                        length = BitConverter.ToInt16(frame, position);
                        break;
                    case 4: // int
                        length = BitConverter.ToInt32(frame, position);
                        break;
                    case 8: // long
                        length = BitConverter.ToInt32(frame, position);
                        break;
                    default:
                        throw new Exception("Frame preamble must be 1, 2, 4, or 8. This may be corrupt.");
                }

                var frameData = frame.Skip(position + (int) frame[0]).Take(length).ToArray();
                fields.Add(frameData);
                position += (int)frame[0] + frameData.Length;
            }
            return fields;
        }

        /// <summary>
        /// Create a frame to be stored with the key
        /// </summary>
        /// <param name="fields">Fields to store in frame</param>
        /// <returns>Byte array frame</returns>
        protected static byte[] CreateBinaryFrame(params byte[][] fields)
        {
            var maxLen = fields.Max(f => f.Length);
            var bytes = 0;
            if (maxLen < byte.MaxValue)
                bytes = 1;
            else if (maxLen < short.MaxValue)
                bytes = 2;
            else if (maxLen < int.MaxValue)
                bytes = 4;
            else if (maxLen < long.MaxValue)
                bytes = 8;
            else
                throw new ArgumentOutOfRangeException("fields", "Length of one of the fields given is longer than the Int64 register");

            var list = new List<byte>();
            list.Add((byte)bytes);
            foreach (var field in fields)
            {
                switch (bytes)
                {
                    case 1: // byte
                        list.Add((byte)field.Length);
                        break;
                    case 2: // short
                        list.AddRange(BitConverter.GetBytes((short)field.Length));
                        break;
                    case 4: // int
                        list.AddRange(BitConverter.GetBytes((int)field.Length));
                        break;
                    case 8: // long
                        list.AddRange(BitConverter.GetBytes((long)field.Length));
                        break;
                }
                list.AddRange(field);
            }
            return list.ToArray();
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CryptInject.Keys
{
    public sealed class KeyDescriptor
    {
        private bool _isLocked;
        public string Name { get; private set; }
        public EncryptionKey KeyData { get; private set; }

        internal delegate void KeyLockChangedDelegate(bool locked);
        internal event KeyLockChangedDelegate KeyLockChanged;

        internal KeyDescriptor(string name, EncryptionKey keyData)
        {
            Name = name;
            KeyData = keyData;
        }

        /// <summary>
        /// Lock or unlock the key. If the key is locked, requests for encryption or decryption are denied regardless of the key material loaded.
        /// </summary>
        public bool Locked
        {
            get { return _isLocked; }
            set
            {
                if (value)
                {
                    EncryptionManager.ProxiedTypes.ForEach(t => t.ClearSensitiveData(Name));
                }

                if (KeyLockChanged != null)
                    KeyLockChanged(value);

                _isLocked = value;
            }
        }

        internal byte[] Encrypt(PropertyInfo property, byte[] bytes)
        {
            if (Locked)
                return null;
            return KeyData.Encrypt(property, bytes);
        }

        internal byte[] Decrypt(PropertyInfo property, byte[] bytes)
        {
            if (Locked)
                return null;
            return KeyData.Decrypt(property, bytes);
        }

        internal byte[] Export()
        {
            if (Encoding.UTF8.GetBytes(Name).Length > short.MaxValue)
                throw new Exception("Name too long. Must be within " + (short.MaxValue / 2) + " characters.");

            var bytes = new List<byte>();
            var name = Encoding.UTF8.GetBytes(Name);
            bytes.AddRange(BitConverter.GetBytes((short)name.Length));
            bytes.AddRange(name);
            var key = KeyData.Export();
            bytes.AddRange(BitConverter.GetBytes(key.Length));
            bytes.AddRange(key);

            var arr = bytes.ToArray();
            bytes.Clear();
            return arr;
        }

        internal static KeyDescriptor Import(byte[] data, int position)
        {
            var nameLen = BitConverter.ToInt16(data, position);
            var name = Encoding.UTF8.GetString(data.Skip(position + 2).Take(nameLen).ToArray());
            var totalKeyLength = BitConverter.ToInt32(data, position + 2 + nameLen);
            var keyData = EncryptionKey.Import(data.Skip(position + 2 + nameLen + 4).Take(totalKeyLength).ToArray(), 0);
            return new KeyDescriptor(name, keyData);
        }
    }
}

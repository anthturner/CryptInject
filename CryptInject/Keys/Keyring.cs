using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.Core.Internal;

namespace CryptInject.Keys
{
    public sealed class Keyring : IEnumerable<KeyDescriptor>
    {
        internal HashSet<string> KeyNames { get; private set; }
        internal List<KeyDescriptor> Keys { get; private set; }

        public delegate void KeyringChangedDelegate();
        public event KeyringChangedDelegate KeyringChanged;

        public static Keyring GlobalKeyring { get; set; }

        internal bool ReadOnly { get; set; }

        static Keyring()
        {
            GlobalKeyring = new Keyring();
        }

        public Keyring()
        {
            Keys = new List<KeyDescriptor>();
            KeyNames = new HashSet<string>();
        }

        /// <summary>
        /// Adds a key to this keyring.
        /// </summary>
        /// <param name="name">Name of the key</param>
        /// <param name="key">Key encryption data</param>
        /// <param name="preLocked">If the key should be locked immediately</param>
        public void Add(string name, EncryptionKey key, bool preLocked = false)
        {
            if (ReadOnly)
                throw new Exception("Keyring is read-only.");
            if (KeyNames.Contains(name))
                throw new Exception("Key already exists. If replacing, use Remove() first.");
            var newKey = new KeyDescriptor(name, key, preLocked);
            newKey.KeyLockChanged += locked => { KeyringChanged?.Invoke(); };
            Keys.Add(newKey);
            KeyNames.Add(newKey.Name);

            KeyringChanged?.Invoke();
        }

        /// <summary>
        /// Removes a key from this keyring.
        /// </summary>
        /// <param name="name">Name of key to remove</param>
        public void Remove(string name)
        {
            if (ReadOnly)
                throw new Exception("Keyring is read-only.");
            if (KeyNames.Contains(name))
                throw new Exception("Key not found.");
            Keys.RemoveAll(k => k.Name == name);
            KeyNames.Remove(name);

            KeyringChanged?.Invoke();
        }

        /// <summary>
        /// Checks if the keyring has a definition for the given key name.
        /// </summary>
        /// <param name="name">Name of key to check</param>
        /// <returns></returns>
        public bool HasKey(string name)
        {
            return KeyNames.Contains(name);
        }

        /// <summary>
        /// Retrieve an enumerable of all key names provided by this keyring.
        /// </summary>
        public IEnumerable<string> KeysProvided
        {
            get { return KeyNames; }
        }

        /// <summary>
        /// Lock the keyring; all keys will be inaccessible.
        /// </summary>
        public void Lock()
        {
            Keys.AsParallel().ForEach(k => k.Locked = true);
        }

        /// <summary>
        /// Unlock the keyring; all keys will be accessible.
        /// </summary>
        public void Unlock()
        {
            Keys.AsParallel().ForEach(k => k.Locked = false);
        }

        /// <summary>
        /// Imports keys from a given keyring to this keyring.
        /// </summary>
        /// <param name="importableKeyring">External keyring to import from</param>
        public void Import(Keyring importableKeyring)
        {
            if (ReadOnly)
                throw new Exception("Keyring is read-only.");
            foreach (var key in importableKeyring.Keys)
            {
                if (HasKey(key.Name))
                    continue;
                Add(key.Name, key.KeyData, key.Locked);
            }

            KeyringChanged?.Invoke();
        }

        /// <summary>
        /// Imports a keyring from a given stream.
        /// </summary>
        /// <param name="stream">Stream to import from</param>
        public void ImportFromStream(Stream stream)
        {
            if (ReadOnly)
                throw new Exception("Keyring is read-only.");
            var countBuffer = new byte[2];
            stream.Read(countBuffer, 0, 2);
            var count = BitConverter.ToInt16(countBuffer, 0);

            for (int i = 0; i < count; i++)
            {
                var lenBuffer = new byte[4];
                stream.Read(lenBuffer, 0, 4);
                var keyLength = BitConverter.ToInt32(lenBuffer, 0);
                var keyBuffer = new byte[keyLength];
                stream.Read(keyBuffer, 0, keyLength);

                var newKey = KeyDescriptor.Import(keyBuffer, 0);
                if (Keys.Any(k => k.Name == newKey.Name))
                    continue;
                Add(newKey.Name, newKey.KeyData);
            }

            if (KeyringChanged != null) KeyringChanged();
        }

        /// <summary>
        /// Exports a keyring to a given stream.
        /// </summary>
        /// <param name="stream">Stream to export to</param>
        public void ExportToStream(Stream stream)
        {
            ExportToStream(stream, Keys.ToArray());
        }

        /// <summary>
        /// Exports a keyring to a given stream, limiting export to only the provided key names.
        /// </summary>
        /// <param name="stream">Stream to export to</param>
        /// <param name="keyNames">Key names to export</param>
        public void ExportToStream(Stream stream, params string[] keyNames)
        {
            ExportToStream(stream, Keys.Where(k => keyNames.Contains(k.Name)).ToArray());
        }

        /// <summary>
        /// Exports a keyring to a given stream, limiting export to only the provided key descriptors.
        /// </summary>
        /// <param name="stream">Stream to export to</param>
        /// <param name="keyDescriptors">Keys to export</param>
        public void ExportToStream(Stream stream, params KeyDescriptor[] keyDescriptors)
        {
            var bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes((short)keyDescriptors.Length));

            foreach (var key in keyDescriptors)
            {
                var exported = key.Export();
                bytes.AddRange(BitConverter.GetBytes(exported.Length));
                bytes.AddRange(exported);
            }

            stream.Write(bytes.ToArray(), 0, bytes.Count);
        }

        /// <summary>
        /// Removes all keys from this keyring.
        /// </summary>
        public void Clear()
        {
            if (ReadOnly)
                throw new Exception("Keyring is read-only.");
            Keys.Clear();
            KeyNames.Clear();
            if (KeyringChanged != null) KeyringChanged();
        }

        public KeyDescriptor this[string name]
        {
            get
            {
                if (!HasKey(name))
                    return null;
                return Keys.FirstOrDefault(k => k.Name == name);
            }
        }

        public IEnumerator<KeyDescriptor> GetEnumerator()
        {
            return Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Keys.GetEnumerator();
        }
    }
}

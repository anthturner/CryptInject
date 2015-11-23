using System;
using System.Management;
using System.Reflection;

namespace CryptInject.Keys.Programmatic
{
    public sealed class VirtualMachineProhibitedKey : EncryptionKey
    {
        private KeyAppliesTo AppliesTo { get; set; }

        /// <summary>
        /// Create/Load an encryption key based around AES-256. Key must be 48 bytes in length.
        /// </summary>
        /// <param name="chainedInnerKey">Key operation to run prior to this key</param>
        public VirtualMachineProhibitedKey(KeyAppliesTo appliesTo = KeyAppliesTo.Both,
            EncryptionKey chainedInnerKey = null) : base(new byte[0], chainedInnerKey)
        {
            AppliesTo = appliesTo;
        }

        public VirtualMachineProhibitedKey() : base(new byte[0], null)
        {
        }

        protected override byte[] Encrypt(PropertyInfo property, byte[] key, byte[] bytes)
        {
            if (AppliesTo.HasFlag(KeyAppliesTo.Encryption))
            {
                if (IsInVirtualMachine())
                    throw new UnauthorizedAccessException("Access not allowed within virtual machine");
            }
            return bytes;
        }

        protected override byte[] Decrypt(PropertyInfo property, byte[] key, byte[] bytes)
        {
            if (AppliesTo.HasFlag(KeyAppliesTo.Decryption))
            {
                if (IsInVirtualMachine())
                    throw new UnauthorizedAccessException("Access not allowed within virtual machine");
            }
            return bytes;
        }

        protected override byte[] ExportData
        {
            get
            {
                return new byte[] { (byte)AppliesTo };
            }
            set
            {
                AppliesTo = (KeyAppliesTo) value[0];
            }
        }

        protected override bool IsPeriodicallyAccessibleKey()
        {
            return false;
        }

        private bool IsInVirtualMachine()
        {
            using (var searcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
            {
                using (var items = searcher.Get())
                {
                    foreach (var item in items)
                    {
                        string manufacturer = item["Manufacturer"].ToString().ToLower();
                        if ((manufacturer == "microsoft corporation" &&
                             item["Model"].ToString().ToUpperInvariant().Contains("VIRTUAL"))
                            || manufacturer.Contains("vmware")
                            || item["Model"].ToString() == "VirtualBox")
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}


    

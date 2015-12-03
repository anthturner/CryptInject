using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CryptInject.Keys.Programmatic
{
    public sealed class AntiPrintScreenKey : EncryptionKey
    {
        private KeyAppliesTo AppliesTo { get; set; }

        /// <summary>
        /// Create/Load a programmatic key to force disabling of PrntScrn (via changing display affinity)
        /// </summary>
        /// <param name="chainedInnerKey">Key operation to run prior to this key</param>
        public AntiPrintScreenKey(KeyAppliesTo appliesTo = KeyAppliesTo.Both, EncryptionKey chainedInnerKey = null) : base(new byte[0], chainedInnerKey)
        {
            AppliesTo = appliesTo;
            ActiveWindows = new List<IntPtr>();
        }

        public AntiPrintScreenKey() : base(new byte[0], null)
        {
            ActiveWindows = new List<IntPtr>();
        }

        ~AntiPrintScreenKey()
        {
            ShowAllWindows();
        }

        /// <summary>
        /// Window pointers for all windows that will have their affinities changed to make print screen more difficult
        /// </summary>
        public List<IntPtr> ActiveWindows { get; }

        protected override byte[] Encrypt(PropertyInfo property, byte[] key, byte[] bytes)
        {
            if (AppliesTo.HasFlag(KeyAppliesTo.Encryption))
            {
                if (!HideAllWindows())
                    throw new UnauthorizedAccessException("Access not allowed within virtual machine");
            }
            return bytes;
        }

        protected override byte[] Decrypt(PropertyInfo property, byte[] key, byte[] bytes)
        {
            if (AppliesTo.HasFlag(KeyAppliesTo.Decryption))
            {
                if (!HideAllWindows())
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
                AppliesTo = (KeyAppliesTo)value[0];
            }
        }

        protected override bool IsPeriodicallyAccessibleKey()
        {
            return false;
        }

        private bool HideAllWindows()
        {
            if (Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 1)
            {
                foreach (var wnd in ActiveWindows)
                    SetWindowDisplayAffinity(wnd, 0);
                return true;
            }
            return false;
        }

        private bool ShowAllWindows()
        {
            if (Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 1)
            {
                foreach (var wnd in ActiveWindows)
                    SetWindowDisplayAffinity(wnd, 1);
                return true;
            }
            return false;
        }

        [DllImport("user32.dll")]
        static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);
    }
}

using System;
using System.Reflection;

namespace CryptInject.Keys.Programmatic
{
    public sealed class TimeWindowKey : EncryptionKey
    {
        private DateTime StartTime { get; set; }
        private DateTime EndTime { get; set; }
        private KeyAppliesTo AppliesTo { get; set; }

        /// <summary>
        /// Create/Load an encryption key based around AES-256. Key must be 48 bytes in length.
        /// </summary>
        /// <param name="key">Encryption Key (32B) + IV (16B)</param>
        /// <param name="chainedInnerKey">Key operation to run prior to this key</param>
        public TimeWindowKey(DateTime startTime, DateTime endTime, KeyAppliesTo appliesTo = KeyAppliesTo.Both, EncryptionKey chainedInnerKey = null) : base(new byte[0], chainedInnerKey)
        {
            StartTime = startTime;
            EndTime = endTime;
            AppliesTo = appliesTo;
        }

        public TimeWindowKey() : base(new byte[0], null)
        {
        }

        protected override byte[] Encrypt(PropertyInfo property, byte[] key, byte[] bytes)
        {
            if (AppliesTo.HasFlag(KeyAppliesTo.Encryption))
            {
                if (DateTime.Now >= StartTime && DateTime.Now <= EndTime)
                {
                    return bytes;
                }
                else
                {
                    throw new UnauthorizedAccessException("Access not within required time window");
                }
            }
            return bytes;
        }

        protected override byte[] Decrypt(PropertyInfo property, byte[] key, byte[] bytes)
        {
            if (AppliesTo.HasFlag(KeyAppliesTo.Decryption))
            {
                if (DateTime.Now >= StartTime && DateTime.Now <= EndTime)
                {
                    return bytes;
                }
                else
                {
                    throw new UnauthorizedAccessException("Access not within required time window");
                }
            }
            return bytes;
        }

        protected override byte[] ExportData
        {
            get
            {
                return CreateBinaryFrame(new byte[] { (byte)AppliesTo }, BitConverter.GetBytes(StartTime.Ticks), BitConverter.GetBytes(EndTime.Ticks));
            }
            set
            {
                var frame = ExtractBinaryFrame(value);
                AppliesTo = (KeyAppliesTo) frame[0][0];
                StartTime = new DateTime(BitConverter.ToInt64(frame[1], 0));
                EndTime = new DateTime(BitConverter.ToInt64(frame[2], 0));
            }
        }

        protected override bool IsPeriodicallyAccessibleKey()
        {
            return true;
        }
    }
}

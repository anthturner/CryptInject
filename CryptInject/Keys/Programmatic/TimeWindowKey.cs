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
        /// Create/Load a programmatic key based on a non-recurring time window of availability
        /// </summary>
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
                return CreateBinaryFrame(
                    new byte[] {(byte) AppliesTo},
                    BitConverter.GetBytes(GetUnixEpoch(StartTime)),
                    BitConverter.GetBytes(GetUnixEpoch(EndTime)));
            }
            set
            {
                var frame = ExtractBinaryFrame(value);
                AppliesTo = (KeyAppliesTo) frame[0][0];
                StartTime = GetDateTime(BitConverter.ToInt32(frame[1], 0));
                EndTime = GetDateTime(BitConverter.ToInt32(frame[2], 0));
            }
        }

        protected override bool IsPeriodicallyAccessibleKey()
        {
            return true;
        }

        private static int GetUnixEpoch(DateTime dateTime)
        {
            var unixTime = dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (int)unixTime.TotalSeconds;
        }

        private static DateTime GetDateTime(int ctime)
        {
            var dateTime = new DateTime(1970, 1, 1);
            return dateTime.AddSeconds(ctime);
        }
    }
}

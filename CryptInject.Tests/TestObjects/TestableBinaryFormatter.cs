using System;

namespace CryptInject.Tests.TestObjects
{
    [Serializable]
    [SerializerRedirect(typeof(SerializableAttribute))]
    public class TestableBinaryFormatter : ITestable
    {
        [Encryptable("AES")]
        public virtual string AesString { get; set; }

        [Encryptable("AES")]
        public virtual int AesInteger { get; set; }

        [Encryptable("AES")]
        public virtual Guid AesGuid { get; set; }

        [Encryptable("DES")]
        public virtual string DesString { get; set; }

        [Encryptable("DES")]
        public virtual int DesInteger { get; set; }

        [Encryptable("DES")]
        public virtual Guid DesGuid { get; set; }

        [Encryptable("AES-DES")]
        public virtual string AesDesString { get; set; }

        [Encryptable("AES-DES")]
        public virtual int AesDesInteger { get; set; }

        [Encryptable("AES-DES")]
        public virtual Guid AesDesGuid { get; set; }

        public string UnencryptedString { get; set; }

        public void Populate()
        {
            AesString = DesString = AesDesString = "This encrypted string exists identically for AES, DES, and AES-DES.";
            AesInteger = DesInteger = AesDesInteger = 42;
            AesGuid = DesGuid = AesDesGuid = new Guid(42, 61, 99, 4, 16, 11, 88, 50, 112, 209, 2);

            UnencryptedString = "This string is always unencrypted.";
        }
    }
}

using System;

namespace CryptInject.Tests.TestObjects
{
    [Serializable]
    [SerializerRedirect(typeof(SerializableAttribute))]
    public class TestableBinaryFormatter : ITestable
    {
        [Encryptable("AES")]
        public string AesString { get; set; }

        [Encryptable("AES")]
        public int AesInteger { get; set; }

        [Encryptable("AES")]
        public Guid AesGuid { get; set; }

        [Encryptable("DES")]
        public string DesString { get; set; }

        [Encryptable("DES")]
        public int DesInteger { get; set; }

        [Encryptable("DES")]
        public Guid DesGuid { get; set; }

        [Encryptable("AES-DES")]
        public string AesDesString { get; set; }

        [Encryptable("AES-DES")]
        public int AesDesInteger { get; set; }

        [Encryptable("AES-DES")]
        public Guid AesDesGuid { get; set; }

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

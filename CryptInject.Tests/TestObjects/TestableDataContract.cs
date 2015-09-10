using System;
using System.Runtime.Serialization;

namespace CryptInject.Tests.TestObjects
{
    [DataContract]
    public class TestableDataContract : ITestable
    {
        [Encryptable("AES")]
        [DataMember]
        public string AesString { get; set; }

        [Encryptable("AES")]
        [DataMember]
        public int AesInteger { get; set; }

        [Encryptable("AES")]
        [DataMember]
        public Guid AesGuid { get; set; }

        [Encryptable("DES")]
        [DataMember]
        public string DesString { get; set; }

        [Encryptable("DES")]
        [DataMember]
        public int DesInteger { get; set; }

        [Encryptable("DES")]
        [DataMember]
        public Guid DesGuid { get; set; }

        [Encryptable("AES-DES")]
        [DataMember]
        public string AesDesString { get; set; }

        [Encryptable("AES-DES")]
        [DataMember]
        public int AesDesInteger { get; set; }

        [Encryptable("AES-DES")]
        [DataMember]
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

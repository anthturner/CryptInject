using System;
using System.Runtime.Serialization;

namespace CryptInject.Tests.TestObjects
{
    [DataContract]
    public class TestableDataContract : ITestable
    {
        [Encryptable("AES")]
        [DataMember]
        public virtual string AesString { get; set; }

        [Encryptable("AES")]
        [DataMember]
        public virtual int AesInteger { get; set; }

        [Encryptable("AES")]
        [DataMember]
        public virtual Guid AesGuid { get; set; }

        [Encryptable("AES")]
        [DataMember]
        public virtual SubObject AesObject { get; set; }

        [Encryptable("DES")]
        [DataMember]
        public virtual string DesString { get; set; }

        [Encryptable("DES")]
        [DataMember]
        public virtual int DesInteger { get; set; }

        [Encryptable("DES")]
        [DataMember]
        public virtual Guid DesGuid { get; set; }

        [Encryptable("DES")]
        [DataMember]
        public virtual SubObject DesObject { get; set; }

        [Encryptable("AES-DES")]
        [DataMember]
        public virtual string AesDesString { get; set; }

        [Encryptable("AES-DES")]
        [DataMember]
        public virtual int AesDesInteger { get; set; }

        [Encryptable("AES-DES")]
        [DataMember]
        public virtual Guid AesDesGuid { get; set; }

        [Encryptable("AES-DES")]
        [DataMember]
        public virtual SubObject AesDesObject { get; set; }

        public string UnencryptedString { get; set; }

        public void Populate()
        {
            AesString = DesString = AesDesString = "This encrypted string exists identically for AES, DES, and AES-DES.";
            AesInteger = DesInteger = AesDesInteger = 42;
            AesGuid = DesGuid = AesDesGuid = new Guid(42, 61, 99, 4, 16, 11, 88, 50, 112, 209, 2);
            AesObject = DesObject = AesDesObject = new SubObject() {ChildInteger = 64};

            UnencryptedString = "This string is always unencrypted.";
        }
    }
}

using System;
using System.Runtime.Serialization;

namespace CryptInject.Tests.TestObjects
{
    [Serializable]
    [SerializerRedirect(typeof(SerializableAttribute))]
    public class FunctionallyCompleteTestable : ITestable
    {
        [Encryptable("AES")]
        public virtual string String { get; set; }

        [Encryptable("DES")]
        public virtual Guid Guid { get; set; }

        [SerializerRedirect(typeof(DataContractAttribute))]
        [Encryptable("AES-DES")]
        public virtual int Integer { get; set; }

        [Encryptable("AES")]
        public virtual SubObject SubObjectInstance { get; set; }

        public void Populate()
        {
            String = "I am a string!";
            Guid = new Guid(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            Integer = 42;
            SubObjectInstance = new SubObject()
            {
                ChildInteger = 64
            };
        }
    }

    [DataContract]
    [Serializable]
    public class SubObject
    {
        public int ChildInteger { get; set; }
    }
}

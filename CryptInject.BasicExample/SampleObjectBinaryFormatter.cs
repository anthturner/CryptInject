using System;
using System.Runtime.Serialization;

namespace CryptInject.BasicExample
{
    [Serializable]
    [SerializerRedirect(typeof(SerializableAttribute))]
    public class SampleObjectBinaryFormatter : IDeserializationCallback
    {
        [Encryptable("Sensitive Information")]
        public virtual int Integer { get; set; }

        [Encryptable("Non-Sensitive Information")]
        public virtual string String { get; set; }
        
        public void OnDeserialization(object sender)
        {
            this.Relink();
        }
    }
}

using System.Runtime.Serialization;

namespace CryptInject.BasicExample
{
    [DataContract]
    [SerializerRedirect(typeof(DataContractAttribute))]
    public class DataObjectInstanceContract
    {
        [SerializerRedirect(typeof(DataMemberAttribute))]
        [Encryptable("Sensitive Information")]
        public virtual int Integer { get; set; }

        [SerializerRedirect(typeof(DataMemberAttribute))]
        [Encryptable("Non-Sensitive Information")]
        public virtual string String { get; set; }

        [OnDeserializing]
        void OnDeserializing(StreamingContext c)
        {
            this.Relink();
        }
    }
}

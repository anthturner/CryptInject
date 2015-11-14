using System.Runtime.Serialization;

namespace CryptInject.BasicExample
{
    [DataContract]
    //[KnownType(typeof(InnerObjectContract))]
    [SerializerRedirect(typeof(DataContractAttribute))]
    public class DataObjectInstanceContract
    {
        //[DataMember]
        //public InnerObjectContract Member { get; set; }

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

    //[DataContract]
    //[SerializerRedirect(typeof(DataContractAttribute))]
    //public class InnerObjectContract
    //{
    //    [SerializerRedirect(typeof(DataMemberAttribute))]
    //    [Encryptable("Semi-Sensitive Information")]
    //    public virtual string HelloStr { get; set; }
    //}
}

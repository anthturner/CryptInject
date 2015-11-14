using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace CryptInject.BasicExample
{
    [Serializable]
    [JsonObject]
    [SerializerRedirect(typeof(SerializableAttribute))]
    public class DataObjectInstance : IDeserializationCallback
    {
        [JsonProperty]
        public InnerObject Member { get; set; }

        [SerializerRedirect(typeof(JsonPropertyAttribute))]
        [Encryptable("Sensitive Information")]
        [JsonIgnore]
        public virtual int Integer { get; set; }

        [SerializerRedirect(typeof(JsonPropertyAttribute))]
        [Encryptable("Non-Sensitive Information")]
        [JsonIgnore]
        public virtual string String { get; set; }

        public DataObjectInstance()
        {
            this.Relink();
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext c)
        {
            this.Relink();
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext c)
        {
            this.Relink();
        }

        public void OnDeserialization(object sender)
        {
            this.Relink();
        }
    }

    [Serializable]
    [JsonObject]
    [SerializerRedirect(typeof(SerializableAttribute))]
    public class InnerObject
    {
        [SerializerRedirect(typeof(JsonPropertyAttribute))]
        [Encryptable("Semi-Sensitive Information")]
        [JsonIgnore]
        public virtual string HelloStr { get; set; }

        public InnerObject()
        {
            this.Relink();
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext c)
        {
            this.Relink();
        }
    }
}

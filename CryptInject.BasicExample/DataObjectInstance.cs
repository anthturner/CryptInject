using System;
using Newtonsoft.Json;

namespace CryptInject.BasicExample
{
    [Serializable]
    [JsonObject]
    [SerializerRedirect(typeof(SerializableAttribute))]
    public class DataObjectInstance
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
    }
}

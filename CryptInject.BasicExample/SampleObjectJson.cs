using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace CryptInject.BasicExample
{
    [JsonObject]
    public class SampleObjectJson
    {
        [Encryptable("Sensitive Information")]
        [SerializerRedirect(typeof(JsonPropertyAttribute))]
        [JsonIgnore]
        public virtual int Integer { get; set; }

        [Encryptable("Non-Sensitive Information")]
        [SerializerRedirect(typeof(JsonPropertyAttribute))]
        [JsonIgnore]
        public virtual string String { get; set; }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext c)
        {
            this.Relink();
        }

        public SampleObjectJson()
        {
            this.Relink();
        }
    }
}

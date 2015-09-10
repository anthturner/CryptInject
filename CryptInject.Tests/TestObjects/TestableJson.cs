using System;
using Newtonsoft.Json;

namespace CryptInject.Tests.TestObjects
{
    [JsonObject]
    public class TestableJson : ITestable
    {
        [Encryptable("AES")]
        [JsonProperty]
        public string AesString { get; set; }

        [Encryptable("AES")]
        [JsonProperty]
        public int AesInteger { get; set; }

        [Encryptable("AES")]
        [JsonProperty]
        public Guid AesGuid { get; set; }

        [Encryptable("DES")]
        [JsonProperty]
        public string DesString { get; set; }

        [Encryptable("DES")]
        [JsonProperty]
        public int DesInteger { get; set; }

        [Encryptable("DES")]
        [JsonProperty]
        public Guid DesGuid { get; set; }

        [Encryptable("AES-DES")]
        [JsonProperty]
        public string AesDesString { get; set; }

        [Encryptable("AES-DES")]
        [JsonProperty]
        public int AesDesInteger { get; set; }

        [Encryptable("AES-DES")]
        [JsonProperty]
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

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using CryptInject.Keys;
using CryptInject.Keys.Builtin;
using Newtonsoft.Json;

namespace CryptInject.BasicExample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create 3 new keys with randomly generated key material
            Keyring.GlobalKeyring.Add("Sensitive Information", AesEncryptionKey.Create(TripleDesEncryptionKey.Create()));
            Keyring.GlobalKeyring.Add("Semi-Sensitive Information", TripleDesEncryptionKey.Create());
            Keyring.GlobalKeyring.Add("Non-Sensitive Information", TripleDesEncryptionKey.Create());
            

            // Create new instances of types to encrypt and fill them with something
            var encryptedObjBinaryFormatter = new SampleObjectBinaryFormatter().AsEncrypted();
            var encryptedObjDataContract = new SampleObjectDataContract().AsEncrypted();
            var encryptedObjJson = new SampleObjectJson().AsEncrypted();

            encryptedObjBinaryFormatter.Integer = encryptedObjDataContract.Integer = encryptedObjJson.Integer = 12;
            encryptedObjBinaryFormatter.String = encryptedObjDataContract.String = encryptedObjJson.String = "abc";

            Console.WriteLine("Before serialize: " + encryptedObjBinaryFormatter.Integer + ", '" + encryptedObjBinaryFormatter.String + "'");


            // Serialize using each of the 3 mainstream serializers
            var bfProxy = TestBinaryFormatter(encryptedObjBinaryFormatter);
            var dcProxy = TestDataContractSerializer(encryptedObjDataContract);
            var jsonProxy = TestJsonSerializer(encryptedObjJson);
            
            Console.WriteLine("After deserialize (BinaryFormatter): " + bfProxy.Integer + ", '" + bfProxy.String + "'");
            Console.WriteLine("After deserialize (DataContract): " + dcProxy.Integer + ", '" + dcProxy.String + "'");
            Console.WriteLine("After deserialize (JSON): " + jsonProxy.Integer + ", '" + jsonProxy.String + "'");

            Console.ReadLine();
        }

        private static T TestBinaryFormatter<T>(T obj)
        {
            var memoryStream = new MemoryStream();
            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(memoryStream, obj);

            memoryStream.Seek(0, SeekOrigin.Begin); // rewind

            binaryFormatter = new BinaryFormatter();
            return (T)binaryFormatter.Deserialize(memoryStream);
        }

        private static T TestDataContractSerializer<T>(T obj) where T : class
        {
            var memoryStream = new MemoryStream();
            var dc = new DataContractSerializer(obj.GetType(), obj.GetKnownTypes());
            dc.WriteObject(memoryStream, obj);

            memoryStream.Seek(0, SeekOrigin.Begin); // rewind

            Console.WriteLine();
            Console.WriteLine("Data Contract: " + System.Text.Encoding.ASCII.GetString(memoryStream.ToArray()));
            Console.WriteLine();

            memoryStream.Seek(0, SeekOrigin.Begin); // rewind

            return (T)dc.ReadObject(memoryStream);
        }

        private static T TestJsonSerializer<T>(T obj)
        {
            var jsonStr = JsonConvert.SerializeObject(obj, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
            Console.WriteLine();
            Console.WriteLine("JSON: " + jsonStr);
            Console.WriteLine();
            return (T)JsonConvert.DeserializeObject(jsonStr, typeof(T).GetEncryptedType(), new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
        }
    }
}
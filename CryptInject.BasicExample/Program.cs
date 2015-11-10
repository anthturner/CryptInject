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
        private static void a()
        {
            var kr = new Keyring();
        }

        static void Main(string[] args)
        {
            // Create 3 new keys with randomly generated key material
            var keyring = new Keyring();
            keyring.Add("Sensitive Information", AesEncryptionKey.Create(TripleDesEncryptionKey.Create()));
            keyring.Add("Semi-Sensitive Information", TripleDesEncryptionKey.Create());
            keyring.Add("Non-Sensitive Information", TripleDesEncryptionKey.Create());

            using (var keyringExportStream = new FileStream("keyring.dat", FileMode.Create))
            {
                // Export only 2 of the 3 keys we created, for example...
                keyring.ExportToStream(keyringExportStream, "Non-Sensitive Information", "Semi-Sensitive Information");
            }

            using (var keyringImportStream = new FileStream("keyring.dat", FileMode.Open))
            {
                keyring.ImportFromStream(keyringImportStream);
            }

            // Create a new instance of the object you're serializing (class for this is below)
            var proxy = new DataObjectInstance().AsEncrypted(keyring);
            var proxyDc = new DataObjectInstanceContract().AsEncrypted(keyring);
            proxy.String = "abc";
            proxy.Member = new InnerObject().AsEncrypted(keyring);
            proxy.Member.HelloStr = "Hello, world!";

            // Do the same for the DataContract-annotated type
            proxyDc.Integer = 12;
            proxyDc.String = "abc";

            Console.WriteLine("Before lock: " + proxy.Integer + ", '" + proxy.String + "', '" + proxy.Member.HelloStr + "'");

            // Lock out access to all keys (and clear cached values)
            keyring.Lock();

            var bfProxy = TestBinaryFormatter(proxy);
            var dcProxy = TestDataContractSerializer(proxyDc);
            var jsonProxy = TestJsonSerializer(proxy);

            Console.WriteLine("Before unlock: " + proxy.Integer + ", '" + proxy.String + "', '" + proxy.Member.HelloStr + "'");
            keyring.Unlock();
            Console.WriteLine("After unlock (BinaryFormatter): " + bfProxy.Integer + ", '" + bfProxy.String + "', '" + bfProxy.Member.HelloStr + "'");
            Console.WriteLine("After unlock (DataContract): " + dcProxy.Integer + ", '" + dcProxy.String + "'");
            Console.WriteLine("After unlock (JSON): " + jsonProxy.Integer + ", '" + jsonProxy.String + "'");
        }

        private static T TestBinaryFormatter<T>(T obj)
        {
            var memoryStream = new MemoryStream();
            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(memoryStream, obj);

            memoryStream.Seek(0, SeekOrigin.Begin); // rewind

            return (T)binaryFormatter.Deserialize(memoryStream);
        }

        private static T TestDataContractSerializer<T>(T obj) where T : class
        {
            var memoryStream = new MemoryStream();
            var dc = new DataContractSerializer(obj.GetType(), obj.GetKnownTypes());
            dc.WriteObject(memoryStream, obj);

            memoryStream.Seek(0, SeekOrigin.Begin); // rewind

            return (T)dc.ReadObject(memoryStream);
        }

        private static T TestJsonSerializer<T>(T obj)
        {
            var jsonStr = JsonConvert.SerializeObject(obj, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
            return (T)JsonConvert.DeserializeObject(jsonStr, typeof(DataObjectInstance).GetEncryptedType(), new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
        }
    }
}
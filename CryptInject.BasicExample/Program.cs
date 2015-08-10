using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using Castle.DynamicProxy;
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
                EncryptionManager.Keyring.ImportFromStream(keyringImportStream);
            }

            EncryptionManager.PreloadProxyTypes();

            // Create a new instance of the object you're serializing (class for this is below)
            var proxy = EncryptionManager.Create<DataObjectInstance>();
            var proxyDc = EncryptionManager.Create<DataObjectInstanceContract>();
            proxy.Integer = 12; // this won't work because we only exported 2 of the 3 keys and this is annotated with the third key :)
            proxy.String = "abc";
            proxy.Member = EncryptionManager.Create<InnerObject>();
            proxy.Member.HelloStr = "Hello, world!";

            // Do the same for the DataContract-annotated type
            proxyDc.Integer = 12;
            proxyDc.String = "abc";

            Console.WriteLine("Before lock: " + proxy.Integer + ", '" + proxy.String + "', '" + proxy.Member.HelloStr + "'");

            // Lock out access to all keys (and clear cached values)
            EncryptionManager.Keyring.Lock();

            var bfProxy = TestBinaryFormatter(proxy);
            var dcProxy = TestDataContractSerializer(proxyDc);
            var jsonProxy = TestJsonSerializer(proxy);

            Console.WriteLine("Before unlock: " + proxy.Integer + ", '" + proxy.String + "', '" + proxy.Member.HelloStr + "'");
            EncryptionManager.Keyring.Unlock();
            Console.WriteLine("After unlock (BinaryFormatter): " + bfProxy.Integer + ", '" + bfProxy.String + "', '" + bfProxy.Member.HelloStr + "'");
            Console.WriteLine("After unlock (DataContract): " + dcProxy.Integer + ", '" + dcProxy.String + "'");
            Console.WriteLine("After unlock (JSON): " + jsonProxy.Integer + ", '" + jsonProxy.String + "'");
        }

        private static T TestBinaryFormatter<T>(T obj)
        {
            var memoryStream = new MemoryStream();
            var binaryFormatter = new BinaryFormatter() { Binder = new EncryptionProxySerializationBinder() };
            binaryFormatter.Serialize(memoryStream, obj);

            memoryStream.Seek(0, SeekOrigin.Begin); // rewind

            return (T)binaryFormatter.Deserialize(memoryStream);
        }

        private static T TestDataContractSerializer<T>(T obj)
        {
            var memoryStream = new MemoryStream();
            var dc = new DataContractSerializer(obj.GetType(), EncryptionManager.GetKnownTypes(obj));
            dc.WriteObject(memoryStream, obj);

            memoryStream.Seek(0, SeekOrigin.Begin); // rewind

            return (T)dc.ReadObject(memoryStream);
        }

        private static T TestJsonSerializer<T>(T obj)
        {
            var jsonStr = JsonConvert.SerializeObject(obj, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
            return (T)JsonConvert.DeserializeObject(jsonStr, EncryptionManager.GetProxyType(typeof(DataObjectInstance)), new JsonSerializerSettings() { Binder = new EncryptionProxySerializationBinder(), TypeNameHandling = TypeNameHandling.Auto });
        }
    }
}
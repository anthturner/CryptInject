using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using CryptInject.Keys;
using CryptInject.Tests.TestObjects;
using ProtoBuf;

namespace CryptInject.Tests
{
    public class ProfiledTestRun<TObject, TSerialized> where TObject : class, ITestable
    {
        private Func<TObject, TSerialized> ProfiledSerializationFunction { get; set; }
        private Func<TSerialized, TObject> ProfiledDeserializationFunction { get; set; }

        private static Keyring GeneratedKeyring { get; set; }

        public ProfiledTestRun(Func<TObject, TSerialized> profiledSerializationFunction, Func<TSerialized, TObject> profiledDeserializationFunction, Keyring keyring)
        {
            ProfiledSerializationFunction = profiledSerializationFunction;
            ProfiledDeserializationFunction = profiledDeserializationFunction;

            GeneratedKeyring = keyring;
        }

        public List<TimeSpan> ProfileSerializationWorkflow(bool useEncryption, int runs = 1)
        {
            var times = new List<TimeSpan>();
            var sw = new Stopwatch();
            var testObject = GenerateSampleObject(useEncryption);
            GeneratedKeyring.Lock();
            for (int i = 0; i < runs; i++)
            {
                sw.Restart();
                ProfiledSerializationFunction.Invoke(testObject);
                sw.Stop();
                times.Add(sw.Elapsed);
            }
            GeneratedKeyring.Unlock();
            return times;
        }

        public List<TimeSpan> ProfileDeserializationWorkflow(bool useEncryption, int runs = 1)
        {
            var times = new List<TimeSpan>();
            var sw = new Stopwatch();
            var testData = GenerateSampleSerializedData(useEncryption);
            GeneratedKeyring.Lock();
            for (int i = 0; i < runs; i++)
            {
                sw.Restart();
                ProfiledDeserializationFunction.Invoke(testData);
                sw.Stop();
                times.Add(sw.Elapsed);
            }
            GeneratedKeyring.Unlock();
            return times;
        }
        
        public TSerialized GenerateSampleSerializedData(bool useEncryption)
        {
            var generatedObject = GenerateSampleObject(useEncryption);
            GeneratedKeyring.Lock();
            var serializedData = ProfiledSerializationFunction.Invoke(generatedObject);
            GeneratedKeyring.Unlock();
            return serializedData;
        }

        public TObject GenerateSampleObject(bool useEncryption)
        {
            var options = new EncryptionProxyConfiguration((property, serializableObject) =>
            {
                var memoryStream = new MemoryStream();
                Serializer.Serialize(memoryStream, serializableObject);
                return memoryStream.ToArray();
            }, (property, data) =>
            {
                var genericInvoke = typeof(Serializer).GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(property.PropertyType);
                return genericInvoke.Invoke(null, new object[] { new MemoryStream(data) });
            });
            var instance = Activator.CreateInstance<TObject>();
            instance.Populate();
            if (useEncryption)
            {
                return instance.AsEncrypted(configuration: options);
            }
            return instance;
        }
    }
}

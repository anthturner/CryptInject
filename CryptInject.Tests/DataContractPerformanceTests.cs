using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using CryptInject.Keys;
using CryptInject.Keys.Builtin;
using CryptInject.Tests.TestObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CryptInject.Tests
{
    [TestClass]
    public class DataContractPerformanceTests
    {
        private ProfiledTestRun<TestableDataContract, Stream> ProfiledSerializerStrategy = new ProfiledTestRun<TestableDataContract, Stream>(
            testObj =>
            {
                var memoryStream = new MemoryStream();
                var dc = new DataContractSerializer(testObj.GetType(), EncryptionManager.GetKnownTypes(testObj));
                dc.WriteObject(memoryStream, testObj);

                return memoryStream;
            },
            testStream =>
            {
                testStream.Seek(0, SeekOrigin.Begin); // rewind

                var dc = new DataContractSerializer(EncryptionManager.GetProxyType(typeof(TestableDataContract)));
                return (TestableDataContract)dc.ReadObject(testStream);
            }
            );

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var keyring = new Keyring();
            keyring.Add("AES", AesEncryptionKey.Create());
            keyring.Add("DES", TripleDesEncryptionKey.Create());
            keyring.Add("AES-DES", AesEncryptionKey.Create());
            EncryptionManager.Keyring = keyring;
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void DataContract_Serialize()
        {
            Trace.WriteLine(ProfiledSerializerStrategy.ProfileSerializationWorkflow(true).First());
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void DataContract_Serialize_10000_Average()
        {
            Trace.WriteLine(TimeSpan.FromMilliseconds(ProfiledSerializerStrategy.ProfileSerializationWorkflow(true, 10000).Average(t => t.TotalMilliseconds)));
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void DataContract_Serialize_10000_Total()
        {
            Trace.WriteLine(TimeSpan.FromMilliseconds(ProfiledSerializerStrategy.ProfileSerializationWorkflow(true, 10000).Sum(t => t.TotalMilliseconds)));
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void DataContract_SerializeBaseline()
        {
            Trace.WriteLine(ProfiledSerializerStrategy.ProfileSerializationWorkflow(false).First());
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void DataContract_SerializeBaseline_10000_Average()
        {
            Trace.WriteLine(TimeSpan.FromMilliseconds(ProfiledSerializerStrategy.ProfileSerializationWorkflow(false, 10000).Average(t => t.TotalMilliseconds)));
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void DataContract_SerializeBaseline_10000_Total()
        {
            Trace.WriteLine(TimeSpan.FromMilliseconds(ProfiledSerializerStrategy.ProfileSerializationWorkflow(false, 10000).Sum(t => t.TotalMilliseconds)));
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void DataContract_Deserialize()
        {
            Trace.WriteLine(ProfiledSerializerStrategy.ProfileDeserializationWorkflow(true).First());
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void DataContract_Deserialize_10000_Average()
        {
            Trace.WriteLine(TimeSpan.FromMilliseconds(ProfiledSerializerStrategy.ProfileDeserializationWorkflow(true, 10000).Average(t => t.TotalMilliseconds)));
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void DataContract_Deserialize_10000_Total()
        {
            Trace.WriteLine(TimeSpan.FromMilliseconds(ProfiledSerializerStrategy.ProfileDeserializationWorkflow(true, 10000).Sum(t => t.TotalMilliseconds)));
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void DataContract_DeserializeBaseline()
        {
            Trace.WriteLine(ProfiledSerializerStrategy.ProfileSerializationWorkflow(false).First());
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void DataContract_DeserializeBaseline_10000_Average()
        {
            Trace.WriteLine(TimeSpan.FromMilliseconds(ProfiledSerializerStrategy.ProfileDeserializationWorkflow(false, 10000).Average(t => t.TotalMilliseconds)));
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void DataContract_DeserializeBaseline_10000_Total()
        {
            Trace.WriteLine(TimeSpan.FromMilliseconds(ProfiledSerializerStrategy.ProfileDeserializationWorkflow(false, 10000).Sum(t => t.TotalMilliseconds)));
        }
    }
}

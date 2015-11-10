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
                var dc = new DataContractSerializer(testObj.GetType(), testObj.GetKnownTypes());
                dc.WriteObject(memoryStream, testObj);

                return memoryStream;
            },
            testStream =>
            {
                testStream.Seek(0, SeekOrigin.Begin); // rewind

                var dc = new DataContractSerializer(typeof(TestableDataContract).GetEncryptedType());
                return (TestableDataContract)dc.ReadObject(testStream);
            },
            GeneratedKeyring
            );

        private static Keyring GeneratedKeyring = new Keyring();

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            GeneratedKeyring.Add("AES", AesEncryptionKey.Create());
            GeneratedKeyring.Add("DES", TripleDesEncryptionKey.Create());
            GeneratedKeyring.Add("AES-DES", AesEncryptionKey.Create());
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void DataContract_Serialize()
        {
            Trace.WriteLine(ProfiledSerializerStrategy.ProfileSerializationWorkflow(true).First());
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void DataContract_Serialize_10000()
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
        public void DataContract_SerializeBaseline_10000()
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
        public void DataContract_Deserialize_10000()
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
        public void DataContract_DeserializeBaseline_10000()
        {
            Trace.WriteLine(TimeSpan.FromMilliseconds(ProfiledSerializerStrategy.ProfileDeserializationWorkflow(false, 10000).Sum(t => t.TotalMilliseconds)));
        }
    }
}

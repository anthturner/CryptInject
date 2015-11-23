using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using CryptInject.Keys;
using CryptInject.Keys.Builtin;
using CryptInject.Tests.TestObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CryptInject.Tests
{
    [TestClass]
    public class BinaryFormatterPerformanceTests
    {
        private ProfiledTestRun<TestableBinaryFormatter, Stream> ProfiledSerializerStrategy = new ProfiledTestRun<TestableBinaryFormatter, Stream>(
            testObj =>
            {
                var memoryStream = new MemoryStream();
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, testObj);

                Assert.IsTrue(memoryStream.Length > 0);

                return memoryStream;
            },
            testStream =>
            {
                testStream.Seek(0, SeekOrigin.Begin); // rewind

                var binaryFormatter = new BinaryFormatter();
                return (TestableBinaryFormatter)binaryFormatter.Deserialize(testStream);
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

            // Warmup
            var types = DataWrapperExtensions.GetAllEncryptableTypes(true);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CryptInject.Proxy.EncryptedInstanceFactory.InvalidateInstancesTypes();
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void BinaryFormatter_Serialize()
        {
            Trace.WriteLine(ProfiledSerializerStrategy.ProfileSerializationWorkflow(true).First());
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void BinaryFormatter_Serialize_10000()
        {
            Trace.WriteLine(TimeSpan.FromMilliseconds(ProfiledSerializerStrategy.ProfileSerializationWorkflow(true, 10000).Sum(t => t.TotalMilliseconds)));
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void BinaryFormatter_SerializeBaseline()
        {
            Trace.WriteLine(ProfiledSerializerStrategy.ProfileSerializationWorkflow(false).First());
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void BinaryFormatter_SerializeBaseline_10000()
        {
            Trace.WriteLine(TimeSpan.FromMilliseconds(ProfiledSerializerStrategy.ProfileSerializationWorkflow(false, 10000).Sum(t => t.TotalMilliseconds)));
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void BinaryFormatter_Deserialize()
        {
            Trace.WriteLine(ProfiledSerializerStrategy.ProfileDeserializationWorkflow(true).First());
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void BinaryFormatter_Deserialize_10000()
        {
            Trace.WriteLine(TimeSpan.FromMilliseconds(ProfiledSerializerStrategy.ProfileDeserializationWorkflow(true, 10000).Sum(t => t.TotalMilliseconds)));
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void BinaryFormatter_DeserializeBaseline()
        {
            Trace.WriteLine(ProfiledSerializerStrategy.ProfileSerializationWorkflow(false).First());
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void BinaryFormatter_DeserializeBaseline_10000()
        {
            Trace.WriteLine(TimeSpan.FromMilliseconds(ProfiledSerializerStrategy.ProfileDeserializationWorkflow(false, 10000).Sum(t => t.TotalMilliseconds)));
        }
    }
}

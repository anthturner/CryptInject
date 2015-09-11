using System;
using System.Diagnostics;
using System.Linq;
using CryptInject.Keys;
using CryptInject.Keys.Builtin;
using CryptInject.Tests.TestObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace CryptInject.Tests
{
    [TestClass]
    public class JsonPerformanceTests
    {
        private ProfiledTestRun<TestableJson, string> ProfiledSerializerStrategy = new ProfiledTestRun<TestableJson, string>(
            testObj =>
            {
                return JsonConvert.SerializeObject(testObj, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
            },
            testString =>
            {
                return (TestableJson)JsonConvert.DeserializeObject(testString, EncryptionManager.GetProxyType(typeof(TestableJson)), new JsonSerializerSettings() { Binder = new EncryptionProxySerializationBinder(), TypeNameHandling = TypeNameHandling.Auto });
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
        public void Json_Serialize()
        {
            Trace.WriteLine(ProfiledSerializerStrategy.ProfileSerializationWorkflow(true).First());
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void Json_Serialize_10000()
        {
            Trace.WriteLine(TimeSpan.FromMilliseconds(ProfiledSerializerStrategy.ProfileSerializationWorkflow(true, 10000).Sum(t => t.TotalMilliseconds)));
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void Json_SerializeBaseline()
        {
            Trace.WriteLine(ProfiledSerializerStrategy.ProfileSerializationWorkflow(false).First());
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void Json_SerializeBaseline_10000()
        {
            Trace.WriteLine(TimeSpan.FromMilliseconds(ProfiledSerializerStrategy.ProfileSerializationWorkflow(false, 10000).Sum(t => t.TotalMilliseconds)));
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void Json_Deserialize()
        {
            Trace.WriteLine(ProfiledSerializerStrategy.ProfileDeserializationWorkflow(true).First());
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void Json_Deserialize_10000()
        {
            Trace.WriteLine(TimeSpan.FromMilliseconds(ProfiledSerializerStrategy.ProfileDeserializationWorkflow(true, 10000).Sum(t => t.TotalMilliseconds)));
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void Json_DeserializeBaseline()
        {
            Trace.WriteLine(ProfiledSerializerStrategy.ProfileSerializationWorkflow(false).First());
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void Json_DeserializeBaseline_10000()
        {
            Trace.WriteLine(TimeSpan.FromMilliseconds(ProfiledSerializerStrategy.ProfileDeserializationWorkflow(false, 10000).Sum(t => t.TotalMilliseconds)));
        }
    }
}

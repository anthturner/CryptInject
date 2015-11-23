using System;
using System.Diagnostics;
using CryptInject.Keys;
using CryptInject.Keys.Builtin;
using CryptInject.Tests.TestObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CryptInject.Tests
{
    [TestClass]
    public class InMemoryPerformanceTests
    {
        private static TestableDataContract BaseTestObject { get; set; }
        private static TestableDataContract GeneratedTestObject { get; set; }
        
        [TestInitialize]
        public void TestInitialize()
        {
            var generatedKeyring = new Keyring();
            generatedKeyring.Add("AES", AesEncryptionKey.Create());
            generatedKeyring.Add("DES", TripleDesEncryptionKey.Create());
            generatedKeyring.Add("AES-DES", AesEncryptionKey.Create(TripleDesEncryptionKey.Create()));

            BaseTestObject = new TestableDataContract();
            BaseTestObject.Populate();

            GeneratedTestObject = new TestableDataContract().AsEncrypted(generatedKeyring);
            GeneratedTestObject.Populate();
        }

        [TestMethod]
        [TestCategory("Access Performance")]
        public void InMemory_AesAccessString()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.AesString != default(string)); });
        }

        [TestMethod]
        [TestCategory("Access Performance")]
        public void InMemory_AesAccessInteger()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.AesInteger != default(int)); });
        }

        [TestMethod]
        [TestCategory("Access Performance")]
        public void InMemory_AesAccessGuid()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.AesGuid != default(Guid)); });
        }

        [TestMethod]
        [TestCategory("Access Performance")]
        public void InMemory_AesAccessObject()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.AesObject != default(SubObject)); });
        }

        [TestMethod]
        [TestCategory("Access Performance")]
        public void InMemory_DesAccessString()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.DesString != default(string)); });
        }

        [TestMethod]
        [TestCategory("Access Performance")]
        public void InMemory_DesAccessInteger()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.DesInteger != default(int)); });
        }

        [TestMethod]
        [TestCategory("Access Performance")]
        public void InMemory_DesAccessGuid()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.DesGuid != default(Guid)); });
        }

        [TestMethod]
        [TestCategory("Access Performance")]
        public void InMemory_DesAccessObject()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.DesObject != default(SubObject)); });
        }

        [TestMethod]
        [TestCategory("Access Performance")]
        public void InMemory_AesDesAccessString()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.AesDesString != default(string)); });
        }

        [TestMethod]
        [TestCategory("Access Performance")]
        public void InMemory_AesDesAccessInteger()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.AesDesInteger != default(int)); });
        }

        [TestMethod]
        [TestCategory("Access Performance")]
        public void InMemory_AesDesAccessGuid()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.AesDesGuid != default(Guid)); });
        }

        [TestMethod]
        [TestCategory("Access Performance")]
        public void InMemory_AesDesAccessObject()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.AesDesObject != default(SubObject)); });
        }

        [TestMethod]
        [TestCategory("Access Performance")]
        public void InMemory_BaselineAccessString()
        {
            Benchmark(() => { Assert.IsTrue(BaseTestObject.AesDesString != default(string)); });
        }

        [TestMethod]
        [TestCategory("Access Performance")]
        public void InMemory_BaselineAccessInteger()
        {
            Benchmark(() => { Assert.IsTrue(BaseTestObject.AesDesInteger != default(int)); });
        }

        [TestMethod]
        [TestCategory("Access Performance")]
        public void InMemory_BaselineAccessGuid()
        {
            Benchmark(() => { Assert.IsTrue(BaseTestObject.AesDesGuid != default(Guid)); });
        }

        [TestMethod]
        [TestCategory("Access Performance")]
        public void InMemory_BaselineAccessUnencryptedString()
        {
            Benchmark(() => { Assert.IsTrue(BaseTestObject.UnencryptedString != default(string)); });
        }

        [TestMethod]
        [TestCategory("Access Performance")]
        public void InMemory_BaselineAccessObject()
        {
            Benchmark(() => { Assert.IsTrue(BaseTestObject.AesDesObject != default(SubObject)); });
        }

        private void Benchmark(Action a)
        {
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                a.Invoke();
            }
            sw.Stop();
            Trace.WriteLine(sw.Elapsed);
        }
    }
}

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

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var keyring = new Keyring();
            keyring.Add("AES", AesEncryptionKey.Create());
            keyring.Add("DES", TripleDesEncryptionKey.Create());
            keyring.Add("AES-DES", AesEncryptionKey.Create());
            EncryptionManager.Keyring = keyring;

            BaseTestObject = new TestableDataContract();
            BaseTestObject.Populate();

            GeneratedTestObject = EncryptionManager.Create<TestableDataContract>();
            GeneratedTestObject.Populate();
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void InMemory_AesAccessString()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.AesString != default(string)); });
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void InMemory_AesAccessInteger()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.AesInteger != default(int)); });
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void InMemory_AesAccessGuid()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.AesGuid != default(Guid)); });
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void InMemory_DesAccessString()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.DesString != default(string)); });
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void InMemory_DesAccessInteger()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.DesInteger != default(int)); });
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void InMemory_DesAccessGuid()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.DesGuid != default(Guid)); });
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void InMemory_AesDesAccessString()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.AesDesString != default(string)); });
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void InMemory_AesDesAccessInteger()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.AesDesInteger != default(int)); });
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void InMemory_AesDesAccessGuid()
        {
            Benchmark(() => { Assert.IsTrue(GeneratedTestObject.AesDesGuid != default(Guid)); });
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void InMemory_BaselineAccessString()
        {
            Benchmark(() => { Assert.IsTrue(BaseTestObject.AesDesString != default(string)); });
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void InMemory_BaselineAccessInteger()
        {
            Benchmark(() => { Assert.IsTrue(BaseTestObject.AesDesInteger != default(int)); });
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void InMemory_BaselineAccessGuid()
        {
            Benchmark(() => { Assert.IsTrue(BaseTestObject.AesDesGuid != default(Guid)); });
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void InMemory_BaselineAccessUnencryptedString()
        {
            Benchmark(() => { Assert.IsTrue(BaseTestObject.UnencryptedString != default(string)); });
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

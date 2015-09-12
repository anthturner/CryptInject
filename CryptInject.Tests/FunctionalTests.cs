using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CryptInject.Keys;
using CryptInject.Keys.Builtin;
using CryptInject.Tests.TestObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;
using ProtoBuf.Meta;

namespace CryptInject.Tests
{
    [TestClass]
    public class FunctionalTests
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            Monitor.Enter(TestHelper.SerialExecutionLock);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            Monitor.Exit(TestHelper.SerialExecutionLock);
        }

        [TestMethod]
        public void Functional_RunsFullyCompleteObject()
        {
            EncryptionManager.PreloadProxyTypes();
            
            var keyring = new Keyring();
            keyring.Add("AES-DES", AesEncryptionKey.Create(TripleDesEncryptionKey.Create()));
            keyring.Add("DES", TripleDesEncryptionKey.Create());
            keyring.Add("AES", AesEncryptionKey.Create());
            EncryptionManager.Keyring = keyring;

            // Create a new instance of the object you're serializing (class for this is below)
            var proxy = EncryptionManager.Create<FunctionallyCompleteTestable>();
            proxy.Populate();

            EncryptionManager.Keyring.Lock();

            Assert.AreEqual(proxy.String, default(string));
            Assert.AreEqual(proxy.Integer, default(int));
            Assert.AreEqual(proxy.Guid, default(Guid));
            Assert.AreEqual(proxy.Guid, default(Guid));
            Assert.AreEqual(proxy.SubObjectInstance, default(SubObject));

            var bf = new BinaryFormatter() {Binder = new EncryptionProxySerializationBinder()};

            var ms = new MemoryStream();
            bf.Serialize(ms, proxy);
            ms.Seek(0, SeekOrigin.Begin);

            EncryptionManager.Keyring.Unlock();

            var replaced = (FunctionallyCompleteTestable) bf.Deserialize(ms);

            var baseObject = new FunctionallyCompleteTestable();
            baseObject.Populate();

            Assert.AreEqual(replaced.Guid, baseObject.Guid);
            Assert.AreEqual(replaced.String, baseObject.String);
            Assert.AreEqual(replaced.Integer, baseObject.Integer);

            Assert.IsNotNull(replaced.SubObjectInstance);

            Assert.AreEqual(replaced.SubObjectInstance.ChildInteger, baseObject.SubObjectInstance.ChildInteger);
        }
    }
}

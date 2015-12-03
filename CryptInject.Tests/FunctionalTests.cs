using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using CryptInject.Keys;
using CryptInject.Keys.Builtin;
using CryptInject.Keys.Programmatic;
using CryptInject.Tests.TestObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CryptInject.Tests
{
    [TestClass]
    public class FunctionalTests
    {
        [TestInitialize]
        public void Initialize()
        {
            Keyring.GlobalKeyring.Add("AES-DES", AesEncryptionKey.Create(TripleDesEncryptionKey.Create()));
            Keyring.GlobalKeyring.Add("DES", TripleDesEncryptionKey.Create());
            Keyring.GlobalKeyring.Add("AES", AesEncryptionKey.Create());
        }

        [TestMethod]
        public void Functional_TestTimeWindowKey()
        {
            Keyring.GlobalKeyring.Add("TimeWindowKey", new TimeWindowKey(DateTime.Now, DateTime.Now.AddSeconds(2), chainedInnerKey:AesEncryptionKey.Create()));
            var timeWindowTestObject = new TimeWindowKeyTest().AsEncrypted();
            timeWindowTestObject.SampleString = "This is a sample string!";
            Assert.AreEqual("This is a sample string!", timeWindowTestObject.SampleString);
            Task.Delay(3000).Wait();
            Assert.IsNull(timeWindowTestObject.SampleString);
        }

        public class TimeWindowKeyTest
        {
            [Encryptable("TimeWindowKey")]
            public virtual string SampleString { get; set; }
        }

        [TestMethod]
        public void Functional_RunsFullyCompleteObject()
        {
            var encryptedObj = new FunctionallyCompleteTestable().AsEncrypted();
            encryptedObj.Populate();

            Keyring.GlobalKeyring.Lock();

            Assert.AreEqual(default(string), encryptedObj.String);
            Assert.AreEqual(default(int), encryptedObj.Integer);
            Assert.AreEqual(default(Guid), encryptedObj.Guid);
            Assert.AreEqual(default(SubObject), encryptedObj.SubObjectInstance);

            var bf = new BinaryFormatter();

            var ms = new MemoryStream();
            bf.Serialize(ms, encryptedObj);
            ms.Seek(0, SeekOrigin.Begin);

            Keyring.GlobalKeyring.Unlock();
            
            var replaced = (FunctionallyCompleteTestable) bf.Deserialize(ms);
            replaced.Relink();

            var baseObject = new FunctionallyCompleteTestable();
            baseObject.Populate();

            Assert.AreEqual(baseObject.Guid, replaced.Guid);
            Assert.AreEqual(baseObject.String, replaced.String);
            Assert.AreEqual(baseObject.Integer, replaced.Integer);

            Assert.IsNotNull(replaced.SubObjectInstance);

            Assert.AreEqual(baseObject.SubObjectInstance.ChildInteger, replaced.SubObjectInstance.ChildInteger);
        }
    }
}

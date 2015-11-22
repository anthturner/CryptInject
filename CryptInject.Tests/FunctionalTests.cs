using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using CryptInject.Keys;
using CryptInject.Keys.Builtin;
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
        public void Functional_RunsFullyCompleteObject()
        {
            var encryptedObj = new FunctionallyCompleteTestable().AsEncrypted();
            encryptedObj.Populate();

            Keyring.GlobalKeyring.Lock();

            Assert.AreEqual(encryptedObj.String, default(string));
            Assert.AreEqual(encryptedObj.Integer, default(int));
            Assert.AreEqual(encryptedObj.Guid, default(Guid));
            Assert.AreEqual(encryptedObj.Guid, default(Guid));
            Assert.AreEqual(encryptedObj.SubObjectInstance, default(SubObject));

            var bf = new BinaryFormatter();

            var ms = new MemoryStream();
            bf.Serialize(ms, encryptedObj);
            ms.Seek(0, SeekOrigin.Begin);

            Keyring.GlobalKeyring.Unlock();
            
            var replaced = (FunctionallyCompleteTestable) bf.Deserialize(ms);
            replaced.Relink();

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

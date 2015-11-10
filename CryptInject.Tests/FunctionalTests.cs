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
        private Keyring GeneratedKeyring { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            GeneratedKeyring = new Keyring();
            GeneratedKeyring.Add("AES-DES", AesEncryptionKey.Create(TripleDesEncryptionKey.Create()));
            GeneratedKeyring.Add("DES", TripleDesEncryptionKey.Create());
            GeneratedKeyring.Add("AES", AesEncryptionKey.Create());
        }

        [TestMethod]
        public void Functional_RunsFullyCompleteObject()
        {
            var functionallyCompleteObject = new FunctionallyCompleteTestable();
            functionallyCompleteObject.Populate();

            var encryptedObj = functionallyCompleteObject.AsEncrypted(GeneratedKeyring);
            
            GeneratedKeyring.Lock();

            Assert.AreEqual(encryptedObj.String, default(string));
            Assert.AreEqual(encryptedObj.Integer, default(int));
            Assert.AreEqual(encryptedObj.Guid, default(Guid));
            Assert.AreEqual(encryptedObj.Guid, default(Guid));
            Assert.AreEqual(encryptedObj.SubObjectInstance, default(SubObject));

            var bf = new BinaryFormatter();

            var ms = new MemoryStream();
            bf.Serialize(ms, encryptedObj);
            ms.Seek(0, SeekOrigin.Begin);

            GeneratedKeyring.Unlock();

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

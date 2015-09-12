using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace CryptInject.Keys.Builtin
{
    public sealed class DataSigningKey : EncryptionKey
    {
        private X509Certificate2 SigningCertificate { get; set; }

        /// <summary>
        /// Sign all encrypted data with a certificate and validate certificate integrity on load
        /// </summary>
        /// <param name="signingCertificate">Certificate to use for signing and/or verification</param>
        /// <param name="chainedInnerKey">Key operation to run prior to this key</param>
        public DataSigningKey(X509Certificate2 signingCertificate, EncryptionKey chainedInnerKey = null) : base(new byte[0], chainedInnerKey)
        {
            SigningCertificate = signingCertificate;
        }

        public DataSigningKey() : base(new byte[0], null) { }

        protected override byte[] Encrypt(PropertyInfo property, byte[] key, byte[] bytes)
        {
            var csp = SigningCertificate.PrivateKey as RSACryptoServiceProvider;
            if (csp == null)
            {
                throw new Exception("Invalid certificate. Must have private key access.");
            }

            // Hash the data
            var sha1 = new SHA1Managed();
            byte[] hash = sha1.ComputeHash(bytes);
            var signature = csp.SignHash(hash, CryptoConfig.MapNameToOID("SHA1"));

            return CreateBinaryFrame(signature, bytes);
        }

        protected override byte[] Decrypt(PropertyInfo property, byte[] key, byte[] bytes)
        {
            var frame = ExtractBinaryFrame(bytes);
            var signature = frame[0];
            var data = frame[1];

            var csp = (RSACryptoServiceProvider) SigningCertificate.PublicKey.Key;

            var sha1 = new SHA1Managed();
            byte[] hash = sha1.ComputeHash(data);
            if (csp.VerifyHash(hash, CryptoConfig.MapNameToOID("SHA1"), signature))
                return data;

            // signature verification failure
            return null;
        }

        protected override byte[] ExportData
        {
            get { return SigningCertificate.Export(X509ContentType.SerializedCert); }
            set { SigningCertificate = new X509Certificate2(value); }
        }
    }
}

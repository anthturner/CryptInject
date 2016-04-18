using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CryptInject.Keys.AzureKeyVault
{
    public static class AzureKeyringHelper
    {
        private const string KeyringPrefix = "CryptInject.";
        private static List<TrackedKeyring> _trackedKeyrings = new List<TrackedKeyring>();

        /// <summary>
        /// Download a stored keyring from Azure Key Vault and load it into a Keyring object
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <param name="secret">Client Secret</param>
        /// <param name="vault">Vault name</param>
        /// <param name="keyringName">Name of the keyring when stored in Key Vault</param>
        /// <returns></returns>
        public async static Task<Keyring> Import(string clientId, string secret, string vault, string keyringName = "Keyring")
        {
            var client = await GetClient(clientId, secret);
            var keyring = await GenerateKeyring(client, vault, $"{KeyringPrefix}.{keyringName}.");
            return keyring;
        }

        /// <summary>
        /// Download a stored keyring from Azure Key Vault and load it into a Keyring object
        /// </summary>
        /// <param name="authCallback">Callback, arguments are 'authority', 'resource', 'scope', returns 'accessToken'</param>
        /// <param name="vault">Vault name</param>
        /// <param name="keyringName">Name of the keyring when stored in Key Vault</param>
        /// <returns></returns>
        public async static Task<Keyring> Import(Func<string, string, string, string> authCallback, string vault, string keyringName = "Keyring")
        {
            var client = await GetClient(authCallback);
            var keyring = await GenerateKeyring(client, vault, $"{KeyringPrefix}.{keyringName}.");
            return keyring;
        }

        /// <summary>
        /// Download a stored keyring from Azure Key Vault, load it into a Keyring object, and track any changes
        /// made to the keyring. If a change is made, the keyring will be updated in Azure.
        /// </summary>
        /// <param name="connectionString">Key vault connection string</param>
        /// <param name="keyringName">Name of the keyring when stored in Key Vault</param>
        /// <returns></returns>
        public static async Task<Keyring> ImportSynced(string clientId, string secret, string vault, string keyringName = "Keyring")
        {
            var client = await GetClient(clientId, secret);
            var keyring = await GenerateKeyring(client, vault, $"{KeyringPrefix}.{keyringName}.");

            if (_trackedKeyrings.Any(k => k.KeyringName == keyringName && k.Vault == vault))
                _trackedKeyrings.RemoveAll(k => k.KeyringName == keyringName && k.Vault == vault);
            
            _trackedKeyrings.Add(new TrackedKeyring(keyring, client, vault, keyringName));

            keyring.KeyringChanged += async() => {
                var trackedKeyring = _trackedKeyrings.FirstOrDefault(k => k.Keyring == keyring);
                if (trackedKeyring == null) return;
                await Export(clientId, secret, vault, keyring, keyringName);
            };
            return keyring;
        }

        /// <summary>
        /// Download a stored keyring from Azure Key Vault, load it into a Keyring object, and track any changes
        /// made to the keyring. If a change is made, the keyring will be updated in Azure.
        /// </summary>
        /// <param name="authCallback">Callback, arguments are 'authority', 'resource', 'scope', returns 'accessToken'</param>
        /// <param name="vault">Vault name</param>
        /// <param name="keyringName">Name of the keyring when stored in Key Vault</param>
        /// <returns></returns>
        public static async Task<Keyring> ImportSynced(Func<string, string, string, string> authCallback, string vault, string keyringName = "Keyring")
        {
            var client = await GetClient(authCallback);
            var keyring = await GenerateKeyring(client, vault, $"{KeyringPrefix}.{keyringName}.");

            if (_trackedKeyrings.Any(k => k.KeyringName == keyringName && k.Vault == vault))
                _trackedKeyrings.RemoveAll(k => k.KeyringName == keyringName && k.Vault == vault);

            _trackedKeyrings.Add(new TrackedKeyring(keyring, client, vault, keyringName));

            keyring.KeyringChanged += async() => {
                var trackedKeyring = _trackedKeyrings.FirstOrDefault(k => k.Keyring == keyring);
                if (trackedKeyring == null) return;
                await Export(authCallback, vault, keyring, keyringName);
            };
            return keyring;
        }

        /// <summary>
        /// Upload a Keyring object to Azure Key Vault
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <param name="secret">Client Secret</param>
        /// <param name="vault">Vault name</param>
        /// <param name="keyring">Keyring to upload</param>
        /// <param name="keyringName">Name of the keyring when stored in Key Vault</param>
        public static async Task Export(string clientId, string secret, string vault, Keyring keyring, string keyringName = "Keyring")
        {
            var client = await GetClient(clientId, secret);
            foreach (var key in keyring)
            {
                var keyName = key.Name; //todo: sanitize
                var ms = new MemoryStream();
                keyring.ExportToStream(ms, key);
                ms.Seek(0, SeekOrigin.Begin);
                await client.SetSecretAsync(vault, $"{KeyringPrefix}.{keyringName}.{keyName}", System.Convert.ToBase64String(ms.ToArray()));
            }

            var remoteKeyring = await GenerateKeyring(client, vault, $"{KeyringPrefix}.{keyringName}.");
            var toBeRemoved = remoteKeyring.Where(remote => keyring.Any(k => k.Name == remote.Name));
            var deleteTasks = new List<Task>();
            foreach (var item in toBeRemoved)
            {
                deleteTasks.Add(client.DeleteSecretAsync(vault, item.Name));
            }
            await Task.WhenAll(deleteTasks);
        }

        /// <summary>
        /// Upload a Keyring object to Azure Key Vault
        /// </summary>
        /// <param name="authCallback">Callback, arguments are 'authority', 'resource', 'scope', returns 'accessToken'</param>
        /// <param name="keyring">Keyring to upload</param>
        /// <param name="keyringName">Name of the keyring when stored in Key Vault</param>
        public static async Task Export(Func<string, string, string, string> authCallback, string vault, Keyring keyring, string keyringName = "Keyring")
        {
            var client = await GetClient(authCallback);
            foreach (var key in keyring)
            {
                var keyName = key.Name; //todo: sanitize
                var ms = new MemoryStream();
                keyring.ExportToStream(ms, key);
                ms.Seek(0, SeekOrigin.Begin);
                await client.SetSecretAsync(vault, $"{KeyringPrefix}.{keyringName}.{keyName}", System.Convert.ToBase64String(ms.ToArray()));
            }

            var remoteKeyring = await GenerateKeyring(client, vault, $"{KeyringPrefix}.{keyringName}.");
            var toBeRemoved = remoteKeyring.Where(remote => keyring.Any(k => k.Name == remote.Name));
            var deleteTasks = new List<Task>();
            foreach (var item in toBeRemoved)
            {
                deleteTasks.Add(client.DeleteSecretAsync(vault, item.Name));
            }
            await Task.WhenAll(deleteTasks);
        }

        private static async Task<Keyring> GenerateKeyring(KeyVaultClient client, string vault, string prefix)
        {
            var secrets = await client.GetSecretsAsync(vault);
            var allSecrets = new List<SecretItem>(secrets.Value);
            while (secrets.NextLink != null)
            {
                secrets = await client.GetSecretsNextAsync(secrets.NextLink);
                allSecrets.AddRange(secrets.Value);
            }

            var keyring = new Keyring();

            foreach (var secret in allSecrets.Where(s => s.Identifier.Name.StartsWith(prefix)))
            {
                var secretItem = await client.GetSecretAsync(secret.Id);
                var bytes = System.Convert.FromBase64String(secretItem.Value);
                keyring.ImportFromStream(new MemoryStream(bytes));
            }

            return keyring;
        }

        private static async Task<KeyVaultClient> GetClient(string clientId, string secret)
        {
            var mre = new ManualResetEvent(false);
            var cli = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(async (authority, resource, scope) =>
            {
                var clientCredential = new ClientCredential(clientId, secret);
                var context = new AuthenticationContext(authority, null);
                var result = await context.AcquireTokenAsync(resource, clientCredential);
                mre.Set();
                return result.AccessToken;
            }));
            if (!mre.WaitOne(5000)) // 5s timeout
                throw new System.Exception("Authentication timed out!");
            return await Task.FromResult(cli);
        }

        /// <summary>
        /// Retrieve a client object, using a given Func for an authentication callback
        /// </summary>
        /// <param name="authenticationCallback">Arguments are 'authority', 'resource', 'scope', returns 'accessToken'</param>
        /// <returns></returns>
        private static async Task<KeyVaultClient> GetClient(Func<string, string, string, string> authenticationCallback)
        {
            var mre = new ManualResetEvent(false);
            var cli = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(async (authority, resource, scope) =>
            {
                return await Task.FromResult(authenticationCallback(authority, resource, scope));
            }));
            if (!mre.WaitOne(5000)) // 5s timeout
                throw new System.Exception("Authentication timed out!");
            return await Task.FromResult(cli);
        }

        internal class TrackedKeyring
        {
            internal Keyring Keyring { get; set; }
            internal KeyVaultClient Client { get; set; }
            internal string Vault { get; set; }
            internal string KeyringName { get; set; }

            public TrackedKeyring(Keyring keyring, KeyVaultClient client, string vault, string keyringName)
            {
                Keyring = keyring;
                Client = client;
                Vault = vault;
                KeyringName = keyringName;
            }
        }
    }
}

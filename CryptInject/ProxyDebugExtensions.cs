using System;
using System.Text;

namespace CryptInject
{
#if DEBUG
    public static class ProxyDebugExtensions
    {
        public static string AsDebugString(this object obj)
        {
            if (Proxy.EncryptedInstanceFactory.GetTrackedInstance(obj) == null)
            {
                throw new Exception("Not a CryptInject object");
            }

            var sb = new StringBuilder();
            sb.AppendLine(obj.GetType().FullName);

            var properties = Proxy.EncryptedInstanceFactory.GetTrackedTypeByEncrypted(obj.GetType()).Properties;
            foreach (var prop in properties)
            {
                sb.AppendFormat("Property: {0}\n", prop.Key);
                sb.AppendFormat("Property Type: {0}\n", prop.Value.Original.PropertyType.FullName);
                sb.AppendFormat("Key Alias: {0}\n", prop.Value.KeyAlias);

                var localKeyring = prop.Value.GetLocalKeyring();
                sb.AppendFormat("Local Keyring Provides: {0}\n", string.Join(", ", localKeyring.KeysProvided));

                sb.AppendFormat("Stored Value: {0}\n", prop.Value.GetBackingValue(obj));

                sb.AppendFormat("Cache Type: {0}\n", prop.Value.Cache != null ? prop.Value.Cache.FieldType.FullName : "<none>");
                sb.AppendFormat("Cache Value: {0}\n", prop.Value.GetBackingValue(obj));

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
#endif
}

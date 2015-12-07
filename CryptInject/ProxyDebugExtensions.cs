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
                sb.AppendFormat("\tProperty: {0}\t ({1})\n", prop.Key, prop.Value.Original.PropertyType.FullName);
                sb.AppendFormat("\tRequired Key Alias: {0}\n", prop.Value.KeyAlias);
                sb.AppendFormat("\tStored Value: {0}\n", BitConverter.ToString(prop.Value.GetBackingValue(obj)));
                
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
#endif
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CryptInject.WcfExample
{
    public class KnownTypesProvider
    {
        public static IEnumerable<Type> GetKnownTypes(ICustomAttributeProvider provider)
        {
            var list = new List<Type>();
            var encryptableTypes = CryptInject.DataWrapperExtensions.GetAllEncryptableTypes();
            foreach (var type in encryptableTypes)
            {
                list.Add(type.GetEncryptedType() ?? Activator.CreateInstance(type).AsEncrypted().GetType());
            }
            return list;
        }
    }
}

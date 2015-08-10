using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace CryptInject
{
    public sealed class EncryptionProxySerializationBinder : SerializationBinder
    {
        private SerializationBinder _fallback;

        public EncryptionProxySerializationBinder(SerializationBinder fallback = null)
        {
            _fallback = fallback;
        }

        public override Type BindToType(string assemblyNameStr, string typeName)
        {
            var assemblyName = new AssemblyName(assemblyNameStr);
            if (assemblyName.Name != DataStorageMixinFactory.ASSEMBLY_NAME && !assemblyName.Name.StartsWith("DynamicProxyGenAssembly"))
            {
                if (_fallback != null)
                    return _fallback.BindToType(assemblyNameStr, typeName);
                else
                {
                    var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
                    if (loadedAssembly == null)
                        return null;

                    return loadedAssembly.GetType(typeName);
                }
            }
            var assembly = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == assemblyName.Name).ToList();

            return assembly.Select(asm => asm.GetType(typeName)).FirstOrDefault(t => t != null);
        }
    }
}

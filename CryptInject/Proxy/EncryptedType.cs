using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using CryptInject.Keys;

namespace CryptInject.Proxy
{
    internal class EncryptedType
    {
        internal static SynchronizedCollection<Type> PendingGenerations { get; private set; }

        internal static ProxyGenerator Generator { get; set; }

        internal Type OriginalType { get; private set; }
        internal Type MixinType { get; private set; }
        internal Type ProxyType { get; private set; }
        internal Keyring Keyring { get; private set; }
        internal EncryptionProxyConfiguration Configuration { get; private set; }

        internal Dictionary<string, EncryptedProperty> Properties { get; private set; }

        static EncryptedType()
        {
            PendingGenerations = new SynchronizedCollection<Type>();
        }
        
        internal EncryptedType(Type type, EncryptionProxyConfiguration configuration = null)
        {
            if (Generator == null) Generator = new ProxyGenerator();

            OriginalType = type;
            Configuration = configuration ?? new EncryptionProxyConfiguration();
            MixinType = DataStorageMixinFactory.Generate(OriginalType).GetType();

            Properties = new Dictionary<string, EncryptedProperty>();
            PendingGenerations.Add(type);
            var generatedSample = GenerateInstance(type);
            PendingGenerations.Remove(type);
            var eligibleProperties = DataStorageMixinFactory.GetEncryptionEligibleProperties(generatedSample.GetType());
            foreach (var eligibleProperty in eligibleProperties)
            {
                Properties.Add(eligibleProperty.Name, new EncryptedProperty(eligibleProperty));
            }

            ProxyType = generatedSample.GetType();
            Keyring = new Keyring();
        }

        internal T GenerateInstance<T>() where T : class
        {
            return (T) GenerateInstance(typeof(T));
        }

        internal object GenerateInstance(Type type)
        {
            if (type != OriginalType && !OriginalType.IsAssignableFrom(type))
                throw new Exception(string.Format("Provided type does not match or inherit from '{0}'", OriginalType.FullName));

            var options = new ProxyGenerationOptions(new CryptInjectHook());
            options.AddMixinInstance(Activator.CreateInstance(MixinType));
            return Generator.CreateClassProxy(OriginalType, options, new EncryptedDataStorageInterceptor());
        }

        internal bool IsManagingProperty(string propertyName)
        {
            return Properties.ContainsKey(propertyName);
        }
    }
}

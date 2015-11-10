using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;

namespace CryptInject.Proxy
{
    internal static class EncryptedInstanceFactory
    {
        private static List<EncryptedType> Types { get; set; }
        private static List<EncryptedInstance> Instances { get; set; }
        private static DateTime LastPrune { get; set; }

        static EncryptedInstanceFactory()
        {
            Types = new List<EncryptedType>();
            Instances = new List<EncryptedInstance>();
            LastPrune = DateTime.Now;
        }

        internal static T GenerateTrackedInstance<T>(T obj, EncryptionProxyConfiguration configuration = null) where T : class
        {
            var trackedType = GetTrackedType(typeof (T), configuration);
            var trackedInstance = new EncryptedInstance(trackedType, trackedType.GenerateInstance<T>());
            Instances.Add(trackedInstance);
            return (T)trackedInstance.Reference.Target;
        }

        internal static object GenerateTrackedInstance(Type type, EncryptionProxyConfiguration configuration = null)
        {
            var trackedType = GetTrackedType(type, configuration);
            var trackedInstance = new EncryptedInstance(trackedType, trackedType.GenerateInstance(type));
            Instances.Add(trackedInstance);
            return trackedInstance.Reference.Target;
        }

        internal static void AttachInterceptor<T>(T obj, EncryptionProxyConfiguration configuration = null) where T : class
        {
            var trackedType = GetTrackedType(typeof(T));

            obj.GetType().GetField("__interceptors", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(obj, new IInterceptor[1] { new EncryptedDataStorageInterceptor() });
            if (GetTrackedInstance(obj) == null)
            {
                Instances.Add(new EncryptedInstance(trackedType, obj));
            }
        }

        internal static void AttachToExistingObject<T>(T obj, EncryptionProxyConfiguration configuration = null) where T : class
        {
            var trackedType = GetTrackedType(typeof (T));
            var storageMixin = Activator.CreateInstance(trackedType.MixinType);
            
            obj.GetType().GetField("__interceptors", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(obj, new IInterceptor[1] { new EncryptedDataStorageInterceptor() });
            obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).FirstOrDefault(f => f.Name.StartsWith("__mixin_IEncryptedData_")).SetValue(obj, storageMixin);

            if (GetTrackedInstance(obj) == null)
            {
                Instances.Add(new EncryptedInstance(trackedType, obj));
            }
        }

        internal static EncryptedInstance GetTrackedInstance(object obj)
        {
            PruneGCedInstanceReferences();
            return Instances.FirstOrDefault(i => i.References(obj));
        }

        internal static EncryptedType GetTrackedTypeOrNull(Type type)
        {
            return Types.FirstOrDefault(t => t.OriginalType == type);
        }

        internal static EncryptedType GetTrackedTypeByEncrypted(Type type)
        {
            return Types.FirstOrDefault(t => t.ProxyType == type);
        }

        internal static EncryptedType GetTrackedType(Type type, EncryptionProxyConfiguration configuration = null)
        {
            var existingType = Types.FirstOrDefault(t => t.OriginalType == type);
            if (existingType == null)
            {
                if (configuration == null)
                    configuration = new EncryptionProxyConfiguration();
                existingType = new EncryptedType(type, configuration);
                Types.Add(existingType);
            }
            return existingType;
        }

        internal static void UpdateInstancesFromKeyring()
        {
            Instances.ForEach(t => t.UpdateFromKeyringScopes());
        }

        private static void PruneGCedInstanceReferences()
        {
            Instances.RemoveAll(i => !i.IsAlive);
        }
    }
}
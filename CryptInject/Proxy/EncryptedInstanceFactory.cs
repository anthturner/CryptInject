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
        private static Dictionary<Type, EncryptedType> TypesByProxy { get; set; }
        private static Dictionary<Type, EncryptedType> TypesByOriginal { get; set; }
        private static DateTime LastPrune { get; set; }

        static EncryptedInstanceFactory()
        {
            Types = new List<EncryptedType>();
            Instances = new List<EncryptedInstance>();
            TypesByProxy = new Dictionary<Type, EncryptedType>();
            TypesByOriginal = new Dictionary<Type, EncryptedType>();
            LastPrune = DateTime.Now;
        }
        
        internal static object GenerateTrackedInstance(Type type, EncryptionProxyConfiguration configuration = null)
        {
            var trackedType = GetTrackedType(type, configuration);
            var trackedInstance = new EncryptedInstance(trackedType, trackedType.GenerateInstance(type));
            Instances.Add(trackedInstance);
            return trackedInstance.Reference.Target;
        }

        internal static void AttachInterceptor(object obj, EncryptionProxyConfiguration configuration = null)
        {
            var trackedType = GetTrackedType(obj.GetType());

            obj.GetType().GetField("__interceptors", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(obj, new IInterceptor[1] { new EncryptedDataStorageInterceptor() });
            if (GetTrackedInstance(obj) == null)
            {
                Instances.Add(new EncryptedInstance(trackedType, obj));
            }
        }

        internal static void AttachToExistingObject(object obj, EncryptionProxyConfiguration configuration = null)
        {
            var trackedType = GetTrackedType(obj.GetType());
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
            if (Instances.Count % 3 == 0)
                PruneGCedInstanceReferences();
            return Instances.FirstOrDefault(i => i.References(obj));
        }

        internal static EncryptedType GetTrackedTypeOrNull(Type type)
        {
            EncryptedType returnVal;
            TypesByOriginal.TryGetValue(type, out returnVal);
            return returnVal;
        }

        internal static EncryptedType GetTrackedTypeByEncrypted(Type type)
        {
            EncryptedType returnVal;
            TypesByProxy.TryGetValue(type, out returnVal);
            return returnVal;
        }

        internal static EncryptedType GetTrackedType(Type type, EncryptionProxyConfiguration configuration = null)
        {
            var encryptedType = GetTrackedTypeByEncrypted(type);
            if (encryptedType != null)
                return encryptedType;

            var existingType = GetTrackedTypeOrNull(type);
            if (existingType == null)
            {
                if (configuration == null)
                    configuration = new EncryptionProxyConfiguration();
                existingType = new EncryptedType(type, configuration);
                Types.Add(existingType);
                TypesByProxy.Add(existingType.ProxyType, existingType);
                TypesByOriginal.Add(type, existingType);
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
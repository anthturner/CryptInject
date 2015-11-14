﻿using System;
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
            PruneGCedInstanceReferences();
            return Instances.FirstOrDefault(i => i.References(obj));
        }

        internal static EncryptedType GetTrackedTypeOrNull(Type type)
        {
            return Types.FirstOrDefault(t => t.OriginalType == type);
        }

        internal static EncryptedType GetTrackedTypeByEncrypted(Type type)
        {
            return Types.FirstOrDefault(t => t.ProxyType.FullName == type.FullName);
        }

        internal static EncryptedType GetTrackedType(Type type, EncryptionProxyConfiguration configuration = null)
        {
            var encryptedType = GetTrackedTypeByEncrypted(type);
            if (encryptedType != null)
                return encryptedType;

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
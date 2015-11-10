using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using CryptInject.Keys;
using CryptInject.Proxy;

namespace CryptInject
{
    public static class DataWrapperExtensions
    {
        static DataWrapperExtensions()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name.StartsWith("DynamicProxyGenAssembly2") || args.Name.StartsWith(DataStorageMixinFactory.ASSEMBLY_NAME))
                {
                    return DataStorageMixinFactory.MixinAssembly;
                }
                return null;
            };
        }

        public static void Initialize() { }

        public static T AsEncrypted<T>(this T inputObject, Keyring keyring = null, EncryptionProxyConfiguration configuration = null) where T : class
        {
            if (keyring == null)
                keyring = new Keyring();

            // Is the object already linked?
            if (HasValidEncryptionExtensions(inputObject))
            {
                EncryptedInstanceFactory.AttachInterceptor(inputObject, configuration);
                inputObject.GetLocalKeyring().Import(keyring);
                return inputObject;
            }

            // Does this object already have the bits we can attach to?
            if (HasUnlinkedEncryptionExtensions(inputObject))
            {
                EncryptedInstanceFactory.AttachToExistingObject(inputObject, configuration);
                inputObject.GetLocalKeyring().Import(keyring);
                return inputObject;
            }

            var trackedInstance = EncryptedInstanceFactory.GenerateTrackedInstance(inputObject, configuration);
            trackedInstance.GetLocalKeyring().Import(keyring);
            CopyObjectProperties(inputObject, trackedInstance);
            return trackedInstance;
        }

        public static Type GetEncryptedType(this Type nonEncryptedType)
        {
            var trackedType = EncryptedInstanceFactory.GetTrackedTypeOrNull(nonEncryptedType);
            return trackedType == null ? null : trackedType.ProxyType;
        }

        public static Type GetNonEncryptedType(this Type encryptedType)
        {
            var trackedType = EncryptedInstanceFactory.GetTrackedTypeByEncrypted(encryptedType);
            return trackedType == null ? null : trackedType.OriginalType;
        }

        #region Keyring Management
        public static Keyring GetReadOnlyUnifiedKeyring<T>(this T objectInstance) where T : class
        {
            var keyring = new Keyring();
            keyring.Import(Keyring.GlobalKeyring);
            keyring.Import(objectInstance.GetTypeKeyring());
            keyring.Import(objectInstance.GetLocalKeyring());
            keyring.ReadOnly = true;
            return keyring;
        }

        public static Keyring GetGlobalKeyring<T>(this T objectInstance) where T : class
        {
            return Keyring.GlobalKeyring;
        }

        public static Keyring GetTypeKeyring<T>(this T objectInstance) where T : class
        {
            var trackedType = EncryptedInstanceFactory.GetTrackedType(typeof (T));
            return trackedType.Keyring;
        }

        public static Keyring GetLocalKeyring<T>(this T objectInstance) where T : class
        {
            var trackedInstance = EncryptedInstanceFactory.GetTrackedInstance(objectInstance);
            if (trackedInstance == null)
                throw new Exception("Object instance is not an encrypted instance");
            return trackedInstance.InstanceKeyring;
        }
        #endregion

        /// <summary>
        /// Retrieve a recursive list of known types within the object. This is used for any DataContractSerializer-driven serializations.
        /// </summary>
        /// <param name="obj">Object to generate known types from</param>
        /// <returns>Array of Types present in the object tree</returns>
        public static Type[] GetKnownTypes<T>(this T obj) where T : class
        {
            var types = new List<Type>();
            if (obj == null)
                return new Type[0];

            foreach (var prop in obj.GetType().GetProperties())
            {
                var val = prop.GetValue(obj, null);
                if (val != null)
                    types.Add(val.GetType());

                if (prop.PropertyType.GetProperties().Any() && prop.PropertyType != typeof(string))
                    types.AddRange(GetKnownTypes(val));
            }

            return types.Distinct().ToArray();
        }

        private static void CopyObjectProperties(object inputObject, object proxiedInstance)
        {
            foreach (var property in inputObject.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var target = proxiedInstance.GetType().GetProperty(property.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (target != null)
                    target.SetValue(proxiedInstance, property.GetValue(inputObject));
            }
            foreach (var field in inputObject.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var target = proxiedInstance.GetType().GetField(field.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (target != null)
                    target.SetValue(proxiedInstance, field.GetValue(inputObject));
            }
        }

        #region Type Interrogators for Encryption Fields
        private static bool HasValidEncryptionExtensions(object inputObject)
        {
            var objectFields = inputObject.GetType().GetFields();

            if (!objectFields.Any(f =>
                f.Name == "__interceptors" &&
                ((IInterceptor[]) f.GetValue(inputObject)).Any(interceptor => interceptor is EncryptedDataStorageInterceptor)))
                return false;

            if (!objectFields.Any(f => f.Name.StartsWith("__mixin_IEncryptedData_") &&
                f.GetValue(inputObject) != null))
                return false;

            return true;
        }

        private static bool HasUnlinkedEncryptionExtensions(object inputObject)
        {
            var objectFields = inputObject.GetType().GetFields();

            if (!objectFields.Any(f => f.Name == "__interceptors"))
                return false;

            if (!objectFields.Any(f => f.Name.StartsWith("__mixin_IEncryptedData_")))
                return false;

            return true;
        }
        #endregion
    }
}

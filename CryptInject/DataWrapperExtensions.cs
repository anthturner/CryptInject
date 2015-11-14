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
                if (args.Name.StartsWith(DataStorageMixinFactory.ASSEMBLY_NAME))
                {
                    return DataStorageMixinFactory.MixinAssembly;
                }
                else if (args.Name.StartsWith(ModuleScope.DEFAULT_ASSEMBLY_NAME))
                {
                    return EncryptedType.Generator.ProxyBuilder.ModuleScope.WeakNamedModule.Assembly;
                }
                return null;
            };
        }

        /// <summary>
        /// Returns a list of all types in all loaded assemblies that contain properties marked with the [Encryptable] attribute
        /// </summary>
        /// <returns>List of all types in all loaded assemblies that contain properties marked with the [Encryptable] attribute</returns>
        public static List<Type> GetAllEncryptableTypes()
        {
            var types = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.GetProperties().Any(p => p.GetCustomAttribute<EncryptableAttribute>() != null))
                    {
                        types.Add(type);
                    }
                }
            }
            return types;
        }

        /// <summary>
        /// Re-link proxy bindings to the given object
        /// </summary>
        /// <typeparam name="T">Type of object being linked</typeparam>
        /// <param name="inputObject">Object to link proxies into</param>
        /// <param name="keyring">Local keyring to use in resulting linked object</param>
        /// <param name="configuration">Local configuration to use in resulting linked object</param>
        public static void Relink<T>(this T inputObject, Keyring keyring = null, EncryptionProxyConfiguration configuration = null) where T : class
        {
            if (EncryptedType.PendingGenerations.Contains(typeof(T)))
            {
                // Ignore any recursive generation from constructors
                return;
            }

            if (keyring == null)
                keyring = new Keyring();

            AttemptRelink(inputObject, keyring, configuration);
        }

        /// <summary>
        /// Return a copy of the given object with proxy bindings (this could either be a relink or a creation operation)
        /// </summary>
        /// <param name="inputObject">Object being linked or wrapped in an encryption proxy</param>
        /// <param name="keyring">Local keyring to use in resulting linked object</param>
        /// <param name="configuration">Local configuration to use in resulting linked object</param>
        /// <returns>Object of same type as input, with proxy bindings</returns>
        public static object AsEncrypted(this object inputObject, Keyring keyring = null, EncryptionProxyConfiguration configuration = null)
        {
            if (keyring == null)
                keyring = new Keyring();

            if (!AttemptRelink(inputObject, keyring, configuration))
            {
                var trackedInstance = EncryptedInstanceFactory.GenerateTrackedInstance(inputObject.GetType(), configuration);
                trackedInstance.GetLocalKeyring().Import(keyring);
                CopyObjectProperties(inputObject, trackedInstance);
                return trackedInstance;
            }
            else
            {
                return inputObject;
            }
        }

        /// <summary>
        /// Return a copy of the given object with proxy bindings (this could either be a relink or a creation operation)
        /// </summary>
        /// <typeparam name="T">Type of object being linked</typeparam>
        /// <param name="inputObject">Object being linked or wrapped in an encryption proxy</param>
        /// <param name="keyring">Local keyring to use in resulting linked object</param>
        /// <param name="configuration">Local configuration to use in resulting linked object</param>
        /// <returns>Object of same type as input, with proxy bindings</returns>
        public static T AsEncrypted<T>(this T inputObject, Keyring keyring = null, EncryptionProxyConfiguration configuration = null) where T : class
        {
            if (keyring == null)
                keyring = new Keyring();

            if (!AttemptRelink(inputObject, keyring, configuration))
            {
                var trackedInstance = EncryptedInstanceFactory.GenerateTrackedInstance(inputObject.GetType(), configuration);
                trackedInstance.GetLocalKeyring().Import(keyring);
                CopyObjectProperties(inputObject, trackedInstance);
                return (T)trackedInstance;
            }
            else
            {
                return inputObject;
            }
        }

        /// <summary>
        /// Get the proxy (encrypted) type for the given regular type
        /// </summary>
        /// <param name="nonEncryptedType">"Regular" object type ("Patient")</param>
        /// <returns>Encrypted object type ("PatientProxy")</returns>
        public static Type GetEncryptedType(this Type nonEncryptedType)
        {
            var trackedType = EncryptedInstanceFactory.GetTrackedTypeOrNull(nonEncryptedType);
            return trackedType == null ? null : trackedType.ProxyType;
        }

        /// <summary>
        /// Get the regular (non-encrypted) type for the given encrypted type
        /// </summary>
        /// <param name="encryptedType">Encrypted object type ("PatientProxy")</param>
        /// <returns>Regular object type ("Patient")</returns>
        public static Type GetNonEncryptedType(this Type encryptedType)
        {
            var trackedType = EncryptedInstanceFactory.GetTrackedTypeByEncrypted(encryptedType);
            return trackedType == null ? null : trackedType.OriginalType;
        }

        #region Keyring Management
        /// <summary>
        /// Retrieve an effective keyring for a given object in read-only mode; this returns a combination of the global, type, and instance keyrings.
        /// </summary>
        /// <typeparam name="T">Type of encrypted object</typeparam>
        /// <param name="objectInstance">Instance of an encrypted object</param>
        /// <returns>Effective keyring</returns>
        public static Keyring GetReadOnlyUnifiedKeyring<T>(this T objectInstance) where T : class
        {
            var keyring = new Keyring();
            keyring.Import(Keyring.GlobalKeyring);
            keyring.Import(objectInstance.GetTypeKeyring());
            keyring.Import(objectInstance.GetLocalKeyring());
            keyring.ReadOnly = true;
            return keyring;
        }

        /// <summary>
        /// Retrieve the global keyring (alias for Keyring.GlobalKeyring)
        /// </summary>
        /// <typeparam name="T">Type of encrypted object</typeparam>
        /// <param name="objectInstance">Instance of an encrypted object</param>
        /// <returns>Global keyring</returns>
        public static Keyring GetGlobalKeyring<T>(this T objectInstance) where T : class
        {
            return Keyring.GlobalKeyring;
        }

        /// <summary>
        /// Retrieve the keyring for an object's Type
        /// </summary>
        /// <typeparam name="T">Type of encrypted object</typeparam>
        /// <param name="objectInstance">Instance of an encrypted object</param>
        /// <returns>Type keyring</returns>
        public static Keyring GetTypeKeyring<T>(this T objectInstance) where T : class
        {
            var trackedType = EncryptedInstanceFactory.GetTrackedType(typeof (T));
            return trackedType.Keyring;
        }

        /// <summary>
        /// Retrieve the keyring for an instance of an object
        /// </summary>
        /// <typeparam name="T">Type of encrypted object</typeparam>
        /// <param name="objectInstance">Instance of an encrypted object</param>
        /// <returns>Local (instance) keyring</returns>
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

        private static bool AttemptRelink(object inputObject, Keyring keyring, EncryptionProxyConfiguration configuration)
        {
            // Is the object already linked?
            if (HasValidEncryptionExtensions(inputObject))
            {
                EncryptedInstanceFactory.AttachInterceptor(inputObject, configuration);
                inputObject.GetLocalKeyring().Import(keyring);
                return true;
            }

            // Does this object already have the bits we can attach to?
            if (HasUnlinkedEncryptionExtensions(inputObject))
            {
                EncryptedInstanceFactory.AttachToExistingObject(inputObject, configuration);
                inputObject.GetLocalKeyring().Import(keyring);
                return true;
            }

            return false;
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
                f.GetValue(inputObject) != null &&
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

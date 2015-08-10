using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using CryptInject.Keys;

namespace CryptInject
{
    public static class EncryptionManager
    {
        internal static List<ProxyEncapsulatedType> ProxiedTypes { get; set; }
        public static Keyring Keyring { get; set; }

        static EncryptionManager()
        {
            ProxiedTypes = new List<ProxyEncapsulatedType>();
            Keyring = new Keyring();
        }

        /// <summary>
        /// Generate proxies in memory for all of the types currently in memory that contain properties with the [Encryptable] attribute
        /// </summary>
        public static void PreloadProxyTypes(EncryptionProxyConfiguration configuration = null)
        {
            if (configuration == null)
                configuration = new EncryptionProxyConfiguration();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    try
                    {
                        if (DataStorageMixinFactory.GetEncryptionEligibleProperties(type).Any())
                        {
                            Create(type, configuration);
                        }
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Create a proxied instance of the given Type, using the configuration provided (if this is the first time wrapping this Type)
        /// </summary>
        /// <typeparam name="T">Type to wrap</typeparam>
        /// <param name="configuration">Configuration to apply to type</param>
        /// <remarks>If configuration is specified but the Type has been previously generated, the configuration will be dropped</remarks>
        /// <returns>Wrapped type</returns>
        public static T Create<T>(EncryptionProxyConfiguration configuration = null) where T : class
        {
            var proxyType = ProxiedTypes.FirstOrDefault(p => p.OriginalType == typeof (T));
            if (proxyType == null)
            {
                if (configuration == null)
                    configuration = new EncryptionProxyConfiguration();
                proxyType = new ProxyEncapsulatedType<T>(configuration);
                ProxiedTypes.Add(proxyType);
            }
            return ((ProxyEncapsulatedType<T>)proxyType).Create();
        }

        /// <summary>
        /// Create a proxied instance of the given Type, using the configuration provided (if this is the first time wrapping this Type)
        /// </summary>
        /// <param name="type">Type to wrap</param>
        /// <param name="configuration">Configuration to apply to type</param>
        /// <remarks>If configuration is specified but the Type has been previously generated, the configuration will be dropped</remarks>
        /// <returns>Wrapped type</returns>
        public static object Create(Type type, EncryptionProxyConfiguration configuration = null)
        {
            var method = typeof(EncryptionManager).GetMethod("Create", new Type[] {typeof(EncryptionProxyConfiguration)});
            var generic = method.MakeGenericMethod(type);
            return generic.Invoke(null, new object[] { configuration });
        }

        /// <summary>
        /// Build a new encryption proxy from an existing object, copying properties and fields, returning the populated proxy object.
        /// </summary>
        /// <typeparam name="T">Type to wrap</typeparam>
        /// <param name="obj">Object to copy data from</param>
        /// <param name="configuration">Configuration to apply to type</param>
        /// <remarks>If configuration is specified but the Type has been previously generated, the configuration will be dropped</remarks>
        /// <returns>Wrapped type with data from given object</returns>
        public static T Attach<T>(T obj, EncryptionProxyConfiguration configuration = null) where T : class
        {
            var proxyType = (ProxyEncapsulatedType<T>) ProxiedTypes.FirstOrDefault(p => p.OriginalType == typeof (T));
            if (proxyType != null && proxyType.Instances.Any(i => i.Reference.IsAlive && i.References(obj)))
            {
                throw new Exception("Object instance already tracked by encryption proxy.");
            }

            var prototype = Create<T>(configuration);

            foreach (var property in obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var target = prototype.GetType().GetProperty(property.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                target.SetValue(prototype, property.GetValue(obj));
            }
            foreach (var field in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var target = prototype.GetType().GetField(field.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                target.SetValue(prototype, field.GetValue(obj));
            }
            return prototype;
        }

        /// <summary>
        /// Check if the EncryptionManger currently has a proxy object generated for a given Type
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns><c>TRUE</c> if a proxy has been generated for this Type, otherwise <c>FALSE</c></returns>
        public static bool Tracks(Type type)
        {
            return ProxiedTypes.Any(p => p.OriginalType == type);
        }

        /// <summary>
        /// Populate proxy bindings on a proxy object that has just been emitted. Generally, this applies when something is using Activator.CreateInstance,
        /// such as in most default serializers. The object MUST already be a proxy type, otherwise use either Attach() or Create().
        /// </summary>
        /// <param name="proxyObj">Object to apply proxy bindings to</param>
        public static void ForceProxyRebind(object proxyObj)
        {
            var proxyType = proxyObj.GetType();
            var proxiedType = ProxiedTypes.FirstOrDefault(p => p.OriginalType == proxyObj.GetType().BaseType);
            if (proxiedType == null)
                return;
            var storageMixin = Activator.CreateInstance(proxiedType.GetStorageMixinType());
            
            proxyType.GetField("__interceptors").SetValue(proxyObj, new IInterceptor[1] { new EncryptedDataStorageInterceptor() });
            proxyType.GetFields().FirstOrDefault(f => f.Name.StartsWith("__mixin_IEncryptedData_")).SetValue(proxyObj, storageMixin);
        }

        /// <summary>
        /// Get the proxied type for a normal Type, if one exists.
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>Proxy type, or <c>NULL</c> if there has not yet been a proxy generated</returns>
        public static Type GetProxyType(Type type)
        {
            var pType = ProxiedTypes.FirstOrDefault(p => p.OriginalType == type);
            if (pType == null) return null;
            return pType.GetProxyType();
        }

        /// <summary>
        /// Retrieve a recursive list of known types within the object. This is used for any DataContractSerializer-driven serializations.
        /// </summary>
        /// <param name="obj">Object to generate known types from</param>
        /// <returns>Array of Types present in the object tree</returns>
        public static Type[] GetKnownTypes(object obj)
        {
            var types = new List<Type>();
            if (obj == null)
                return new Type[0];

            foreach (var prop in obj.GetType().GetProperties())
            {
                var val = prop.GetValue(obj);
                if (val != null)
                    types.Add(val.GetType());

                if (prop.PropertyType.GetProperties().Any())
                    types.AddRange(GetKnownTypes(val));
            }

            return types.Distinct().ToArray();
        }

        internal static ProxyEncapsulatedType GetProxiedType(Type type)
        {
            return ProxiedTypes.FirstOrDefault(p => p.OriginalType == type);
        }
    }
}

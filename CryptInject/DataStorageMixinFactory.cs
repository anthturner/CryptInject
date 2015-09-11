using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace CryptInject
{
    internal static class DataStorageMixinFactory
    {
        internal static string BACKING_PROPERTY_PREFIX = "_encStored_";
        internal static string CACHE_PROPERTY_PREFIX = "_encStored_cache_";

        internal static string ASSEMBLY_NAME = "Dynamic_EncryptedDataStorage";
        private static string IMPLEMENTED_TYPE_PREFIX = "EncryptedData_Dynamic_";
        private static string INTERFACE_TYPE_PREFIX = "IEncryptedData_Dynamic_";

        private static readonly AssemblyBuilder AssemblyBuilder;
        private static readonly ModuleBuilder ModuleBuilder;

        static DataStorageMixinFactory()
        {
            if (AssemblyBuilder == null)
            {
                var assemblyName = new AssemblyName() {Name = ASSEMBLY_NAME};
                var thisDomain = Thread.GetDomain();
                AssemblyBuilder = thisDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                ModuleBuilder = AssemblyBuilder.DefineDynamicModule(AssemblyBuilder.GetName().Name, false);
            }
        }

        internal static object Generate<T>()
        {
            var existingType = AssemblyBuilder.DefinedTypes.FirstOrDefault(t => t.Name == IMPLEMENTED_TYPE_PREFIX + typeof (T).Name);
            if (existingType != null)
            {
                return Activator.CreateInstance(existingType);
            }

            var properties = GetEncryptionEligibleProperties(typeof(T)).ToArray();
            var interfaceType = CreateInterfaceType(INTERFACE_TYPE_PREFIX + typeof(T).Name, properties);
            var implementedType = CreateImplementationType(IMPLEMENTED_TYPE_PREFIX + typeof(T).Name, interfaceType.GetProperties(), typeof(T));
            implementedType.AddInterfaceImplementation(interfaceType);

            var createdImplementedType = implementedType.CreateType();

            return Activator.CreateInstance(createdImplementedType);
        }

        internal static IEnumerable<PropertyInfo> GetEncryptionEligibleProperties(Type type)
        {
            var eligibleProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).Where(p => p.GetCustomAttribute<EncryptableAttribute>() != null).ToList();
            var propertiesMarkedWithoutVirtual = eligibleProperties.Where(prop => !prop.GetMethod.IsVirtual || !prop.SetMethod.IsVirtual).ToList();
            if (propertiesMarkedWithoutVirtual.Count > 0)
            {
                throw new Exception("All properties marked with [Encryptable] must also be marked virtual:" + Environment.NewLine +
                    string.Join(Environment.NewLine + Environment.NewLine, propertiesMarkedWithoutVirtual.Select(p => p.DeclaringType.Name + "." + p.Name)));
            }

            return eligibleProperties.Where(p => p.GetGetMethod().IsVirtual && p.GetSetMethod().IsVirtual);
        }

        #region Type Generation
        private static TypeBuilder CreateImplementationType(string name, IEnumerable<PropertyInfo> properties, Type mirroredType, bool copyClassAttributes = true)
        {
            TypeBuilder typeBuilder = ModuleBuilder.DefineType(name,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.AutoLayout,
                typeof(object));

            if (copyClassAttributes)
            {
                CopyProperties(mirroredType, typeBuilder);
            }

            foreach (PropertyInfo pi in properties)
            {
                string piName = pi.Name;
                Type propertyType = pi.PropertyType;
                
                FieldBuilder field = typeBuilder.DefineField("_" + piName, propertyType, FieldAttributes.Private);

                MethodInfo getMethod = pi.GetGetMethod();
                if (getMethod != null)
                {
                    MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                        getMethod.Name,
                        MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual,
                        propertyType,
                        Type.EmptyTypes);

                    var ilGenerator = methodBuilder.GetILGenerator();
                    ilGenerator.Emit(OpCodes.Ldarg_0);      // Load "this"
                    ilGenerator.Emit(OpCodes.Ldfld, field); // Load the backing field
                    ilGenerator.Emit(OpCodes.Ret);          // Return the value on the stack (field)

                    // Override the get method from the original type to this IL-generated version
                    typeBuilder.DefineMethodOverride(methodBuilder, getMethod);
                }

                var setMethod = pi.GetSetMethod();
                if (setMethod != null)
                {
                    var methodBuilder = typeBuilder.DefineMethod(
                        setMethod.Name,
                        MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual,
                        typeof (void),
                        new Type[] {pi.PropertyType});

                    var ilGenerator = methodBuilder.GetILGenerator();
                    ilGenerator.Emit(OpCodes.Ldarg_0);      // Load "this"
                    ilGenerator.Emit(OpCodes.Ldarg_1);      // Load the set value
                    ilGenerator.Emit(OpCodes.Stfld, field); // Set the field to the set value
                    ilGenerator.Emit(OpCodes.Ret);

                    // Override the set method from the original type to this IL-generated version
                    typeBuilder.DefineMethodOverride(methodBuilder, setMethod);
                }
            }

            return typeBuilder;
        }

        private static TypeBuilder CreateInterfaceType(string name, IEnumerable<PropertyInfo> properties, bool copyPropertyAttributes = true)
        {
            TypeBuilder typeBuilder = ModuleBuilder.DefineType(name,
                TypeAttributes.Public |
                TypeAttributes.Abstract |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.Interface);

            foreach (var property in properties)
            {
                var newProperty = typeBuilder.DefineProperty(BACKING_PROPERTY_PREFIX + property.Name,
                    PropertyAttributes.None, typeof (byte[]), null);
                var newPropertyGet = typeBuilder.DefineMethod("get_" + newProperty.Name,
                    MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.Public |
                    MethodAttributes.SpecialName, typeof (byte[]), Type.EmptyTypes);
                var newPropertySet = typeBuilder.DefineMethod("set_" + newProperty.Name,
                    MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.Public |
                    MethodAttributes.SpecialName, null, new Type[] {typeof (byte[])});

                if (copyPropertyAttributes)
                    CopyProperties(property, newProperty);
                newProperty.SetGetMethod(newPropertyGet);
                newProperty.SetSetMethod(newPropertySet);


                if (!property.PropertyType.IsValueType && property.PropertyType != typeof(string))
                {
                    var cacheProperty = typeBuilder.DefineProperty(CACHE_PROPERTY_PREFIX + property.Name,
                        PropertyAttributes.None, typeof (object), null);
                    var newCachePropertyGet = typeBuilder.DefineMethod("get_" + cacheProperty.Name,
                        MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.Public |
                        MethodAttributes.SpecialName, typeof (object), Type.EmptyTypes);
                    var newCachePropertySet = typeBuilder.DefineMethod("set_" + cacheProperty.Name,
                        MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.Public |
                        MethodAttributes.SpecialName, null, new Type[] {typeof (object)});

                    if (copyPropertyAttributes)
                        CopyProperties(property, cacheProperty);
                    cacheProperty.SetGetMethod(newCachePropertyGet);
                    cacheProperty.SetSetMethod(newCachePropertySet);
                }
            }

            typeBuilder.CreateType();

            return typeBuilder;
        }
        #endregion

        private static void CopyProperties(MemberInfo type, object builder)
        {
            foreach (var attribute in type.GetCustomAttributes<Attribute>().ToArray())
            {
                if (attribute is SerializerRedirectAttribute)
                {
                    var attribConstructor = ((SerializerRedirectAttribute) attribute).Type.GetConstructor(Type.EmptyTypes);
                    var attribBuilder = new CustomAttributeBuilder(attribConstructor, new object[0] {});

                    if (builder is TypeBuilder) ((TypeBuilder) builder).SetCustomAttribute(attribBuilder);
                    else if (builder is PropertyBuilder) ((PropertyBuilder) builder).SetCustomAttribute(attribBuilder);
                }
            }
        }
    }
}

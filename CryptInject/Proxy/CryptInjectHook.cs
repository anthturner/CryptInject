using System;
using System.Reflection;
using System.Runtime.Serialization;
using Castle.DynamicProxy;

namespace CryptInject.Proxy
{
    [DataContract]
    [Serializable]
    internal class CryptInjectHook : AllMethodsHook
    {
        public override bool ShouldInterceptMethod(Type type, MethodInfo methodInfo)
        {
            if (!methodInfo.Name.StartsWith("get_") && !methodInfo.Name.StartsWith("set_"))
                return false;

            if (methodInfo.Name.Substring(4).StartsWith(DataStorageMixinFactory.BACKING_PROPERTY_PREFIX))
                return false;

            return true;
        }
    }
}

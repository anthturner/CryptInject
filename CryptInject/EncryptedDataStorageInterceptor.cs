using System;
using Castle.DynamicProxy;

namespace CryptInject
{
    [Serializable]
    internal sealed class EncryptedDataStorageInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            if (invocation.Method.Name.StartsWith("set_") || invocation.Method.Name.StartsWith("get_"))
            {
                var propertyName = invocation.Method.Name.Substring(4); // remove get_ or set_
                var proxiedType = EncryptionManager.GetProxiedType(invocation.TargetType);

                if (proxiedType != null && proxiedType.IsManagingProperty(propertyName))
                {
                    if (invocation.Method.Name.Replace(propertyName, "") == "get_")
                    {
                        invocation.ReturnValue = proxiedType.GetValue(invocation.Proxy, propertyName);
                        return;
                    }
                    else
                    {
                        proxiedType.SetValue(invocation.Proxy, propertyName, invocation.Arguments[0]);
                        return;
                    }
                }
            }

            invocation.Proceed();
        }
    }
}

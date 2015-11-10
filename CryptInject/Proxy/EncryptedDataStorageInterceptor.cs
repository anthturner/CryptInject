using System;
using Castle.DynamicProxy;

namespace CryptInject.Proxy
{
    [Serializable]
    internal sealed class EncryptedDataStorageInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            if (invocation.Method.Name.StartsWith("set_") || invocation.Method.Name.StartsWith("get_"))
            {
                var propertyName = invocation.Method.Name.Substring(4); // remove get_ or set_
                var proxiedType = EncryptedInstanceFactory.GetTrackedInstance(invocation.InvocationTarget);

                if (proxiedType != null && proxiedType.EncryptedType.IsManagingProperty(propertyName))
                {
                    if (invocation.Method.Name.Replace(propertyName, "") == "get_")
                    {
                        invocation.ReturnValue = proxiedType.GetValue(propertyName);
                        return;
                    }
                    else
                    {
                        proxiedType.SetValue(propertyName, invocation.Arguments[0]);
                        if (invocation.Arguments[0] is string)
                        {
                            // todo: look in config to be able to turn on/off immutable string security workaround
                            unsafe
                            {
                                fixed (char* chars = ((string)invocation.Arguments[0]))
                                {
                                    for (int i = 0; i < ((string) invocation.Arguments[0]).Length; i++)
                                        chars[i] = 'x';
                                }
                            }
                        }
                        invocation.Arguments[0] = null;

                        return;
                    }
                }
            }

            invocation.Proceed();
        }
    }
}

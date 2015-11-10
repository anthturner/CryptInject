using System;

namespace CryptInject
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class SerializerRedirectAttribute : Attribute
    {
        internal Type Type { get; private set; }

        public SerializerRedirectAttribute(Type type)
        {
            Type = type;
        }
    }
}

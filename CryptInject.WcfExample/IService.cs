using System.ServiceModel;

namespace CryptInject.WcfExample
{
    [ServiceKnownType("GetKnownTypes", typeof(KnownTypesProvider))]
    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        void SetValue(int idx, Patient value);

        [OperationContract]
        Patient GetValue(int idx);
    }
}

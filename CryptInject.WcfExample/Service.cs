using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace CryptInject.WcfExample
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class Service : IService
    {
        private Dictionary<int, Patient> StoredPatients { get; set; }

        public Service()
        {
            StoredPatients = new Dictionary<int, Patient>();
        }

        public void SetValue(int idx, Patient value)
        {
            StoredPatients[idx] = value;
        }

        public string ServerGetName(int idx)
        {
            Console.WriteLine("Client requested patient's name from index {0}: '{1} {2}'", idx, StoredPatients[idx].FirstName, StoredPatients[idx].LastName);
            return $"'{StoredPatients[idx].FirstName} {StoredPatients[idx].LastName}'";
        }

        public Patient GetValue(int idx)
        {
            Console.WriteLine("Client requested entire patient record from index {0}", idx);
            if (!StoredPatients.ContainsKey(idx))
                return null;
            return StoredPatients[idx];
        }
    }
}

using System;
using System.Runtime.Serialization;

namespace CryptInject.WcfExample
{
    [DataContract]
    [SerializerRedirect(typeof (DataContractAttribute))]
    public class Patient
    {
        [DataMember]
        public Guid PatientId { get; set; }
        
        [Encryptable("Semi-Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual string FirstName { get; set; }

        [Encryptable("Semi-Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual string LastName { get; set; }

        [Encryptable("Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual string SSN { get; set; }

        [Encryptable("Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual DateTime DOB { get; set; }

        [Encryptable("Non-Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual double Glucose { get; set; }

        [Encryptable("Non-Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual double CPeptide { get; set; }

        [Encryptable("Non-Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual double ALT { get; set; }

        [Encryptable("Non-Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual double AST { get; set; }

        [Encryptable("Non-Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual double BMI { get; set; }

        [Encryptable("Non-Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual double HDL { get; set; }

        [DataMember]
        public DateTime Collected { get; set; }

        public Patient()
        {
            PatientId = Guid.NewGuid();
            Collected = DateTime.Now;
        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext c)
        {
            // The call below is necessary so that when WCF materializes the new object, the proxy is linked back into the entity
            // Note: WCF uses FormatterServices.GetUninitializedObject() to instantiate the classes, which *skips the constructor*
            this.Relink();
        }
    }
}

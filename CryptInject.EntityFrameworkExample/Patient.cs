using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace CryptInject.EntityFrameworkExample
{
    [Table("Patients")]
    [DataContract]
    [SerializerRedirect(typeof (DataContractAttribute))]
    public class Patient
    {
        [DataMember]
        [Key]
        [Required]
        public Guid PatientId { get; set; }
        
        [NotMapped]
        [Encryptable("Semi-Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual string FirstName { get; set; }

        [NotMapped]
        [Encryptable("Semi-Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual string LastName { get; set; }

        [NotMapped]
        [Encryptable("Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual string SSN { get; set; }

        [NotMapped]
        [Encryptable("Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual DateTime DOB { get; set; }

        [NotMapped]
        [Encryptable("Non-Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual double Glucose { get; set; }

        [NotMapped]
        [Encryptable("Non-Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual double CPeptide { get; set; }

        [NotMapped]
        [Encryptable("Non-Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual double ALT { get; set; }

        [NotMapped]
        [Encryptable("Non-Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual double AST { get; set; }

        [NotMapped]
        [Encryptable("Non-Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual double BMI { get; set; }

        [NotMapped]
        [Encryptable("Non-Sensitive Information")]
        [SerializerRedirect(typeof(DataMemberAttribute))]
        public virtual double HDL { get; set; }

        [DataMember]
        public DateTime Collected { get; set; }

        public Patient()
        {
            // The call below is necessary so that when Entity Framework materializes the new object, the proxy is linked back into the entity
            this.Relink();

            PatientId = Guid.NewGuid();
            Collected = DateTime.Now;
        }
    }
}

using System;

namespace CryptInject.NHibernateExample
{
    public class Patient
    {
        public virtual Guid PatientId { get; set; }

        [Encryptable("Semi-Sensitive Information")]
        public virtual string FirstName { get; set; }

        [Encryptable("Semi-Sensitive Information")]
        public virtual string LastName { get; set; }

        [Encryptable("Sensitive Information")]
        public virtual string SSN { get; set; }

        [Encryptable("Sensitive Information")]
        public virtual DateTime DOB { get; set; }

        [Encryptable("Non-Sensitive Information")]
        public virtual double Glucose { get; set; }

        [Encryptable("Non-Sensitive Information")]
        public virtual double CPeptide { get; set; }

        [Encryptable("Non-Sensitive Information")]
        public virtual double ALT { get; set; }

        [Encryptable("Non-Sensitive Information")]
        public virtual double AST { get; set; }

        [Encryptable("Non-Sensitive Information")]
        public virtual double BMI { get; set; }

        [Encryptable("Non-Sensitive Information")]
        public virtual double HDL { get; set; }

        public virtual DateTime Collected { get; set; }
    }
}

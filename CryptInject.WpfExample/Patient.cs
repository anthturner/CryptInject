using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CryptInject.WpfExample
{
    [Serializable]
    [SerializerRedirect(typeof (SerializableAttribute))]
    public class Patient : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private DateTime _collected;

        [Encryptable("Semi-Sensitive Information")]
        public virtual string FirstNameStored { get; set; }
        public string FirstName
        {
            get { return FirstNameStored; }
            set
            {
                FirstNameStored = value;
                OnPropertyChanged();
            }
        }

        [Encryptable("Semi-Sensitive Information")]
        public virtual string LastNameStored { get; set; }
        public string LastName
        {
            get { return LastNameStored; }
            set
            {
                LastNameStored = value;
                OnPropertyChanged();
            }
        }

        [Encryptable("Sensitive Information")]
        public virtual string SSNStored { get; set; }
        public string SSN
        {
            get { return SSNStored; }
            set
            {
                SSNStored = value;
                OnPropertyChanged();
            }
        }

        [Encryptable("Sensitive Information")]
        public virtual DateTime DOBStored { get; set; }
        public DateTime DOB
        {
            get { return DOBStored; }
            set
            {
                DOBStored = value;
                OnPropertyChanged();
            }
        }



        [Encryptable("Non-Sensitive Information")]
        public virtual double GlucoseStored { get; set; }
        public double Glucose
        {
            get { return GlucoseStored; }
            set
            {
                GlucoseStored = value;
                OnPropertyChanged();
            }
        }

        [Encryptable("Non-Sensitive Information")]
        public virtual double CPeptideStored { get; set; }
        public double CPeptide
        {
            get { return CPeptideStored; }
            set
            {
                CPeptideStored = value;
                OnPropertyChanged();
            }
        }

        [Encryptable("Non-Sensitive Information")]
        public virtual double ALTStored { get; set; }
        public double ALT
        {
            get { return ALTStored; }
            set
            {
                ALTStored = value;
                OnPropertyChanged();
            }
        }

        [Encryptable("Non-Sensitive Information")]
        public virtual double ASTStored { get; set; }
        public double AST
        {
            get { return ASTStored; }
            set
            {
                ASTStored = value;
                OnPropertyChanged();
            }
        }

        [Encryptable("Non-Sensitive Information")]
        public virtual double BMIStored { get; set; }
        public double BMI
        {
            get { return BMIStored; }
            set
            {
                BMIStored = value;
                OnPropertyChanged();
            }
        }

        [Encryptable("Non-Sensitive Information")]
        public virtual double HDLStored { get; set; }
        public double HDL
        {
            get { return HDLStored; }
            set
            {
                HDLStored = value;
                OnPropertyChanged();
            }
        }

        public DateTime Collected { get { return _collected; }
            set
            {
                _collected = value;
                OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

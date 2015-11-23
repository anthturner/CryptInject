using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace CryptInject.WpfExample
{
    [JsonObject]
    [Serializable]
    [SerializerRedirect(typeof(SerializableAttribute))]
    public class Patient : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private DateTime _collected;

        public string FirstNameStored { get; set; }
        [JsonIgnore]
        public string FirstName
        {
            get { return FirstNameStored; }
            set
            {
                FirstNameStored = value;
                OnPropertyChanged();
            }
        }

        public string LastNameStored { get; set; }
        [JsonIgnore]
        public string LastName
        {
            get { return LastNameStored; }
            set
            {
                LastNameStored = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        [Encryptable("Office")]
        [SerializerRedirect(typeof(JsonPropertyAttribute))]
        public virtual string SSNStored { get; set; }
        [JsonIgnore]
        public string SSN
        {
            get { return SSNStored; }
            set
            {
                SSNStored = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        [Encryptable("Office")]
        [SerializerRedirect(typeof(JsonPropertyAttribute))]
        public virtual DateTime DOBStored { get; set; }
        [JsonIgnore]
        public DateTime DOB
        {
            get { return DOBStored; }
            set
            {
                DOBStored = value;
                OnPropertyChanged();
            }
        }
        
        [JsonIgnore]
        [Encryptable("Restricted")]
        [SerializerRedirect(typeof(JsonPropertyAttribute))]
        public virtual double WeightStored { get; set; }
        [JsonIgnore]
        public double Weight
        {
            get { return WeightStored; }
            set
            {
                WeightStored = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        [Encryptable("Restricted")]
        [SerializerRedirect(typeof(JsonPropertyAttribute))]
        public virtual string LastBloodPressureStored { get; set; }
        [JsonIgnore]
        public string LastBloodPressure
        {
            get { return LastBloodPressureStored; }
            set
            {
                LastBloodPressureStored = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        [Encryptable("Doctor Only")]
        [SerializerRedirect(typeof(JsonPropertyAttribute))]
        public virtual double ALTStored { get; set; }
        [JsonIgnore]
        public double ALT
        {
            get { return ALTStored; }
            set
            {
                ALTStored = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        [Encryptable("Doctor Only")]
        [SerializerRedirect(typeof(JsonPropertyAttribute))]
        public virtual double ASTStored { get; set; }
        [JsonIgnore]
        public double AST
        {
            get { return ASTStored; }
            set
            {
                ASTStored = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        [Encryptable("Doctor Only")]
        [SerializerRedirect(typeof(JsonPropertyAttribute))]
        public virtual double BMIStored { get; set; }
        [JsonIgnore]
        public double BMI
        {
            get { return BMIStored; }
            set
            {
                BMIStored = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        [Encryptable("Doctor Only")]
        [SerializerRedirect(typeof(JsonPropertyAttribute))]
        public virtual double HDLStored { get; set; }
        [JsonIgnore]
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

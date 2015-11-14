using System;
using System.Collections.Generic;
using System.Linq;
using CryptInject.Keys;
using CryptInject.Keys.Builtin;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;

namespace CryptInject.NHibernateExample
{
    class Program
    {
        static void Main(string[] args)
        {
            Keyring.GlobalKeyring.Add("Sensitive Information", AesEncryptionKey.Create(TripleDesEncryptionKey.Create()));
            Keyring.GlobalKeyring.Add("Semi-Sensitive Information", AesEncryptionKey.Create());
            Keyring.GlobalKeyring.Add("Non-Sensitive Information", TripleDesEncryptionKey.Create());

            var examplePatients = GetExamplePatients();

            using (var session = GetSession())
            {
                List<Patient> patients = new List<Patient>();
                patients.AddRange(session.Query<Patient>().ToList());

                if (patients.Count == 0)
                {
                    Keyring.GlobalKeyring.Lock();

                    var transaction = session.BeginTransaction();
                    foreach (var patient in examplePatients)
                    {
                        session.Save(patient);
                    }
                    transaction.Commit();

                    patients = session.Query<Patient>().ToList();
                }

                Console.WriteLine("WHILE KEYRING IS LOCKED:");
                foreach (var patient in patients)
                {
                    Console.WriteLine("{0} {1} - SSN#{2}", patient.FirstName, patient.LastName, patient.SSN);
                }

                Console.WriteLine("WHILE KEYRING IS UNLOCKED:");
                Keyring.GlobalKeyring.Unlock();
                foreach (var patient in patients)
                {
                    Console.WriteLine("{0} {1} - SSN#{2}", patient.FirstName, patient.LastName, patient.SSN);
                }

                Console.ReadKey();
            }
        }

        private static List<Patient> GetExamplePatients()
        {
            var patients = new List<Patient>();
            foreach (var dataRow in SampleDataCsv.Split('\n'))
            {
                var record = dataRow.Split(',');

                var newPatient = new Patient().AsEncrypted();
                newPatient.FirstName = record[0];
                newPatient.LastName = record[1];
                newPatient.ALT = double.Parse(record[2]);
                newPatient.AST = double.Parse(record[3]);
                newPatient.BMI = double.Parse(record[4]);
                newPatient.CPeptide = double.Parse(record[5]);
                newPatient.Glucose = double.Parse(record[6]);
                newPatient.HDL = double.Parse(record[7]);
                newPatient.SSN = record[8];
                newPatient.DOB = DateTime.Parse(record[9]);
                newPatient.Collected = DateTime.Parse(record[10]);
                patients.Add(newPatient);
            }
            return patients;
        }

        private static ISession GetSession()
        {
            var configuration = new Configuration();
            configuration.Configure("hibernate.cfg.xml");
            configuration.AddFile("Mappings\\Patient.hbm.xml");
            ISessionFactory sessionFactory = configuration.BuildSessionFactory();
            return sessionFactory.OpenSession();
        }

        private static string SampleDataCsv =
            @"Jimmy,Chavez,1.2,3.2,36.8,4.7,20.9,95,700-09-9789,4/16/1952,8/10/2007
Ashley,Ford,1.9,8.0,41.2,2.5,19.9,35,276-84-3302,4/19/1954,1/25/2007
Gregory,Ramirez,6.3,9.0,13.5,1.4,16.9,83,217-85-3620,8/9/1991,10/16/2012
Lori,Watkins,7.0,9.2,39.0,5.5,16.7,44,159-40-2237,7/3/1977,6/25/2010
Carlos,Morrison,9.8,6.6,32.4,5.6,22.8,109,230-97-3389,7/14/1971,1/27/2014";
    }
}

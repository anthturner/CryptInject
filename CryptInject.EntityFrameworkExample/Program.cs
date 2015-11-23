using System;
using System.IO;
using CryptInject.Keys;
using CryptInject.Keys.Builtin;

namespace CryptInject.EntityFrameworkExample
{
    class Program
    {
        private static void Main(string[] args)
        {
            File.Delete("keyring.dat");
            if (!File.Exists("keyring.dat"))
            {
                DataGeneration();
            }
            DataReading();
            Console.ReadKey();
        }

        private static void DataGeneration()
        {
            Keyring.GlobalKeyring.Add("Sensitive Information", AesEncryptionKey.Create(TripleDesEncryptionKey.Create()));
            Keyring.GlobalKeyring.Add("Semi-Sensitive Information", AesEncryptionKey.Create());
            Keyring.GlobalKeyring.Add("Non-Sensitive Information", TripleDesEncryptionKey.Create());

            using (var db = new DatabaseContext())
            {
                db.Patients.RemoveRange(db.Patients);
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
                    db.Patients.Attach(newPatient);

                    db.Patients.Add(newPatient);
                }
                db.SaveChanges();

                using (var keyringStream = new FileStream("keyring.dat", FileMode.OpenOrCreate))
                {
                    Keyring.GlobalKeyring.ExportToStream(keyringStream);
                }
            }
        }

        private static void DataReading()
        {
            using (var keyringStream = new FileStream("keyring.dat", FileMode.Open))
            {
                Keyring.GlobalKeyring.ImportFromStream(keyringStream);
            }
            Keyring.GlobalKeyring.Lock();
            using (var db = new DatabaseContext())
            {
                Console.WriteLine("WHILE KEYRING IS LOCKED:");
                foreach (var patient in db.Patients)
                {
                    Console.WriteLine("{0} {1} - SSN#{2}", patient.FirstName, patient.LastName, patient.SSN);
                }

                Console.WriteLine("WHILE KEYRING IS UNLOCKED:");
                Keyring.GlobalKeyring.Unlock();
                foreach (var patient in db.Patients)
                {
                    Console.WriteLine("{0} {1} - SSN#{2}", patient.FirstName, patient.LastName, patient.SSN);
                }
            }
        }

        private static string SampleDataCsv =
            @"Jimmy,Chavez,1.2,3.2,36.8,4.7,20.9,95,700-09-9789,4/16/1952,8/10/2007
Ashley,Ford,1.9,8.0,41.2,2.5,19.9,35,276-84-3302,4/19/1954,1/25/2007
Gregory,Ramirez,6.3,9.0,13.5,1.4,16.9,83,217-85-3620,8/9/1991,10/16/2012
Lori,Watkins,7.0,9.2,39.0,5.5,16.7,44,159-40-2237,7/3/1977,6/25/2010
Carlos,Morrison,9.8,6.6,32.4,5.6,22.8,109,230-97-3389,7/14/1971,1/27/2014
Raymond,Romero,5.6,3.3,22.6,6.4,17.8,95,590-48-6371,5/20/1959,9/24/2006
Edward,Howell,7.5,7.3,45.8,1.1,9.7,42,858-85-6622,8/28/1966,7/31/2015
James,Lane,6.0,4.5,28.0,3.0,3.0,106,930-47-3460,12/11/1966,9/29/2011
Eric,Boyd,3.1,1.2,46.2,3.1,15.6,79,222-96-5623,8/13/1992,7/2/2009
Louis,Gomez,6.9,3.1,26.8,5.3,20.4,51,680-14-5172,3/21/1955,7/9/2011
David,Mcdonald,6.5,8.7,36.7,3.1,19.0,89,242-89-6688,4/22/1977,10/12/2014
Sharon,Arnold,6.8,8.0,40.7,1.3,17.6,95,200-50-8231,8/13/1985,2/22/2009
Justin,Crawford,1.9,5.1,25.3,2.0,17.5,46,622-01-7252,7/31/1999,7/22/2015
Christina,Flores,5.8,3.8,26.9,4.8,13.4,77,820-09-5696,9/6/1951,12/3/2005
Sharon,Vasquez,5.0,1.2,49.4,5.6,7.3,26,152-44-3542,10/23/1967,1/1/2007
Larry,Elliott,9.4,2.7,43.6,9.5,13.9,81,614-59-8615,8/31/1951,4/13/2011
Edward,Wagner,7.6,1.9,41.0,7.9,9.5,43,966-15-4172,11/23/1979,8/18/2006
Lois,Stephens,4.1,6.1,14.7,8.2,20.7,41,697-44-8685,3/6/1958,5/31/2007
Keith,Warren,1.4,5.1,40.6,4.5,3.0,15,168-24-3445,5/28/1969,2/2/2006
Kevin,Miller,2.6,5.4,36.6,6.3,24.3,104,355-93-6419,5/3/1981,5/16/2013";
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CryptInject.Keys;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace CryptInject.WpfExample
{
    /// <summary>
    /// Interaction logic for RecordList.xaml
    /// </summary>
    public partial class RecordList : MetroWindow
    {
        public ObservableCollection<Patient> Patients { get; protected set; }

        private bool _isLoggedIn = false;

        public RecordList()
        {
            InitializeComponent();
            DataContext = this;
            patientList.ItemsSource = Patients;
            
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            SetLoggedOut();
            Keyring.GlobalKeyring.KeyringChanged += () => patientList.Dispatcher.Invoke(() =>
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(patientList.ItemsSource);
                if (view != null)
                {
                    view.Refresh();
                }
            });

            Patients = new ObservableCollection<Patient>();
            if (File.Exists("data.dat"))
            {
                File.Delete("data.dat");
                try
                {
                    using (var fs = new FileStream("data.dat", FileMode.Open))
                    {
                        if (fs.Length == 0)
                            return;
                        var patientsList = (List<Patient>)JsonConvert.DeserializeObject(new StreamReader(fs).ReadToEnd(),
                                    typeof(List<Patient>).GetEncryptedType(),
                                    new JsonSerializerSettings()
                                    {
                                        TypeNameHandling = TypeNameHandling.Auto
                                    });
                        foreach (var p in patientsList)
                        {
                            Patients.Add(p);
                        }
                    }
                }
                catch (Exception ex)
                {
                    GenerateData();
                }
            }
            else
            {
                GenerateData();
            }
            patientList.ItemsSource = Patients;

            Keyring.GlobalKeyring.Unlock();
        }

        private void GenerateData()
        {
            ImportFromFile("jthomas.keyring");
            ImportFromFile("lmcdonald.keyring");
            Patients.Clear();

            foreach (var dataRow in SampleDataCsv.Split('\n'))
            {
                var record = dataRow.Split(',');

                var newPatient = new Patient().AsEncrypted();
                newPatient.FirstName = record[0];
                newPatient.LastName = record[1];
                newPatient.ALT = double.Parse(record[2]);
                newPatient.AST = double.Parse(record[3]);
                newPatient.BMI = double.Parse(record[4]);
                newPatient.Weight = double.Parse(record[5]);
                newPatient.LastBloodPressure = record[6];
                newPatient.HDL = double.Parse(record[7]);
                newPatient.SSN = record[8];
                newPatient.DOB = DateTime.Parse(record[9]);
                newPatient.Collected = DateTime.Parse(record[10]);
                Patients.Add(newPatient);
            }

            using (var fs = new FileStream("data.dat", FileMode.Create))
            {
                Keyring.GlobalKeyring.Lock();
                var jsonStr = JsonConvert.SerializeObject(Patients.ToList(), new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
                using (var sw = new StreamWriter(fs))
                {
                    sw.Write(jsonStr);
                }
                Keyring.GlobalKeyring.Unlock();
            }
            Keyring.GlobalKeyring.Clear();
        }

        private void ImportFromFile(string file)
        {
            using (var fs = new FileStream(file, FileMode.Open))
            {
                var keyring = new Keyring();
                keyring.ImportFromStream(fs);
                Keyring.GlobalKeyring.Import(keyring);
            }
        }

        private string SampleDataCsv =
            @"Jimmy,Chavez,1.2,3.2,36.8,4.7,180/120,95,700-09-9789,4/16/1952,8/10/2007
Ashley,Ford,1.9,8.0,41.2,250,180/100,35,276-84-3302,4/19/1954,1/25/2007
Gregory,Ramirez,6.3,9.0,13.5,140,140/90,83,217-85-3620,8/9/1991,10/16/2012
Lori,Watkins,7.0,9.2,39.0,155,145/86,44,159-40-2237,7/3/1977,6/25/2010
Carlos,Morrison,9.8,6.6,32.4,256,160/100,109,230-97-3389,7/14/1971,1/27/2014
Raymond,Romero,5.6,3.3,22.6,164,135/80,95,590-48-6371,5/20/1959,9/24/2006
Edward,Howell,7.5,7.3,45.8,211,150/100,42,858-85-6622,8/28/1966,7/31/2015
James,Lane,6.0,4.5,28.0,300,150/100,106,930-47-3460,12/11/1966,9/29/2011
Eric,Boyd,3.1,1.2,46.2,331,160/100,79,222-96-5623,8/13/1992,7/2/2009
Louis,Gomez,6.9,3.1,26.8,153,135/90,51,680-14-5172,3/21/1955,7/9/2011
David,Mcdonald,6.5,8.7,36.7,231,145/86,89,242-89-6688,4/22/1977,10/12/2014
Sharon,Arnold,6.8,8.0,40.7,130,160/100,95,200-50-8231,8/13/1985,2/22/2009
Justin,Crawford,1.9,5.1,25.3,220,140/90,46,622-01-7252,7/31/1999,7/22/2015
Christina,Flores,5.8,3.8,26.9,248,145/86,77,820-09-5696,9/6/1951,12/3/2005
Sharon,Vasquez,5.0,1.2,49.4,258,135/80,26,152-44-3542,10/23/1967,1/1/2007
Larry,Elliott,9.4,2.7,43.6,195,140/90,81,614-59-8615,8/31/1951,4/13/2011
Edward,Wagner,7.6,1.9,41.0,279,180/100,43,966-15-4172,11/23/1979,8/18/2006
Lois,Stephens,4.1,6.1,14.7,182,145/86,41,697-44-8685,3/6/1958,5/31/2007
Keith,Warren,1.4,5.1,40.6,145,145/86,15,168-24-3445,5/28/1969,2/2/2006
Kevin,Miller,2.6,5.4,36.6,163,180/100,104,355-93-6419,5/3/1981,5/16/2013";

        private async void LoginLogout_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoggedIn)
            {
                // logout
                Keyring.GlobalKeyring.Clear();
                SetLoggedOut();
            }
            else
            {
                SetLoggedOut();
                while (true)
                {
                    var loginDlg = await this.ShowLoginAsync("HealthPro+ Login", "Enter your credentials to access this secure system.");
                    if (loginDlg == null)
                    {
                        break;
                    }
                    else if (!File.Exists(loginDlg.Username + ".keyring"))
                    {
                        await this.ShowMessageAsync("HealthPro+ Login", "Invalid user name or password.");
                    }
                    else
                    {
                        ImportFromFile(loginDlg.Username + ".keyring");
                        
                        switch (loginDlg.Username)
                        {
                            case "jthomas":
                                SetLoggedIn("maleAvatar.png");
                                break;
                            case "lmcdonald":
                                SetLoggedIn("femaleAvatar.png");
                                break;
                        }
                        break;
                    }
                }
            }
        }

        private void SetLoggedIn(string avatar)
        {
            LoginButton.Content = "Logout";
            avatarRect.Fill = new ImageBrush(GenerateImageSource(avatar));
            loadedKeys.Text = string.Join(Environment.NewLine, Keyring.GlobalKeyring.KeysProvided);
            _isLoggedIn = true;
        }

        private void SetLoggedOut()
        {
            LoginButton.Content = "Login";
            avatarRect.Fill = new ImageBrush(GenerateImageSource("blankAvatar.png"));
            loadedKeys.Text = "Not logged in";
            _isLoggedIn = false;
        }

        static internal ImageSource GenerateImageSource(string resourceName)
        {
            Uri oUri = new Uri("pack://application:,,,/" + Assembly.GetAssembly(typeof(App)).GetName().Name + ";component/Images/" + resourceName, UriKind.RelativeOrAbsolute);
            return BitmapFrame.Create(oUri);
        }
    }
}

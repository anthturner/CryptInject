using System.IO;
using System.Linq;
using System.Windows;
using CryptInject.Keys;
using CryptInject.Keys.Builtin;

namespace CryptInject.WpfExample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /* Scenario:
         * Health Information System application with 3 roles and 2 users.
         * 
         * Roles (Business Cases):
         * - "Doctor Only" -> Only Dr. Linda McDonald can see this information
         * - "Restricted" -> Only the doctor and nurse(s) can see this information
         * - "Office" -> Office staff can see this information
         * 
         * It is assumed that patient names are not sensitive information.
         */

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Any() && e.Args[0] == "/generate")
            {
                var sensitiveKey = AesEncryptionKey.Create(TripleDesEncryptionKey.Create());
                var somewhatSensitiveKey = TripleDesEncryptionKey.Create();
                var nonSensitiveKey = TripleDesEncryptionKey.Create();

                var keyring = new Keyring();
                keyring.Add("Doctor Only", sensitiveKey);
                keyring.Add("Restricted", somewhatSensitiveKey);
                keyring.Add("Office", nonSensitiveKey);

                // John's Keyring
                using (var johnFs = new FileStream("jthomas.keyring", FileMode.Create))
                {
                    keyring.ExportToStream(johnFs, "Restricted", "Office");
                }

                // Linda's Keyring
                using (var lindaFs = new FileStream("lmcdonald.keyring", FileMode.Create))
                {
                    keyring.ExportToStream(lindaFs);
                }
            }
            else
            {
                new RecordList().ShowDialog();
            }
            this.Shutdown();
        }
    }
}

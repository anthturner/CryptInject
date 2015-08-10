using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using CryptInject.Keys;
using CryptInject.Keys.Builtin;
using Microsoft.Win32;

namespace CryptInject.WpfExample
{
    /// <summary>
    /// Interaction logic for KeyringSelection.xaml
    /// </summary>
    public partial class KeyringSelection : Window
    {
        public KeyringSelection()
        {
            InitializeComponent();
            Loaded += (sender, args) =>
            {
                DrawTree();
            };
        }

        private void Import_Partial_Keyring_Click(object sender, RoutedEventArgs e)
        {
            var openFile = new OpenFileDialog();
            openFile.DefaultExt = ".keyring";
            openFile.ValidateNames = true;

            if (openFile.ShowDialog().HasValue && !string.IsNullOrEmpty(openFile.FileName))
            {
                ImportFromFile(openFile.FileName);
                DrawTree();
            }
        }

        private void Generate_Sample_Keyring_Click(object sender, RoutedEventArgs e)
        {
            var sensitiveKey = AesEncryptionKey.Create(TripleDesEncryptionKey.Create());
            var somewhatSensitiveKey = TripleDesEncryptionKey.Create();
            var nonSensitiveKey = TripleDesEncryptionKey.Create();

            var keyring = new Keyring();
            keyring.Add("Sensitive Information", sensitiveKey);
            keyring.Add("Semi-Sensitive Information", somewhatSensitiveKey);
            keyring.Add("Non-Sensitive Information", nonSensitiveKey);

            // John's Keyring
            using (var johnFs = new FileStream("John.keyring", FileMode.Create))
            {
                keyring.ExportToStream(johnFs, "Non-Sensitive Information", "Semi-Sensitive Information");
            }

            // Jane's Keyring
            using (var janeFs = new FileStream("Jane.keyring", FileMode.Create))
            {
                keyring.ExportToStream(janeFs);
            }

            MessageBox.Show("Sample keyrings generated as 'John.keyring' and 'Jane.keyring'. They have not been pre-loaded.");

            DrawTree();
        }

        private void Clear_Keyring_Click(object sender, RoutedEventArgs e)
        {
            EncryptionManager.Keyring.Clear();
            DrawTree();
        }

        private void ImportFromFile(string file)
        {
            using (var fs = new FileStream(file, FileMode.Open))
            {
                var keyring = new Keyring();
                keyring.ImportFromStream(fs);
                EncryptionManager.Keyring.Import(keyring);
            }
        }

        private void DrawTree()
        {
            keyringTree.Items.Clear();
            foreach (var key in EncryptionManager.Keyring)
            {
                var newKey = CreateTreeKey(key.KeyData, key.Name);
                keyringTree.Items.Add(newKey);
            }
        }

        private TreeViewItem CreateTreeKey(EncryptionKey key, string name = null)
        {
            var tvi = new TreeViewItem();

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(name))
                sb.AppendFormat("[{0}] ", name);

            if (key is AesEncryptionKey)
                sb.Append("AES-256");
            else if (key is HmacEncryptionKey)
                sb.Append("HMAC Signed");
            else if (key is TripleDesEncryptionKey)
                sb.AppendFormat("3DES ({0})", (CipherMode)((TripleDesEncryptionKey)key).CipherMode);

            if (key.ChainedInnerKey != null)
                tvi.Items.Add(CreateTreeKey(key.ChainedInnerKey));

            tvi.Header = sb.ToString();
            return tvi;
        }
    }
}

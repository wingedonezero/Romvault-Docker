using System.Text;
using Avalonia.Controls;
using RomVaultCore.RvDB;

namespace ROMVault
{
    public partial class FrmRomInfo : Window
    {
        public FrmRomInfo()
        {
            InitializeComponent();
        }

        public bool SetRom(RvFile tFile)
        {
            if (tFile.FileGroup == null)
                return false;

            StringBuilder sb = new StringBuilder();

            foreach (var v in tFile.FileGroup.Files)
            {
                sb.AppendLine(v.GotStatus + " | " + v.FullName);
            }
            textBox1.Text = sb.ToString();
            return true;
        }
    }
}

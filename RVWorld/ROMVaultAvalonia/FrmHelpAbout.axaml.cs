using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ROMVault
{
    public partial class FrmHelpAbout : Window
    {
        public FrmHelpAbout()
        {
            InitializeComponent();
            Title = "Version " + Program.strVersion + " : " + AppContext.BaseDirectory;
            lblVersion.Text = "Version " + Program.strVersion;
        }

        private void label1_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("http://www.romvault.com/");
        }

        private void pictureBox2_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("http://paypal.me/romvault");
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }
    }
}

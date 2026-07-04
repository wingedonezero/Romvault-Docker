using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RomVaultCore;

namespace ROMVault
{
    public partial class FrmShowError : Window
    {
        public FrmShowError()
        {
            InitializeComponent();
            if (Settings.rvSettings.DoNotReportFeedback)
                label1.Text = "You have opted out of sending this Crash Report";
        }

        public void settype(string s)
        {
            textBox1.Text = s;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}

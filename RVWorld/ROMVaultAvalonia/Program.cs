using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DarkAvalonia;
using RomVaultCore;
using RomVaultCore.FixFile.FixAZipCore;
using RomVaultCore.Utils;

namespace ROMVault
{
    internal static class Program
    {
        private static readonly Version Version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(3, 7, 0);
        public static string strVersion;

        public static MainWindow frmMain;

        private static Mutex mutex;

        [STAThread]
        public static void Main(string[] args)
        {
            strVersion = $"{Version.Major}.{Version.Minor}.{Version.Build}";
            if (Version.Revision > 0)
                strVersion += $" WIP{Version.Revision}";

            string appName = AppContext.BaseDirectory;
            appName = appName.Replace("\\", "_");
            appName = appName.Replace("/", "_");
            appName = appName.Replace(":", "_");
            appName = appName.Replace(".", "_");

            mutex = new Mutex(true, appName, out bool createdNew);
            if (!createdNew)
            {
                Console.Error.WriteLine("You cannot run two copies of the same instance of RomVault.");
                return;
            }

            Settings.rvSettings = new Settings();
            Settings.rvSettings = Settings.SetDefaults(out string errorReadingSettings);

            ReportError.ErrorForm += ShowErrorForm;
            ReportError.Dialog += ShowDialog;

            dark.darkEnabled = Settings.rvSettings.Darkness;

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

            ReportError.Close();
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        public static void ShowErrorForm(string message)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                FrmShowError fshow = new FrmShowError();
                fshow.settype(message);
                fshow.ShowDialog(frmMain);
            });
        }

        public static void ShowDialog(string text, string caption)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var msgWindow = new Window
                {
                    Title = caption,
                    Width = 400,
                    Height = 150,
                    WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
                    Content = new Avalonia.Controls.StackPanel
                    {
                        Margin = new Avalonia.Thickness(16),
                        Children =
                        {
                            new Avalonia.Controls.TextBlock { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                            new Avalonia.Controls.Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Margin = new Avalonia.Thickness(0, 16, 0, 0) }
                        }
                    }
                };
                ((Avalonia.Controls.Button)((Avalonia.Controls.StackPanel)msgWindow.Content).Children[1]).Click += (s, e) => msgWindow.Close();
                if (frmMain != null)
                    msgWindow.ShowDialog(frmMain);
                else
                    msgWindow.Show();
            });
        }
    }
}

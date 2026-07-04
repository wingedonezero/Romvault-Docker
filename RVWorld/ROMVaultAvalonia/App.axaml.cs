using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DarkAvalonia;
using RomVaultCore;

namespace ROMVault
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                dark.SetTheme(this, Settings.rvSettings?.Darkness ?? false);

                var splash = new FrmSplashScreen();
                desktop.MainWindow = splash;
                splash.Show();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}

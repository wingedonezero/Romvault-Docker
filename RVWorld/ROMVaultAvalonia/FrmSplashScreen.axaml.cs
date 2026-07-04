using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using RomVaultCore;
using RomVaultCore.RvDB;
using RomVaultCore.FixFile.FixAZipCore;
using RomVaultCore.Utils;
using Avalonia.Controls.ApplicationLifetimes;

namespace ROMVault
{
    public partial class FrmSplashScreen : Window
    {
        private double _opacityIncrement = 0.05;
        private readonly ThreadWorker _thWrk;
        private readonly DispatcherTimer _timer;

        public FrmSplashScreen()
        {
            InitializeComponent();
            lblVersion.Text = $"Version {Program.strVersion} : {AppContext.BaseDirectory}";
            Opacity = 0;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _timer.Tick += Timer1Tick;

            _thWrk = new ThreadWorker(StartUpCode) { wReport = BgwProgressChanged, wFinal = BgwRunWorkerCompleted };

            Opened += FrmSplashScreenShown;
        }

        private void FrmSplashScreenShown(object sender, EventArgs e)
        {
            _thWrk.StartAsync();
            _timer.Start();
        }

        private static void StartUpCode(ThreadWorker thWrk)
        {
            RepairStatus.InitStatusCheck();
            DB.Read(thWrk);
        }

        private void BgwProgressChanged(object e)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => BgwProgressChanged(e));
                return;
            }

            if (e is int percent)
            {
                if (percent >= progressBar.Minimum && percent <= progressBar.Maximum)
                {
                    progressBar.Value = percent;
                }
                return;
            }

            if (e is bgwSetRange bgwSr)
            {
                progressBar.Minimum = 0;
                progressBar.Maximum = bgwSr.MaxVal;
                progressBar.Value = 0;
                return;
            }

            if (e is bgwText bgwT)
            {
                lblStatus.Text = bgwT.Text;
            }
        }

        private void BgwRunWorkerCompleted()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(BgwRunWorkerCompleted);
                return;
            }

            _opacityIncrement = -0.1;
            _timer.Start();
        }

        private void Timer1Tick(object sender, EventArgs e)
        {
            if (_opacityIncrement > 0)
            {
                if (Opacity < 1)
                {
                    Opacity += _opacityIncrement;
                }
                else
                {
                    _timer.Stop();
                }
            }
            else
            {
                if (Opacity > 0)
                {
                    Opacity += _opacityIncrement;
                }
                else
                {
                    _timer.Stop();

                    FindSourceFile.SetFixOrderSettings();
                    RootDirsCreate.CheckDatRoot();
                    RootDirsCreate.CheckRomRoot();
                    RootDirsCreate.CheckToSort();

                    var mainWindow = new MainWindow();
                    Program.frmMain = mainWindow;

                    if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        desktop.MainWindow = mainWindow;
                    }

                    mainWindow.Show();
                    Close();
                }
            }
        }
    }
}

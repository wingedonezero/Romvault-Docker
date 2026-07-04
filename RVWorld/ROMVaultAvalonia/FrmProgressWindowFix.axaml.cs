/******************************************************
 *     ROMVault3 is written by Gordon J.              *
 *     Contact gordon@romvault.com                    *
 *     Copyright 2025                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using RomVaultCore;
using RomVaultCore.FixFile;

namespace ROMVault
{
    public class FixRowItem
    {
        public string FixDir { get; set; }
        public string FixZip { get; set; }
        public string FixFile { get; set; }
        public string Size { get; set; }
        public string Status { get; set; }
        public string SourceDir { get; set; }
        public string SourceZip { get; set; }
        public string SourceFile { get; set; }
        public bool IsError { get; set; }
    }

    public partial class FrmProgressWindowFix : Window
    {
        private readonly Window _parentForm;
        private int _rowCount;
        private readonly List<string[][]> _reportPages;
        private string[][] _pageNow;

        private int _rowDisplay;

        private bool _bDone;
        private bool _closeOnExit;
        private bool _canClose;

        private ThreadWorker _thWrk;
        private readonly Finished _funcFinished;

        private readonly DispatcherTimer _timer;
        private readonly ObservableCollection<FixRowItem> _fixItems;

        public FrmProgressWindowFix(Window parentForm, bool closeOnExit, Finished funcFinished)
        {
            _closeOnExit = closeOnExit;
            _rowCount = 0;
            _rowDisplay = -1;
            _canClose = false;

            _reportPages = new List<string[][]>();
            _parentForm = parentForm;
            _funcFinished = funcFinished;

            InitializeComponent();

            _fixItems = new ObservableCollection<FixRowItem>();
            dataGridView1.ItemsSource = _fixItems;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _timer.Tick += Timer1Tick;
            _timer.Start();

            Opened += FrmProgressWindowFixShown;
            Closing += OnClosing;
        }

        private void OnClosing(object sender, WindowClosingEventArgs e)
        {
            e.Cancel = !_canClose;
        }

        private void Timer1Tick(object sender, EventArgs e)
        {
            int tmpRowCount = _rowCount;

            if (_rowDisplay == tmpRowCount || tmpRowCount == 0)
                return;

            // Add any new rows that haven't been added to the ObservableCollection yet
            while (_rowDisplay + 1 < tmpRowCount)
            {
                _rowDisplay++;
                int pageIndex = _rowDisplay / 1000;
                int rowIndex = _rowDisplay % 1000;

                string[][] page = _reportPages[pageIndex];
                string[] row = page[rowIndex];

                _fixItems.Add(new FixRowItem
                {
                    FixDir = row[0],
                    FixZip = row[1],
                    FixFile = row[2],
                    Size = row[3],
                    Status = row[4],
                    SourceDir = row[5],
                    SourceZip = row[6],
                    SourceFile = row[7],
                    IsError = row[8] != null
                });
            }

            if (_fixItems.Count > 0)
            {
                dataGridView1.ScrollIntoView(_fixItems[_fixItems.Count - 1], null);
            }
        }

        private void FrmProgressWindowFixShown(object sender, EventArgs e)
        {
            _thWrk = new ThreadWorker(Fix.PerformFixes) { wReport = BgwProgressChanged, wFinal = BgwRunWorkerCompleted };
            _thWrk.StartAsync();
        }

        private void BgwProgressChanged(object e)
        {
            if (e is bgwShowFix bgwSf)
            {
                int reportLineIndex = _rowCount % 1000;

                if (reportLineIndex == 0)
                {
                    _pageNow = new string[1000][];
                    _reportPages.Add(_pageNow);
                }

                _pageNow[reportLineIndex] =
                    new[]
                    {
                        bgwSf.FixDir, bgwSf.FixZip, bgwSf.FixFile, bgwSf.Size, bgwSf.Dir,
                        bgwSf.SourceDir, bgwSf.SourceZip, bgwSf.SourceFile, null
                    };
                _rowCount += 1;
                return;
            }

            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => BgwProgressChanged(e));
                return;
            }

            if (e is bgwShowFixError bgwSFE)
            {
                int errorRowCount = _rowCount - 1;
                int pageIndex = errorRowCount / 1000;
                int rowIndex = errorRowCount % 1000;

                string[] errorRow = _reportPages[pageIndex][rowIndex];
                errorRow[4] = bgwSFE.FixError;
                errorRow[8] = "error";

                // Update the corresponding item in the ObservableCollection if it exists
                if (errorRowCount < _fixItems.Count)
                {
                    _fixItems[errorRowCount].Status = bgwSFE.FixError;
                    _fixItems[errorRowCount].IsError = true;

                    // Force a refresh by removing and re-adding the item
                    int idx = errorRowCount;
                    var item = _fixItems[idx];
                    _fixItems.RemoveAt(idx);
                    _fixItems.Insert(idx, item);
                }
                return;
            }

            if (e is bgwProgress bgwProg)
            {
                if (bgwProg.Progress >= progressBar.Minimum && bgwProg.Progress <= progressBar.Maximum)
                {
                    progressBar.Value = bgwProg.Progress;
                }
                UpdateStatusText();
                return;
            }

            if (e is bgwText bgwT)
            {
                label.Text = bgwT.Text;
                return;
            }

            if (e is bgwSetRange bgwSR)
            {
                progressBar.Minimum = 0;
                progressBar.Maximum = bgwSR.MaxVal >= 0 ? bgwSR.MaxVal : 0;
                progressBar.Value = 0;
                UpdateStatusText();
            }
        }

        private void BgwRunWorkerCompleted()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(BgwRunWorkerCompleted);
                return;
            }

            _timer.Stop();

            // Do one final timer tick to flush any remaining rows
            Timer1Tick(null, null);

            if (!_closeOnExit)
            {
                cancelButton.Content = "Close";
                cancelButton.IsEnabled = true;
                _bDone = true;
            }
            else
            {
                _funcFinished?.Invoke();
                _parentForm.Show();
                _canClose = true;
                Close();
            }
        }

        private void UpdateStatusText()
        {
            double range = progressBar.Maximum - progressBar.Minimum;
            int percent = range > 0 ? (int)(progressBar.Value * 100 / range) : 0;
            Title = $"Fixing Files - {percent}% complete";
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            if (_bDone)
            {
                if (!_parentForm.IsVisible)
                {
                    _parentForm.Show();
                }
                _funcFinished?.Invoke();
                _canClose = true;
                Close();
            }
            else
            {
                cancelButton.IsEnabled = false;
                cancelButton.Content = "Cancelling";
                _thWrk.Cancel();
            }
        }

        private void FrmProgressWindowFix_WindowStateChanged()
        {
            switch (WindowState)
            {
                case WindowState.Minimized:
                    if (_parentForm.IsVisible)
                    {
                        _parentForm.Hide();
                    }
                    return;
                case WindowState.Maximized:
                case WindowState.Normal:
                    if (!_parentForm.IsVisible)
                    {
                        _parentForm.Show();
                    }
                    return;
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == WindowStateProperty)
            {
                FrmProgressWindowFix_WindowStateChanged();
            }
        }
    }
}

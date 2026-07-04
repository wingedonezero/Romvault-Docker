/******************************************************
 *     ROMVault3 is written by Gordon J.              *
 *     Contact gordon@romvault.com                    *
 *     Copyright 2025                                 *
 ******************************************************/

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using RomVaultCore;

namespace ROMVault
{
    public delegate void Finished();

    public class ErrorRowItem
    {
        public string Error { get; set; }
        public string ErrorFile { get; set; }
        public IBrush ForegroundBrush { get; set; }
    }

    public partial class FrmProgressWindow : Window
    {
        private readonly string _titleRoot;
        private readonly Window _parentForm;
        private bool _errorOpen;
        private bool _bDone;
        public bool Cancelled;
        public bool ShowTimeLog = false;

        private readonly ThreadWorker _thWrk;
        private readonly Finished _funcFinished;

        private DateTime _dateTime;
        private DateTime _dateTimeLast;
        private string _lastMessage;

        private bool _canClose;

        private readonly ObservableCollection<ErrorRowItem> _errorItems;

        public FrmProgressWindow(Window parentForm, string titleRoot, WorkerStart function, Finished funcFinished)
        {
            Cancelled = false;
            _parentForm = parentForm;
            _titleRoot = titleRoot;
            _funcFinished = funcFinished;
            _canClose = false;

            InitializeComponent();

            _errorItems = new ObservableCollection<ErrorRowItem>();
            ErrorGrid.ItemsSource = _errorItems;

            Width = 527;
            Height = 170;
            _dateTime = DateTime.Now;
            _dateTimeLast = _dateTime;

            _titleRoot = titleRoot;
            _lastMessage = "Initializing";

            _thWrk = new ThreadWorker(function);

            Opened += FrmProgressWindowNewShown;
            Closing += OnClosing;
        }

        public void HideCancelButton()
        {
            cancelButton.Content = "Close";
            cancelButton.IsEnabled = false;
        }

        private void OnClosing(object sender, WindowClosingEventArgs e)
        {
            e.Cancel = !_canClose;
        }

        private void FrmProgressWindowNewShown(object sender, EventArgs e)
        {
            _thWrk.wReport = BgwProgressChanged;
            _thWrk.wFinal = BgwRunWorkerCompleted;
            _thWrk.StartAsync();
        }

        private void TimeLogShow(string message)
        {
            if (!_errorOpen)
            {
                _errorOpen = true;
                Height = 350;
                MinHeight = 350;
                ErrorGrid.IsVisible = true;

                // Change headers for time log mode
                ErrorGrid.Columns[0].Header = "Time";
                ErrorGrid.Columns[1].Header = "Log";
            }

            DateTime dtNow = DateTime.Now;
            string total = Math.Round((dtNow - _dateTime).TotalSeconds, 3).ToString();
            string part = Math.Round((dtNow - _dateTimeLast).TotalSeconds, 3).ToString();
            _dateTimeLast = dtNow;

            _errorItems.Add(new ErrorRowItem
            {
                Error = $"{total} s  ,  ({part} s)",
                ErrorFile = $"Completed: {_lastMessage}"
            });
            _lastMessage = message;

            if (_errorItems.Count > 0)
            {
                ErrorGrid.ScrollIntoView(_errorItems[_errorItems.Count - 1], null);
            }
        }

        private void BgwProgressChanged(object obj)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => BgwProgressChanged(obj));
                return;
            }

            if (obj is int e)
            {
                if (e >= progressBar.Minimum && e <= progressBar.Maximum)
                {
                    progressBar.Value = e;
                }
                UpdateStatusText();
                return;
            }

            if (obj is bgwText bgwT)
            {
                label.Text = bgwT.Text;
                if (ShowTimeLog)
                    TimeLogShow(bgwT.Text);
                return;
            }
            if (obj is bgwSetRange bgwSr)
            {
                progressBar.Minimum = 0;
                progressBar.Maximum = bgwSr.MaxVal >= 0 ? bgwSr.MaxVal : 0;
                progressBar.Value = 0;
                UpdateStatusText();
                return;
            }

            if (obj is bgwText2 bgwT2)
            {
                label2.Text = bgwT2.Text;
                return;
            }

            if (obj is bgwValue2 bgwV2)
            {
                if (bgwV2.Value >= progressBar2.Minimum && bgwV2.Value <= progressBar2.Maximum)
                {
                    progressBar2.Value = bgwV2.Value;
                }
                UpdateStatusText2();
                return;
            }

            if (obj is bgwSetRange2 bgwSr2)
            {
                progressBar2.Minimum = 0;
                progressBar2.Maximum = bgwSr2.MaxVal >= 0 ? bgwSr2.MaxVal : 0;
                progressBar2.Value = 0;
                UpdateStatusText2();
                return;
            }
            if (obj is bgwRange2Visible bgwR2V)
            {
                label2.IsVisible = bgwR2V.Visible;
                progressBar2.IsVisible = bgwR2V.Visible;
                lbl2Prog.IsVisible = bgwR2V.Visible;
                return;
            }

            if (obj is bgwText3 bgwT3)
            {
                label3.Text = bgwT3.Text;
                return;
            }

            if (obj is bgwShowError bgwSE)
            {
                if (!_errorOpen)
                {
                    _errorOpen = true;
                    Height = 350;
                    MinHeight = 350;
                    ErrorGrid.IsVisible = true;
                }

                _errorItems.Add(new ErrorRowItem
                {
                    Error = bgwSE.error,
                    ErrorFile = bgwSE.filename,
                    ForegroundBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0))
                });

                if (_errorItems.Count > 0)
                {
                    ErrorGrid.ScrollIntoView(_errorItems[_errorItems.Count - 1], null);
                }
            }
        }

        private void UpdateStatusText()
        {
            double range = progressBar.Maximum - progressBar.Minimum;
            int percent = range > 0 ? (int)(progressBar.Value * 100 / range) : 0;
            Title = $"{_titleRoot} - {percent}% complete";
        }

        private void UpdateStatusText2()
        {
            lbl2Prog.Text = progressBar2.Maximum > 0 ? $"{(int)progressBar2.Value}/{(int)progressBar2.Maximum}" : "";
        }

        private void BgwRunWorkerCompleted()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(BgwRunWorkerCompleted);
                return;
            }

            if (_errorOpen)
            {
                cancelButton.IsVisible = true;
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
                Cancelled = true;
                cancelButton.IsVisible = true;
                cancelButton.Content = "Cancelling";
                cancelButton.IsEnabled = false;
                _thWrk.Cancel();
            }
        }

        private void FrmProgressWindow_WindowStateChanged()
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
                FrmProgressWindow_WindowStateChanged();
            }
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls.Primitives;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Compress;
using RVIO;
using TrrntZip;

namespace TrrntZipUIAvalonia
{
    public class GridItem : INotifyPropertyChanged
    {
        private string _fileName;
        private string _status;

        public string FileName
        {
            get => _fileName;
            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileName)));
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public partial class FrmTrrntzip : Window
    {
        private int _fileIndex;
        private int FileCount;
        private int FileCountProcessed;

        private BlockingCollection<cFile> bccFile;

        private class ThreadProcess
        {
            public TextBlock threadLabel;
            public ProgressBar threadProgress;
            public string tLabel;
            public int tProgress;
            public CProcessZip cProcessZip;
            public Thread thread;
        }

        private readonly List<ThreadProcess> _threads;

        private class dGrid
        {
            public int fileId;
            public string filename;
            public string status;
        }

        private readonly List<dGrid> tGrid;
        private int tGridMax = 0;

        private readonly ObservableCollection<GridItem> _gridItems;

        private readonly PauseCancel pc;

        private bool _working;
        private int _threadCount;

        private bool UiUpdate = false;
        private bool scanningForFiles = false;

        private DispatcherTimer _timer;

        private Bitmap _pauseBitmap;
        private Bitmap _resumeBitmap;

        public FrmTrrntzip()
        {
            UiUpdate = true;
            InitializeComponent();

            this.Title = $"SAM-UI ({Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0"})";

            // Load button images from Avalonia resources
            _pauseBitmap = LoadAssetBitmap("Pause.png");
            _resumeBitmap = LoadAssetBitmap("Resume.png");

            // Set up drag-drop on DropBox
            DropBox.AddHandler(DragDrop.DropEvent, PDragDrop);
            DropBox.AddHandler(DragDrop.DragOverEvent, PDragEnter);

            // Load settings
            string sval = AppSettings.ReadSetting("InZip");
            if (!int.TryParse(sval, out int intVal))
            {
                intVal = 2;
            }
            cboInType.SelectedIndex = intVal;

            sval = AppSettings.ReadSetting("OutZip");
            if (!int.TryParse(sval, out intVal))
            {
                intVal = 0;
            }
            cboOutType.SelectedIndex = UIIndexFromZipStructure((ZipStructure)intVal);

            sval = AppSettings.ReadSetting("Force");
            chkForce.IsChecked = sval == "True";

            sval = AppSettings.ReadSetting("Fix");
            chkFix.IsChecked = sval != "False";

            tbProccessors.Minimum = 1;
            tbProccessors.Maximum = Environment.ProcessorCount;
            sval = AppSettings.ReadSetting("ProcCount");
            if (!int.TryParse(sval, out int procc))
            {
                procc = (int)tbProccessors.Maximum;
            }

            if (procc > (int)tbProccessors.Maximum)
            {
                procc = (int)tbProccessors.Maximum;
            }

            tbProccessors.Value = procc;

            _threads = new List<ThreadProcess>();
            tGrid = new List<dGrid>();
            _gridItems = new ObservableCollection<GridItem>();
            dataGrid.ItemsSource = _gridItems;
            pc = new PauseCancel();

            // Set up the timer
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(125)
            };
            _timer.Tick += Timer_Tick;

            // Wire up Closing event
            Closing += OnClosing;

            SetUpWorkerThreads();

            UiUpdate = false;
        }

        private static Bitmap LoadAssetBitmap(string name)
        {
            try
            {
                var uri = new Uri($"avares://TrrntZipUIAvalonia/Assets/{name}");
                using var stream = AssetLoader.Open(uri);
                return new Bitmap(stream);
            }
            catch
            {
                return null;
            }
        }

        private void SetUpWorkerThreads()
        {
            _threadCount = (int)tbProccessors.Value;

            bccFile?.CompleteAdding();

            foreach (ThreadProcess tp in _threads)
            {
                ThreadStatusPanel.Children.Remove(tp.threadLabel);
                ThreadStatusPanel.Children.Remove(tp.threadProgress);

                tp.cProcessZip.ProcessFileStartCallBack = null;
                tp.cProcessZip.StatusCallBack = null;
                tp.cProcessZip.ErrorCallBack = null;
                tp.cProcessZip.ProcessFileEndCallBack = null;
                tp.thread.Join();
            }

            bccFile?.Dispose();

            _threads.Clear();
            bccFile = new BlockingCollection<cFile>();

            int workers = (Environment.ProcessorCount - 1) / _threadCount;
            if (workers == 0) workers = 1;

            for (int i = 0; i < _threadCount; i++)
            {
                ThreadProcess threadProcess = new ThreadProcess();
                _threads.Add(threadProcess);

                TextBlock pLabel = new TextBlock
                {
                    IsVisible = true,
                    Margin = new Thickness(12, 0, 12, 0),
                    Text = $"Processor {i + 1}"
                };
                ThreadStatusPanel.Children.Add(pLabel);
                threadProcess.threadLabel = pLabel;

                ProgressBar pProgress = new ProgressBar
                {
                    IsVisible = true,
                    Margin = new Thickness(12, 0, 12, 4),
                    Height = 12,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0
                };
                ThreadStatusPanel.Children.Add(pProgress);
                threadProcess.threadProgress = pProgress;

                threadProcess.cProcessZip = new CProcessZip
                {
                    ThreadId = i,
                    bcCfile = bccFile,
                    ProcessFileStartCallBack = ProcessFileStartCallback,
                    StatusCallBack = StatusCallBack,
                    ErrorCallBack = ErrorCallBack,
                    ProcessFileEndCallBack = ProcessFileEndCallback,
                    pauseCancel = pc,
                    workerCount = workers
                };
                threadProcess.thread = new Thread(threadProcess.cProcessZip.MigrateZip);
                threadProcess.thread.Start();
            }

            // Ensure window is tall enough for thread displays
            double neededHeight = 325 + 30 * _threadCount;
            if (Height < neededHeight)
            {
                Height = neededHeight;
            }

            Debug.WriteLine($"Cores found: {Environment.ProcessorCount},  File Workers: {_threadCount},   zstd core per file: {workers}");
        }

        private void PDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Files))
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }

        private void PDragDrop(object sender, DragEventArgs e)
        {
            if (_working)
                return;

            var storageItems = e.Data.GetFiles();
            if (storageItems == null)
                return;

            var files = storageItems
                .Select(item => item.TryGetLocalPath())
                .Where(path => path != null)
                .ToArray();

            if (files.Length == 0)
                return;

            Array.Sort(files);

            Program.ForceReZip = chkForce.IsChecked == true;
            Program.CheckOnly = chkFix.IsChecked != true;
            Program.InZip = (zipType)cboInType.SelectedIndex;
            Program.OutZip = ZipStructureFromUIIndex(cboOutType.SelectedIndex);

            lock (tGrid)
            {
                tGrid.Clear();
            }
            tGridMax = 0;
            _gridItems.Clear();

            StartWorking();

            FileCountProcessed = 0;
            scanningForFiles = true;
            FileAdder pm = new FileAdder(bccFile, files, UpdateFileCount, ProcessFileEndCallback);
            Thread procT = new Thread(pm.ProcFiles);
            procT.Start();

            _timer.Interval = TimeSpan.FromMilliseconds(125);
            _timer.IsEnabled = true;
        }

        private void StartWorking()
        {
            _working = true;
            DropBoxImage.Source = LoadAssetBitmap("giphy.gif");
            cboInType.IsEnabled = false;
            cboOutType.IsEnabled = false;
            chkForce.IsEnabled = false;
            chkFix.IsEnabled = false;
            tbProccessors.IsEnabled = false;
            btnCancel.IsEnabled = true;
            btnPause.IsEnabled = true;
        }

        private void StopWorking()
        {
            _working = false;
            DropBoxImage.Source = null;
            cboInType.IsEnabled = true;
            cboOutType.IsEnabled = true;
            chkForce.IsEnabled = true;
            chkFix.IsEnabled = true;
            tbProccessors.IsEnabled = true;
            btnCancel.IsEnabled = false;
            btnPause.IsEnabled = false;
        }

        protected void OnClosing(object sender, WindowClosingEventArgs e)
        {
            if (_working)
            {
                e.Cancel = true;
            }
            else
            {
                bccFile?.CompleteAdding();
                foreach (ThreadProcess tp in _threads)
                {
                    tp.cProcessZip.ProcessFileStartCallBack = null;
                    tp.cProcessZip.StatusCallBack = null;
                    tp.cProcessZip.ErrorCallBack = null;
                    tp.cProcessZip.ProcessFileEndCallBack = null;
                    tp.thread.Join();
                }
                bccFile?.Dispose();
            }

            if (_errorLogWindow != null)
            {
                _errorLogWindow.closing = true;
                _errorLogWindow.Close();
            }
        }

        private void picTitle_Click(object sender, RoutedEventArgs e)
        {
            ClickDonate();
        }

        private void ClickDonate()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://paypal.me/romvault",
                    UseShellExecute = true
                });
            }
            catch
            {
                // ignored
            }
        }

        private void tbProccessors_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (UiUpdate)
                return;

            AppSettings.AddUpdateAppSettings("ProcCount", ((int)tbProccessors.Value).ToString());
            SetUpWorkerThreads();
        }

        private void chkFix_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (UiUpdate)
                return;
            AppSettings.AddUpdateAppSettings("Fix", (chkFix.IsChecked == true).ToString());
        }

        private void chkForce_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (UiUpdate)
                return;
            AppSettings.AddUpdateAppSettings("Force", (chkForce.IsChecked == true).ToString());
        }

        private void cboInType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UiUpdate)
                return;
            AppSettings.AddUpdateAppSettings("InZip", cboInType.SelectedIndex.ToString());
        }

        private void cboOutType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UiUpdate)
                return;
            AppSettings.AddUpdateAppSettings("OutZip", ((int)ZipStructureFromUIIndex(cboOutType.SelectedIndex)).ToString());
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            if (!pc.Paused)
            {
                // Pause
                if (_resumeBitmap != null)
                    btnPauseImage.Source = _resumeBitmap;
                DropBox.IsEnabled = false;
                pc.Pause();
            }
            else
            {
                // Resume after a Pause
                if (_pauseBitmap != null)
                    btnPauseImage.Source = _pauseBitmap;
                DropBox.IsEnabled = true;
                pc.UnPause();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            // start Cancel
            if (_pauseBitmap != null)
                btnPauseImage.Source = _pauseBitmap;
            pc.Cancel();
            DropBox.IsEnabled = true;
            btnCancel.IsEnabled = false;
            btnPause.IsEnabled = false;
        }

        private static ZipStructure ZipStructureFromUIIndex(int cboIndex)
        {
            switch (cboIndex)
            {
                case 0: return ZipStructure.ZipTrrnt;
                case 1: return ZipStructure.ZipZSTD;
                case 2: return ZipStructure.SevenZipNZSTD;
                case 3: return ZipStructure.SevenZipSZSTD;
                case 4: return ZipStructure.SevenZipNLZMA;
                case 5: return ZipStructure.SevenZipSLZMA;
                default: return ZipStructure.ZipTrrnt;
            }
        }

        private static int UIIndexFromZipStructure(ZipStructure zipStructure)
        {
            switch (zipStructure)
            {
                case ZipStructure.ZipTrrnt: return 0;
                case ZipStructure.ZipZSTD: return 1;
                case ZipStructure.SevenZipNZSTD: return 2;
                case ZipStructure.SevenZipSZSTD: return 3;
                case ZipStructure.SevenZipNLZMA: return 4;
                case ZipStructure.SevenZipSLZMA: return 5;
                default: return 0;
            }
        }

        #region callbacks

        private void UpdateFileCount(int fileCount)
        {
            FileCount = fileCount;
        }

        private void ProcessFileStartCallback(int processId, int fileId, string filename)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => ProcessFileStartCallback(processId, fileId, filename));
                return;
            }

            _fileIndex = fileId + 1;

            _threads[processId].tLabel = RVIO.Path.GetFileName(filename);
            _threads[processId].tProgress = 0;

            if ((fileId + 1) > tGridMax)
                tGridMax = (fileId + 1);

            lock (tGrid)
            {
                tGrid.Add(new dGrid() { fileId = fileId, filename = filename, status = "Processing....(" + processId + ")" });
            }
        }

        private void ProcessFileEndCallback(int processId, int fileId, TrrntZipStatus trrntZipStatus)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => ProcessFileEndCallback(processId, fileId, trrntZipStatus));
                return;
            }

            if (processId == -1)
            {
                scanningForFiles = false;

                if (FileCount == 0)
                {
                    StopWorking();
                    if (pc.Cancelled)
                        pc.ResetCancel();
                }
            }
            else
            {
                _threads[processId].tProgress = 100;
                if ((fileId + 1) > tGridMax)
                    tGridMax = (fileId + 1);

                dGrid tGridn = new dGrid() { fileId = fileId, filename = null };
                switch (trrntZipStatus)
                {
                    case TrrntZipStatus.ValidTrrntzip:
                        tGridn.status = "Valid Archive";
                        break;
                    case TrrntZipStatus.Trrntzipped:
                        tGridn.status = "Re-Structured";
                        break;
                    default:
                        tGridn.status = trrntZipStatus.ToString();
                        break;
                }
                lock (tGrid)
                {
                    tGrid.Add(tGridn);
                }

                FileCountProcessed += 1;

                if (!scanningForFiles && FileCountProcessed == FileCount)
                {
                    StopWorking();
                    if (pc.Cancelled)
                        pc.ResetCancel();
                }
            }
        }

        private void StatusCallBack(int processId, int percent)
        {
            _threads[processId].tProgress = percent;
        }

        private ErrorLogWindow _errorLogWindow = null;

        private void ErrorCallBack(int processId, string message)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => ErrorCallBack(processId, message));
                return;
            }

            if (_errorLogWindow == null)
            {
                _errorLogWindow = new ErrorLogWindow();
            }
            _errorLogWindow.AddError(message);
        }

        #endregion

        private int uiFileCount = -1;
        private int uiFileIndex = -1;

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_fileIndex != uiFileIndex || FileCount != uiFileCount)
            {
                uiFileIndex = _fileIndex;
                uiFileCount = FileCount;

                lblTotalStatus.Text = @"( " + uiFileIndex + @" / " + uiFileCount + @" )";
            }

            foreach (ThreadProcess tp in _threads)
            {
                if (tp.tProgress != (int)tp.threadProgress.Value)
                    tp.threadProgress.Value = tp.tProgress;
                if (tp.tLabel != tp.threadLabel.Text)
                    tp.threadLabel.Text = tp.tLabel;
            }

            // Ensure _gridItems has enough entries
            while (_gridItems.Count < tGridMax)
            {
                _gridItems.Add(new GridItem { FileName = "", Status = "" });
            }

            lock (tGrid)
            {
                foreach (dGrid dg in tGrid)
                {
                    if (dg.fileId < _gridItems.Count)
                    {
                        if (dg.filename != null)
                            _gridItems[dg.fileId].FileName = dg.filename;
                        if (dg.status != null)
                            _gridItems[dg.fileId].Status = dg.status;
                    }
                }
                tGrid.Clear();
            }

            // Scroll to last item
            if (_gridItems.Count > 0)
            {
                dataGrid.ScrollIntoView(_gridItems[_gridItems.Count - 1], null);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Compress;
using DarkAvalonia;
using DATReader.DatClean;
using RomVaultCore;
using RomVaultCore.ReadDat;
using RomVaultCore.RvDB;

namespace ROMVault
{
    public partial class FrmDirectorySettings : Window
    {
        private static readonly Color _cMagenta = Color.FromArgb(255, 255, 214, 255);
        private static readonly Color _cGreen = Color.FromArgb(255, 214, 255, 214);
        private static readonly Color _cYellow = Color.FromArgb(255, 255, 255, 214);

        public bool ChangesMade;

        private DatRule _rule;
        private bool _displayType;

        private ObservableCollection<DatRuleRowViewModel> _gridItems = new ObservableCollection<DatRuleRowViewModel>();
        private ObservableCollection<string> _categoryItems = new ObservableCollection<string>();

        public FrmDirectorySettings()
        {
            InitializeComponent();

            cboFileType.Items.Clear();
            cboFileType.Items.Add("Uncompressed");
            cboFileType.Items.Add("Zip");
            cboFileType.Items.Add("SevenZip");
            cboFileType.Items.Add("Mixed (Archive as File)");

            cboMergeType.Items.Clear();
            cboMergeType.Items.Add("Nothing");
            cboMergeType.Items.Add("Split");
            cboMergeType.Items.Add("Merge");
            cboMergeType.Items.Add("NonMerge");

            cboFilterType.Items.Clear();
            cboFilterType.Items.Add("Roms & CHDs");
            cboFilterType.Items.Add("Roms Only");
            cboFilterType.Items.Add("CHDs Only");

            cboDirType.Items.Clear();
            cboDirType.Items.Add("Use subdirs for all sets");
            cboDirType.Items.Add("Do not use subdirs for sets");
            cboDirType.Items.Add("Use subdirs for rom name conflicts");
            cboDirType.Items.Add("Use subdirs for multi-rom sets");
            cboDirType.Items.Add("Use subdirs for multi-rom sets or set/rom name mismatches");

            cboHeaderType.Items.Clear();
            cboHeaderType.Items.Add("Optional");
            cboHeaderType.Items.Add("Headered");
            cboHeaderType.Items.Add("Headerless");

            cboFileType.SelectionChanged += cboFileType_SelectionChanged;
            chkSingleArchive.Click += chkSingleArchive_CheckedChanged;
            chkAddCategorySubDirs.Click += chkAddCategorySubDirs_CheckedChanged;

            DataGridGames.ItemsSource = _gridItems;
            DataGridGames.DoubleTapped += DataGridGamesDoubleClick;

            dgCategories.ItemsSource = _categoryItems;

            Activated += FrmSetDirActivated;
        }

        public void SetLocation(string dLocation)
        {
            _rule = FindRule(dLocation);
            SetDisplay();
            UpdateGrid();
        }

        public void SetDisplayType(bool type)
        {
            _displayType = type;
            btnDelete.IsVisible = type;

            // Hide bottom section (grid + bottom buttons) when type is true
            btnDeleteSelected.IsVisible = !type;
            btnResetAll.IsVisible = !type;
            DataGridGames.IsVisible = !type;

            if (type)
            {
                // Compact mode still needs extra room in Avalonia/Fluent to avoid clipping
                MinHeight = 540;
                Height = 540;
                CanResize = true;
            }
            else
            {
                MinHeight = 620;
                Height = 700;
                CanResize = true;
            }
        }

        private static DatRule FindRule(string dLocation)
        {
            foreach (DatRule t in Settings.rvSettings.DatRules)
            {
                if (string.Compare(t.DirKey, dLocation, StringComparison.Ordinal) == 0)
                    return t;
            }

            return new DatRule { DirKey = dLocation, IgnoreFiles = new List<string>() };
        }

        private void SetCompressionTypeFromArchive()
        {
            cboCompression.Items.Clear();
            switch (cboFileType.SelectedIndex)
            {
                case 0:
                    chkFileTypeOverride.IsEnabled = true;
                    cboCompression.IsEnabled = false;
                    chkConvertWhenFixing.IsEnabled = false;
                    break;
                case 1:
                    chkFileTypeOverride.IsEnabled = true;
                    cboCompression.Items.Add("Deflate - Trrntzip");
                    cboCompression.Items.Add("ZSTD");
                    cboCompression.IsEnabled = true;
                    chkConvertWhenFixing.IsEnabled = true;
                    if (_rule.CompressionSub == ZipStructure.ZipTrrnt)
                        cboCompression.SelectedIndex = 0;
                    else if (_rule.CompressionSub == ZipStructure.ZipZSTD)
                        cboCompression.SelectedIndex = 1;
                    else
                        cboCompression.SelectedIndex = 0;
                    break;
                case 2:
                    chkFileTypeOverride.IsEnabled = true;
                    cboCompression.Items.Add("LZMA Solid - rv7z");
                    cboCompression.Items.Add("LZMA Non-Solid");
                    cboCompression.Items.Add("ZSTD Solid");
                    cboCompression.Items.Add("ZSTD Non-Solid");
                    cboCompression.IsEnabled = true;
                    chkConvertWhenFixing.IsEnabled = true;
                    if (_rule.CompressionSub == ZipStructure.SevenZipSLZMA)
                        cboCompression.SelectedIndex = 0;
                    else if (_rule.CompressionSub == ZipStructure.SevenZipNLZMA)
                        cboCompression.SelectedIndex = 1;
                    else if (_rule.CompressionSub == ZipStructure.SevenZipSZSTD)
                        cboCompression.SelectedIndex = 2;
                    else if (_rule.CompressionSub == ZipStructure.SevenZipNZSTD)
                        cboCompression.SelectedIndex = 3;
                    else
                        cboCompression.SelectedIndex = 0;
                    break;
                case 3:
                    chkFileTypeOverride.IsEnabled = false;
                    cboCompression.IsEnabled = false;
                    chkConvertWhenFixing.IsEnabled = false;
                    break;
            }
        }

        private void cboFileType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetCompressionTypeFromArchive();
        }

        private void SetDisplay()
        {
            txtDATLocation.Text = _rule.DirKey;

            cboFileType.SelectedIndex = _rule.Compression == FileType.FileOnly ? 3 : (int)_rule.Compression - 1;
            chkFileTypeOverride.IsChecked = _rule.CompressionOverrideDAT;

            SetCompressionTypeFromArchive();
            chkConvertWhenFixing.IsChecked = _rule.ConvertWhileFixing;

            cboMergeType.SelectedIndex = (int)_rule.Merge;
            chkMergeTypeOverride.IsChecked = _rule.MergeOverrideDAT;

            cboFilterType.SelectedIndex = (int)_rule.Filter;

            chkMultiDatDirOverride.IsChecked = _rule.MultiDATDirOverride;
            chkUseDescription.IsChecked = _rule.UseDescriptionAsDirName;
            chkUseIdForName.IsChecked = _rule.UseIdForName;

            chkSingleArchive.IsChecked = _rule.SingleArchive;

            cboDirType.IsEnabled = _rule.SingleArchive;
            cboDirType.SelectedIndex = (int)_rule.SubDirType;

            cboHeaderType.SelectedIndex = (int)_rule.HeaderType;

            textBox1.Text = "";
            if (_rule.IgnoreFiles != null)
            {
                foreach (string file in _rule.IgnoreFiles)
                {
                    textBox1.Text += file + Environment.NewLine;
                }
            }

            chkCompleteOnly.IsChecked = _rule.CompleteOnly;

            chkAddCategorySubDirs.IsChecked = _rule.AddCategorySubDirs;
            if (_rule.AddCategorySubDirs)
                SetCategoryList();
        }

        private void UpdateGrid()
        {
            _gridItems.Clear();

            foreach (DatRule t in Settings.rvSettings.DatRules)
            {
                Color? rowColor = null;

                if (t.DirPath == "ToSort")
                {
                    rowColor = _cMagenta;
                }
                else if (t == _rule)
                {
                    rowColor = _cGreen;
                }
                else if (t.DirKey.Length > _rule.DirKey.Length)
                {
                    string separator = OperatingSystem.IsWindows() ? "\\" : "/";
                    if (t.DirKey.Substring(0, _rule.DirKey.Length + 1) == _rule.DirKey + separator)
                    {
                        rowColor = _cYellow;
                    }
                }

                _gridItems.Add(new DatRuleRowViewModel(t, rowColor));
            }

            DataGridGames.SelectedIndex = -1;
        }

        private ZipStructure ReadFromCheckBoxes()
        {
            if (cboFileType.SelectedIndex == 0)
                return ZipStructure.None;

            if (cboFileType.SelectedIndex == 1)
            {
                if (cboCompression.SelectedIndex == 0)
                    return ZipStructure.ZipTrrnt;
                if (cboCompression.SelectedIndex == 1)
                    return ZipStructure.ZipZSTD;
            }
            else if (cboFileType.SelectedIndex == 2)
            {
                if (cboCompression.SelectedIndex == 0)
                    return ZipStructure.SevenZipSLZMA;
                if (cboCompression.SelectedIndex == 1)
                    return ZipStructure.SevenZipNLZMA;
                if (cboCompression.SelectedIndex == 2)
                    return ZipStructure.SevenZipSZSTD;
                if (cboCompression.SelectedIndex == 3)
                    return ZipStructure.SevenZipNZSTD;
            }
            else if (cboFileType.SelectedIndex == 3)
                return ZipStructure.None;

            return ZipStructure.None;
        }

        private void BtnApplyClick(object sender, RoutedEventArgs e)
        {
            ChangesMade = true;

            _rule.Compression = cboFileType.SelectedIndex == 3 ? FileType.FileOnly : (FileType)cboFileType.SelectedIndex + 1;
            _rule.CompressionOverrideDAT = chkFileTypeOverride.IsChecked == true;
            _rule.CompressionSub = ReadFromCheckBoxes();
            _rule.ConvertWhileFixing = chkConvertWhenFixing.IsChecked == true;
            _rule.Merge = (MergeType)cboMergeType.SelectedIndex;
            _rule.MergeOverrideDAT = chkMergeTypeOverride.IsChecked == true;
            _rule.Filter = (FilterType)cboFilterType.SelectedIndex;
            _rule.HeaderType = (HeaderType)cboHeaderType.SelectedIndex;
            _rule.SingleArchive = chkSingleArchive.IsChecked == true;
            _rule.SubDirType = (RemoveSubType)cboDirType.SelectedIndex;
            _rule.MultiDATDirOverride = chkMultiDatDirOverride.IsChecked == true;
            _rule.UseDescriptionAsDirName = chkUseDescription.IsChecked == true;
            _rule.UseIdForName = chkUseIdForName.IsChecked == true;

            _rule.CompleteOnly = chkCompleteOnly.IsChecked == true;

            _rule.AddCategorySubDirs = chkAddCategorySubDirs.IsChecked == true;

            string strtxt = textBox1.Text ?? "";
            strtxt = strtxt.Replace("\r", "");
            string[] strsplit = strtxt.Split('\n');

            _rule.IgnoreFiles = new List<string>(strsplit);
            int i;
            for (i = 0; i < _rule.IgnoreFiles.Count; i++)
            {
                _rule.IgnoreFiles[i] = _rule.IgnoreFiles[i].Trim();
                if (string.IsNullOrEmpty(_rule.IgnoreFiles[i]))
                {
                    _rule.IgnoreFiles.RemoveAt(i);
                    i--;
                }
            }

            bool updatingRule = false;
            for (i = 0; i < Settings.rvSettings.DatRules.Count; i++)
            {
                if (Settings.rvSettings.DatRules[i] == _rule)
                {
                    updatingRule = true;
                    break;
                }

                if (string.Compare(Settings.rvSettings.DatRules[i].DirKey, _rule.DirKey, StringComparison.Ordinal) > 0)
                {
                    break;
                }
            }

            if (!updatingRule)
                Settings.rvSettings.DatRules.Insert(i, _rule);

            Settings.rvSettings.SetRegExRules();

            UpdateGrid();
            Settings.WriteConfig(Settings.rvSettings);
            DatUpdate.CheckAllDats(DB.DirRoot.Child(0), _rule.DirKey);

            if (_displayType)
                Close();
        }

        private void BtnDeleteClick(object sender, RoutedEventArgs e)
        {
            string datLocation = _rule.DirKey;

            if (datLocation == "RomVault")
            {
                ReportError.Show("You cannot delete the " + datLocation + " Directory Settings", "RomVault Rom Location");
                return;
            }

            ChangesMade = true;

            DatUpdate.CheckAllDats(DB.DirRoot.Child(0), datLocation);
            for (int i = 0; i < Settings.rvSettings.DatRules.Count; i++)
            {
                if (Settings.rvSettings.DatRules[i].DirKey == datLocation)
                {
                    Settings.rvSettings.DatRules.RemoveAt(i);
                    i--;
                }
            }
            Settings.WriteConfig(Settings.rvSettings);

            UpdateGrid();
            Close();
        }

        private void BtnDeleteSelectedClick(object sender, RoutedEventArgs e)
        {
            ChangesMade = true;
            var selectedItems = DataGridGames.SelectedItems;
            if (selectedItems == null) return;

            foreach (object item in selectedItems)
            {
                if (item is DatRuleRowViewModel rowVm)
                {
                    string datLocation = rowVm.DirKey;

                    if (datLocation == "RomVault")
                    {
                        ReportError.Show("You cannot delete the " + datLocation + " Directory Settings", "RomVault Rom Location");
                    }
                    else
                    {
                        DatUpdate.CheckAllDats(DB.DirRoot.Child(0), datLocation);
                        for (int i = 0; i < Settings.rvSettings.DatRules.Count; i++)
                        {
                            if (Settings.rvSettings.DatRules[i].DirKey == datLocation)
                            {
                                Settings.rvSettings.DatRules.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }
            }
            Settings.WriteConfig(Settings.rvSettings);

            UpdateGrid();
        }

        private void BtnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DataGridGamesDoubleClick(object sender, RoutedEventArgs e)
        {
            if (DataGridGames.SelectedItem is DatRuleRowViewModel rowVm)
            {
                Title = "Edit Existing DATs Rule";
                _rule = rowVm.Rule;
                UpdateGrid();
                SetDisplay();
            }
        }

        private void DataGridGames_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.DataContext is not DatRuleRowViewModel rowVm)
                return;

            if (rowVm.RowColor.HasValue)
            {
                var bgColor = dark.StatusColor(rowVm.RowColor.Value);
                var fgColor = MainWindow.Contrasty(bgColor);
                e.Row.Background = new SolidColorBrush(bgColor);
                TextElement.SetForeground(e.Row, new SolidColorBrush(fgColor));
            }
            else
            {
                e.Row.ClearValue(DataGridRow.BackgroundProperty);
                e.Row.ClearValue(TextElement.ForegroundProperty);
            }
        }

        private void FrmSetDirActivated(object sender, EventArgs e)
        {
            DataGridGames.SelectedIndex = -1;
        }

        private void BtnResetAllClick(object sender, RoutedEventArgs e)
        {
            ChangesMade = true;
            for (int i = 0; i < Settings.rvSettings.DatRules.Count; i++)
            {
                DatRule rule = Settings.rvSettings.DatRules[i];

                if (rule.Compression != FileType.Zip ||
                    rule.CompressionOverrideDAT ||
                    rule.Merge != MergeType.Split ||
                    rule.HeaderType != HeaderType.Optional ||
                    rule.MergeOverrideDAT ||
                    rule.SubDirType != RemoveSubType.KeepAllSubDirs ||
                    rule.SingleArchive ||
                    rule.MultiDATDirOverride ||
                    rule.UseDescriptionAsDirName ||
                    rule.UseIdForName ||
                    rule.CompleteOnly)
                    DatUpdate.CheckAllDats(DB.DirRoot.Child(0), rule.DirKey);
            }

            Settings.rvSettings.ResetDatRules();
            Settings.WriteConfig(Settings.rvSettings);
            _rule = Settings.rvSettings.DatRules[0];
            UpdateGrid();
            SetDisplay();
        }

        private void chkSingleArchive_CheckedChanged(object sender, RoutedEventArgs e)
        {
            cboDirType.IsEnabled = chkSingleArchive.IsChecked == true;
        }

        private void chkAddCategorySubDirs_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (chkAddCategorySubDirs.IsChecked != true)
            {
                dgCategories.IsEnabled = false;
                btnUp.IsEnabled = false;
                btnDown.IsEnabled = false;
                return;
            }

            dgCategories.IsEnabled = true;
            btnUp.IsEnabled = true;
            btnDown.IsEnabled = true;

            if (_rule.CategoryOrder == null || _rule.CategoryOrder.Count == 0)
            {
                _rule.CategoryOrder = new List<string>()
                {
                    "Preproduction",
                    "Educational",
                    "Guides",
                    "Manuals",
                    "Magazines",
                    "Documents",
                    "Audio",
                    "Video",
                    "Multimedia",
                    "Coverdiscs",
                    "Covermount",
                    "Bonus Discs",
                    "Bonus",
                    "Add-Ons",
                    "Source Code",
                    "Updates",
                    "Applications",
                    "Demos",
                    "Games",
                    "Miscellaneous"
                };
            }
            SetCategoryList();
        }

        private void SetCategoryList()
        {
            _categoryItems.Clear();
            if (_rule.CategoryOrder == null) return;
            foreach (string s in _rule.CategoryOrder)
            {
                _categoryItems.Add(s);
            }
        }

        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            if (_rule.CategoryOrder == null) return;
            int idx = dgCategories.SelectedIndex;
            if (idx <= 0)
                return;

            string v = _rule.CategoryOrder[idx];
            _rule.CategoryOrder[idx] = _rule.CategoryOrder[idx - 1];
            _rule.CategoryOrder[idx - 1] = v;

            _categoryItems[idx] = _rule.CategoryOrder[idx];
            _categoryItems[idx - 1] = _rule.CategoryOrder[idx - 1];

            dgCategories.SelectedIndex = idx - 1;
            dgCategories.ScrollIntoView(_categoryItems[Math.Max(0, idx - 1 - 4)], null);
        }

        private void btnDown_Click(object sender, RoutedEventArgs e)
        {
            if (_rule.CategoryOrder == null) return;
            int idx = dgCategories.SelectedIndex;
            if (idx < 0 || idx >= _rule.CategoryOrder.Count - 1)
                return;

            string v = _rule.CategoryOrder[idx];
            _rule.CategoryOrder[idx] = _rule.CategoryOrder[idx + 1];
            _rule.CategoryOrder[idx + 1] = v;

            _categoryItems[idx] = _rule.CategoryOrder[idx];
            _categoryItems[idx + 1] = _rule.CategoryOrder[idx + 1];

            dgCategories.SelectedIndex = idx + 1;
            dgCategories.ScrollIntoView(_categoryItems[Math.Min(_categoryItems.Count - 1, idx + 1)], null);
        }
    }

    /// <summary>
    /// ViewModel for each row in the DataGridGames grid. Wraps a DatRule
    /// and exposes display properties for binding.
    /// </summary>
    public class DatRuleRowViewModel : INotifyPropertyChanged
    {
        public DatRule Rule { get; }
        public Color? RowColor { get; }

        public DatRuleRowViewModel(DatRule rule, Color? rowColor)
        {
            Rule = rule;
            RowColor = rowColor;
        }

        public string DirKey => Rule.DirKey;

        public string ArchiveTypeDisplay => Rule.CompressionSub.ToString();

        public string MergeDisplay => Rule.Merge.ToString();

        public Bitmap SingleArchiveImage =>
            Rule.SingleArchive
                ? rvImages.GetBitmap("Tick")
                : rvImages.GetBitmap("unTick");

        public IBrush RowBackground =>
            RowColor.HasValue
                ? new SolidColorBrush(RowColor.Value)
                : Brushes.Transparent;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

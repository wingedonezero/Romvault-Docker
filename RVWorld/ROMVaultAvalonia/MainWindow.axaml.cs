/******************************************************
 *     ROMVault3 is written by Gordon J.              *
 *     Contact gordon@romvault.com                    *
 *     Copyright 2025                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DarkAvalonia;
using DATReader.DatStore;
using DATReader.DatWriter;
using RomVaultCore;
using RomVaultCore.ReadDat;
using RomVaultCore.RvDB;
using RomVaultCore.Scanner;
using RomVaultCore.Utils;
using RVIO;
using TrrntZipUIAvalonia;

namespace ROMVault
{
    public partial class MainWindow : Window
    {
        private static readonly Color CBlue = Color.FromRgb(214, 214, 255);
        private static readonly Color CGreyBlue = Color.FromRgb(214, 224, 255);
        private static readonly Color CRed = Color.FromRgb(255, 214, 214);
        private static readonly Color CBrightRed = Color.FromRgb(255, 0, 0);
        private static readonly Color CGreen = Color.FromRgb(214, 255, 214);
        private static readonly Color CNeonGreen = Color.FromRgb(100, 255, 100);
        private static readonly Color CLightRed = Color.FromRgb(255, 235, 235);
        private static readonly Color CSoftGreen = Color.FromRgb(150, 200, 150);
        private static readonly Color CGrey = Color.FromRgb(214, 214, 214);
        private static readonly Color CCyan = Color.FromRgb(214, 255, 255);
        private static readonly Color CCyanGrey = Color.FromRgb(214, 225, 225);
        private static readonly Color CMagenta = Color.FromRgb(255, 214, 255);
        private static readonly Color CBrown = Color.FromRgb(140, 80, 80);
        private static readonly Color CPurple = Color.FromRgb(214, 140, 214);
        private static readonly Color CYellow = Color.FromRgb(255, 255, 214);
        private static readonly Color CDarkYellow = Color.FromRgb(255, 255, 100);
        private static readonly Color COrange = Color.FromRgb(255, 214, 140);
        private static readonly Color CWhite = Color.FromRgb(255, 255, 255);

        internal static int[] _gameGridColumnXPositions;

        internal readonly Color[] _displayColor;
        internal readonly Color[] _fontColor;

        private RvFile _clickedTree;
        internal bool _updatingGameGrid;

        private FrmKey _fk;

        private DispatcherTimer _timer1;

        // Context menus built in code
        private ContextMenu _mnuContext;
        private ContextMenu _mnuContextToSort;

        private MenuItem _mnuOpen;
        private MenuItem _mnuToSortOpen;
        private MenuItem _mnuToSortDelete;
        private MenuItem _mnuToSortSetPrimary;
        private MenuItem _mnuToSortSetCache;
        private MenuItem _mnuToSortSetFileOnly;
        private MenuItem _mnuToSortClearFileOnly;
        private MenuItem _mnuToSortUp;
        private MenuItem _mnuToSortDown;

        // Observable collections for DataGrid binding
        internal ObservableCollection<GameGridItem> _gameGridItems = new ObservableCollection<GameGridItem>();
        internal ObservableCollection<RomGridItem> _romGridItems = new ObservableCollection<RomGridItem>();

        #region MainUISetup

        public MainWindow()
        {
            InitializeComponent();

            // Set button images
            imgUpdateDats.Source = rvImages.GetBitmap("btnUpdateDats_Enabled");
            imgScanRoms.Source = rvImages.GetBitmap("btnScanRoms_Enabled");
            imgFindFixes.Source = rvImages.GetBitmap("btnFindFixes_Enabled");
            imgFixFiles.Source = rvImages.GetBitmap("btnFixFiles_Enabled");
            imgReport.Source = rvImages.GetBitmap("btnReport_Enabled");

            imgDefault1.Source = rvImages.GetBitmap("default1");
            imgDefault2.Source = rvImages.GetBitmap("default2");
            imgDefault3.Source = rvImages.GetBitmap("default3");
            imgDefault4.Source = rvImages.GetBitmap("default4");

            AddGameMetaData();
            Title = $"RomVault ({Program.strVersion})";

            _displayColor = new Color[(int)RepStatus.EndValue];
            _fontColor = new Color[(int)RepStatus.EndValue];

            _displayColor[(int)RepStatus.UnScanned] = CBlue;
            _displayColor[(int)RepStatus.DirCorrect] = CGreen;
            _displayColor[(int)RepStatus.DirMissing] = CRed;
            _displayColor[(int)RepStatus.DirCorrupt] = CBrightRed;
            _displayColor[(int)RepStatus.Missing] = CRed;
            _displayColor[(int)RepStatus.Correct] = CGreen;
            _displayColor[(int)RepStatus.CorrectMIA] = CNeonGreen;
            _displayColor[(int)RepStatus.NotCollected] = CGrey;
            _displayColor[(int)RepStatus.UnNeeded] = CCyanGrey;
            _displayColor[(int)RepStatus.Unknown] = CCyan;
            _displayColor[(int)RepStatus.InToSort] = CMagenta;
            _displayColor[(int)RepStatus.MissingMIA] = CSoftGreen;
            _displayColor[(int)RepStatus.Corrupt] = CBrightRed;
            _displayColor[(int)RepStatus.Ignore] = CGreyBlue;
            _displayColor[(int)RepStatus.CanBeFixed] = CYellow;
            _displayColor[(int)RepStatus.CanBeFixedMIA] = CDarkYellow;
            _displayColor[(int)RepStatus.MoveToSort] = CPurple;
            _displayColor[(int)RepStatus.Delete] = CBrown;
            _displayColor[(int)RepStatus.NeededForFix] = COrange;
            _displayColor[(int)RepStatus.Rename] = COrange;
            _displayColor[(int)RepStatus.CorruptCanBeFixed] = CYellow;
            _displayColor[(int)RepStatus.MoveToCorrupt] = CPurple;
            _displayColor[(int)RepStatus.Incomplete] = CLightRed;
            _displayColor[(int)RepStatus.Deleted] = CWhite;

            for (int i = 0; i < (int)RepStatus.EndValue; i++)
            {
                _fontColor[i] = Contrasty(_displayColor[i]);
            }

            _gameGridColumnXPositions = new int[(int)RepStatus.EndValue];

            // Bind DataGrids to observable collections
            GameGrid.ItemsSource = _gameGridItems;
            RomGrid.ItemsSource = _romGridItems;

            // Wire up DataGrid events
            GameGrid.SelectionChanged += GameGridSelectionChanged;
            GameGrid.DoubleTapped += GameGridMouseDoubleClick;
            GameGrid.LoadingRow += GameGridLoadingRow;
            GameGrid.Sorting += GameGridSorting;

            RomGrid.SelectionChanged += RomGridSelectionChanged;
            RomGrid.LoadingRow += RomGridLoadingRow;
            RomGrid.Sorting += RomGridSorting;

            // Attach pointer events for context menus
            GameGrid.AddHandler(PointerReleasedEvent, GameGridPointerReleased, RoutingStrategies.Tunnel);
            RomGrid.AddHandler(PointerReleasedEvent, RomGridPointerReleased, RoutingStrategies.Tunnel);

            // Focus DataGrids on pointer enter so mouse wheel scrolling works
            GameGrid.PointerEntered += (s, e) => GameGrid.Focus();
            RomGrid.PointerEntered += (s, e) => RomGrid.Focus();

            ctrRvTree.Setup(ref DB.DirRoot);

            // Focus tree on pointer enter so mouse wheel scrolling works
            ctrRvTree.PointerEntered += (s, e) => ctrRvTree.Focus();

            // Set up tree events
            ctrRvTree.RvSelected += DirTreeRvSelected;
            ctrRvTree.RvChecked += DirTreeRvChecked;

            // Build context menus
            BuildContextMenus();

            // Wire checkbox events
            chkBoxShowComplete.IsCheckedChanged += ChkBoxShowCompleteCheckedChanged;
            chkBoxShowPartial.IsCheckedChanged += ChkBoxShowPartialCheckedChanged;
            chkBoxShowEmpty.IsCheckedChanged += chkBoxShowEmptyCheckedChanged;
            chkBoxShowFixes.IsCheckedChanged += ChkBoxShowFixesCheckedChanged;
            chkBoxShowMIA.IsCheckedChanged += chkBoxShowMIA_CheckedChanged;
            chkBoxShowMerged.IsCheckedChanged += ChkBoxShowMergedCheckedChanged;

            // Wire filter events
            txtFilter.TextChanged += TxtFilter_TextChanged;
            btnClear.Click += BtnClear_Click;

            // Wire button events
            btnUpdateDats.Click += BtnUpdateDatsClick;
            btnUpdateDats.AddHandler(PointerReleasedEvent, BtnUpdateDatsPointerReleased, RoutingStrategies.Tunnel);
            btnScanRoms.Click += BtnScanRomsClick;
            btnFindFixes.Click += btnFindFixes_Click;
            btnFindFixes.AddHandler(PointerReleasedEvent, BtnFindFixesPointerReleased, RoutingStrategies.Tunnel);
            btnFixFiles.Click += BtnFixFilesClick;
            btnFixFiles.AddHandler(PointerReleasedEvent, BtnFixFilesPointerReleased, RoutingStrategies.Tunnel);
            btnReport.AddHandler(PointerReleasedEvent, BtnReportPointerReleased, RoutingStrategies.Tunnel);

            // Default buttons
            btnDefault1.AddHandler(PointerReleasedEvent, (s, e) => { treeDefault(e.InitialPressMouseButton == MouseButton.Right, 1); }, RoutingStrategies.Tunnel);
            btnDefault2.AddHandler(PointerReleasedEvent, (s, e) => { treeDefault(e.InitialPressMouseButton == MouseButton.Right, 2); }, RoutingStrategies.Tunnel);
            btnDefault3.AddHandler(PointerReleasedEvent, (s, e) => { treeDefault(e.InitialPressMouseButton == MouseButton.Right, 3); }, RoutingStrategies.Tunnel);
            btnDefault4.AddHandler(PointerReleasedEvent, (s, e) => { treeDefault(e.InitialPressMouseButton == MouseButton.Right, 4); }, RoutingStrategies.Tunnel);

            // Wire menu items
            updateNewDATsToolStripMenuItem.Click += updateNewDATsToolStripMenuItem_Click;
            updateAllDATsToolStripMenuItem.Click += updateAllDATsToolStripMenuItem_Click;
            tsmScanLevel1.Click += TsmScanLevel1Click;
            tsmScanLevel2.Click += TsmScanLevel2Click;
            tsmScanLevel3.Click += TsmScanLevel3Click;
            tsmFindFixes.Click += TsmFindFixesClick;
            FixROMsToolStripMenuItem.Click += FixFilesToolStripMenuItemClick;
            fixDatReportToolStripMenuItem.Click += fixDatReportToolStripMenuItem_Click;
            fullReportToolStripMenuItem.Click += fullReportToolStripMenuItem_Click;
            fixReportToolStripMenuItem.Click += fixReportToolStripMenuItem_Click;
            romVaultSettingsToolStripMenuItem.Click += RomVaultSettingsToolStripMenuItem_Click;
            directorySettingsToolStripMenuItem.Click += DirectorySettingsToolStripMenuItem_Click;
            directoryMappingsToolStripMenuItem.Click += directoryMappingsToolStripMenuItem_Click;
            addToSortToolStripMenuItem.Click += AddToSortToolStripMenuItem_Click;
            torrentZipToolStripMenuItem.Click += torrentZipToolStripMenuItem_Click;
            visitHelpWikiToolStripMenuItem.Click += visitHelpWikiToolStripMenuItem_Click;
            colorKeyToolStripMenuItem.Click += colorKeyToolStripMenuItem_Click;
            whatsNewToolStripMenuItem.Click += whatsNewToolStripMenuItem_Click;
            aboutRomVaultToolStripMenuItem.Click += AboutRomVaultToolStripMenuItemClick;

            // Load saved checkbox states
            chkBoxShowComplete.IsChecked = Settings.rvSettings.chkBoxShowComplete;
            chkBoxShowPartial.IsChecked = Settings.rvSettings.chkBoxShowPartial;
            chkBoxShowFixes.IsChecked = Settings.rvSettings.chkBoxShowFixes;
            chkBoxShowMIA.IsChecked = Settings.rvSettings.chkBoxShowMIA;
            chkBoxShowMerged.IsChecked = Settings.rvSettings.chkBoxShowMerged;

            TabArtworkInitialize();

            InitGameGridMenu();

            // Setup timer
            _timer1 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(8000) };
            _timer1.Tick += timer1_Tick;

            // Tooltips
            ToolTip.SetTip(btnDefault1, "Right Click: Save Tree Settings\nLeft Click: Load Tree Settings");
            ToolTip.SetTip(btnDefault2, "Right Click: Save Tree Settings\nLeft Click: Load Tree Settings");
            ToolTip.SetTip(btnDefault3, "Right Click: Save Tree Settings\nLeft Click: Load Tree Settings");
            ToolTip.SetTip(btnDefault4, "Right Click: Save Tree Settings\nLeft Click: Load Tree Settings");
            ToolTip.SetTip(btnUpdateDats, "Left Click: Dat Update\nShift Left Click: Full Dat Rescan\n\nRight Click: Open DatVault");
            ToolTip.SetTip(btnFixFiles, "Left Click: Fix Files\nRight Click: Scan / Find Fix / Fix");
        }

        // returns either white or black, depending on quick luminance of the Color
        internal static Color Contrasty(Color a)
        {
            return (a.R << 1) + a.B + a.G + (a.G << 2) < 1024 ? Colors.White : Colors.Black;
        }

        #endregion

        #region ContextMenus

        private void BuildContextMenus()
        {
            // Main tree context menu
            _mnuContext = new ContextMenu();

            var mnuScan1 = new MenuItem { Header = "Scan Quick (Headers Only)", Tag = EScanLevel.Level1 };
            var mnuScan2 = new MenuItem { Header = "Scan", Tag = EScanLevel.Level2 };
            var mnuScan3 = new MenuItem { Header = "Scan Full (Complete Re-Scan)", Tag = EScanLevel.Level3 };
            var mnuDirDatSettings = new MenuItem { Header = "Set Dir Dat Settings" };
            var mnuDirMappings = new MenuItem { Header = "Set Dir Mappings" };
            _mnuOpen = new MenuItem { Header = "Open Directory" };
            var mnuFixDat = new MenuItem { Header = "Save fix DATs" };
            var mnuMakeDat = new MenuItem { Header = "Save full DAT" };

            mnuScan1.Click += MnuScan;
            mnuScan2.Click += MnuScan;
            mnuScan3.Click += MnuScan;
            mnuDirDatSettings.Click += MnuDirSettings;
            mnuDirMappings.Click += MnuDirMappings;
            _mnuOpen.Click += MnuOpenClick;
            mnuFixDat.Click += MnuMakeFixDatClick;
            mnuMakeDat.Click += MnuMakeDatClick;

            _mnuContext.Items.Add(mnuScan2);
            _mnuContext.Items.Add(mnuScan1);
            _mnuContext.Items.Add(mnuScan3);
            _mnuContext.Items.Add(mnuDirDatSettings);
            _mnuContext.Items.Add(mnuDirMappings);
            _mnuContext.Items.Add(new Separator());
            _mnuContext.Items.Add(_mnuOpen);
            _mnuContext.Items.Add(mnuFixDat);
            _mnuContext.Items.Add(mnuMakeDat);

            // ToSort context menu
            _mnuContextToSort = new ContextMenu();

            var mnuToSortScan1 = new MenuItem { Header = "Scan Quick (Headers Only)", Tag = EScanLevel.Level1 };
            var mnuToSortScan2 = new MenuItem { Header = "Scan", Tag = EScanLevel.Level2 };
            var mnuToSortScan3 = new MenuItem { Header = "Scan Full (Complete Re-Scan)", Tag = EScanLevel.Level3 };
            _mnuToSortOpen = new MenuItem { Header = "Open ToSort Directory" };
            _mnuToSortDelete = new MenuItem { Header = "Remove" };
            _mnuToSortSetPrimary = new MenuItem { Header = "Set To Primary ToSort" };
            _mnuToSortSetCache = new MenuItem { Header = "Set To Cache ToSort" };
            _mnuToSortSetFileOnly = new MenuItem { Header = "Set To File Only ToSort" };
            _mnuToSortClearFileOnly = new MenuItem { Header = "Clear File Only ToSort" };
            _mnuToSortUp = new MenuItem { Header = "Move Up" };
            _mnuToSortDown = new MenuItem { Header = "Move Down" };

            mnuToSortScan1.Click += MnuScan;
            mnuToSortScan2.Click += MnuScan;
            mnuToSortScan3.Click += MnuScan;
            _mnuToSortOpen.Click += MnuToSortOpen;
            _mnuToSortDelete.Click += MnuToSortDelete;
            _mnuToSortSetPrimary.Click += MnuToSortSetPrimary;
            _mnuToSortSetCache.Click += MnuToSortSetCache;
            _mnuToSortSetFileOnly.Click += MnuToSortSetFileOnly;
            _mnuToSortClearFileOnly.Click += MnuToSortClearFileOnly;
            _mnuToSortUp.Click += MnuToSortUp;
            _mnuToSortDown.Click += MnuToSortDown;

            _mnuContextToSort.Items.Add(mnuToSortScan2);
            _mnuContextToSort.Items.Add(mnuToSortScan1);
            _mnuContextToSort.Items.Add(mnuToSortScan3);
            _mnuContextToSort.Items.Add(_mnuToSortOpen);
            _mnuContextToSort.Items.Add(new Separator());
            _mnuContextToSort.Items.Add(_mnuToSortSetPrimary);
            _mnuContextToSort.Items.Add(_mnuToSortSetCache);
            _mnuContextToSort.Items.Add(_mnuToSortSetFileOnly);
            _mnuContextToSort.Items.Add(_mnuToSortClearFileOnly);
            _mnuContextToSort.Items.Add(_mnuToSortDelete);
            _mnuContextToSort.Items.Add(new Separator());
            _mnuContextToSort.Items.Add(_mnuToSortUp);
            _mnuContextToSort.Items.Add(_mnuToSortDown);
        }

        #endregion

        #region Tree

        private void DirTreeRvChecked(object sender, RvTreeEventArgs e)
        {
            RepairStatus.ReportStatusReset(DB.DirRoot);
            DatSetSelected(ctrRvTree.Selected);
        }

        private void DirTreeRvSelected(object sender, RvTreeEventArgs e)
        {
            RvFile cf = e.RvFile;

            if (e.PointerArgs.InitialPressMouseButton != MouseButton.Right)
            {
                if (cf != gameGridSource)
                {
                    DatSetSelected(cf);
                }
                return;
            }

            if (cf != ctrRvTree.Selected)
            {
                DatSetSelected(cf);
            }

            _clickedTree = cf;

            if (_working)
                return;

            if (cf.IsInToSort)
            {
                _mnuToSortOpen.IsEnabled = Directory.Exists(_clickedTree.FullName);
                _mnuToSortDelete.IsEnabled = !(_clickedTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortPrimary) || _clickedTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortCache));
                _mnuToSortSetCache.IsVisible = !(_clickedTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortCache) || _clickedTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortFileOnly));
                _mnuToSortSetPrimary.IsVisible = !(_clickedTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortPrimary) || _clickedTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortFileOnly));
                _mnuToSortSetFileOnly.IsVisible = !(_clickedTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortFileOnly) || _clickedTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortPrimary) || _clickedTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortCache));
                _mnuToSortClearFileOnly.IsVisible = _clickedTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortFileOnly);

                int thisToSort = 0;
                for (int i = 0; i < DB.DirRoot.ChildCount; i++)
                {
                    if (DB.DirRoot.Child(i) == cf)
                    {
                        thisToSort = i;
                        break;
                    }
                }
                _mnuToSortUp.IsEnabled = thisToSort >= 2;
                _mnuToSortDown.IsEnabled = thisToSort <= DB.DirRoot.ChildCount - 2;

                _mnuContextToSort.Open(ctrRvTree);
            }
            else
            {
                _mnuOpen.IsEnabled = Directory.Exists(_clickedTree.FullName);
                _mnuContext.Open(ctrRvTree);
            }
        }

        #endregion

        #region PopupMenus

        private void MnuScan(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is EScanLevel level)
                ScanRoms(level, _clickedTree);
        }

        private async void MnuDirSettings(object sender, RoutedEventArgs e)
        {
            var fDirSettings = new FrmDirectorySettings();
            string tDir = _clickedTree.TreeFullName;
            fDirSettings.SetLocation(tDir);
            fDirSettings.SetDisplayType(true);
            await fDirSettings.ShowDialog(this);

            if (fDirSettings.ChangesMade)
                await UpdateDats();
        }

        private void MnuDirMappings(object sender, RoutedEventArgs e)
        {
            var fDirMappings = new FrmDirectoryMappings();
            string tDir = _clickedTree.TreeFullName;
            fDirMappings.SetLocation(tDir);
            fDirMappings.SetDisplayType(true);
            fDirMappings.ShowDialog(this);
        }

        private void MnuOpenClick(object sender, RoutedEventArgs e)
        {
            string tDir = _clickedTree.FullName;
            if (Directory.Exists(tDir))
                OpenFolder(tDir);
        }

        private void MnuMakeFixDatClick(object sender, RoutedEventArgs e)
        {
            MakeFixDat(_clickedTree, true);
        }

        private async void MakeFixDat(RvFile baseDir, bool redOnly)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Please select fixdat files destination. NOTE: " +
                        (redOnly
                            ? "reports will include Missing & MIA items only (omitting any Fixable items that may be present)"
                            : "reports will include both Missing, MIA and Fixable items"),
                Directory = Settings.rvSettings.FixDatOutPath
            };

            string result = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(result))
                return;

            if (!Directory.Exists(result))
                return;

            if (result != Settings.rvSettings.FixDatOutPath)
            {
                Settings.rvSettings.FixDatOutPath = result;
                Settings.WriteConfig(Settings.rvSettings);
            }

            FixDatReport.RecursiveDatTree(Settings.rvSettings.FixDatOutPath, baseDir, redOnly);
        }

        private async void MnuMakeDatClick(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                DefaultExtension = "dat",
                Title = "Save a Dat File",
                InitialFileName = _clickedTree.Name
            };
            dialog.Filters.Add(new FileDialogFilter { Name = "DAT file", Extensions = { "dat" } });

            string result = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(result))
                return;

            DatHeader dh = (new ExternalDatConverterTo()).ConvertToExternalDat(_clickedTree);
            DatXMLWriter.WriteDat(result, dh);
        }

        private void MnuToSortOpen(object sender, RoutedEventArgs e)
        {
            string tDir = _clickedTree.FullName;
            if (Directory.Exists(tDir))
                OpenFolder(tDir);
        }

        private void MnuToSortDelete(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < DB.DirRoot.ChildCount; i++)
            {
                if (DB.DirRoot.Child(i) == _clickedTree)
                {
                    DB.DirRoot.ChildRemove(i);
                    RepairStatus.ReportStatusReset(DB.DirRoot);
                    ctrRvTree.Setup(ref DB.DirRoot);
                    DatSetSelected(DB.DirRoot.Child(i - 1));
                    DB.Write();
                    ctrRvTree.InvalidateVisual();
                    return;
                }
            }
        }

        private void MnuToSortSetPrimary(object sender, RoutedEventArgs e)
        {
            if (_clickedTree.Tree.Checked == RvTreeRow.TreeSelect.Locked)
            {
                _clickedTree.Tree.SetChecked(RvTreeRow.TreeSelect.Selected, true);
            }

            RvFile t = DB.GetToSortPrimary();
            bool wasCache = t.ToSortStatusIs(RvFile.ToSortDirType.ToSortCache);
            t.ToSortStatusClear(RvFile.ToSortDirType.ToSortPrimary | RvFile.ToSortDirType.ToSortCache);

            _clickedTree.ToSortStatusSet(RvFile.ToSortDirType.ToSortPrimary);
            if (wasCache)
                _clickedTree.ToSortStatusSet(RvFile.ToSortDirType.ToSortCache);

            DB.Write();
            ctrRvTree.InvalidateVisual();
        }

        private void MnuToSortSetCache(object sender, RoutedEventArgs e)
        {
            if (_clickedTree.Tree.Checked == RvTreeRow.TreeSelect.Locked)
            {
                _clickedTree.Tree.SetChecked(RvTreeRow.TreeSelect.Selected, true);
            }

            RvFile t = DB.GetToSortCache();
            t.ToSortStatusClear(RvFile.ToSortDirType.ToSortCache);

            _clickedTree.ToSortStatusSet(RvFile.ToSortDirType.ToSortCache);

            DB.Write();
            ctrRvTree.InvalidateVisual();
        }

        private void MnuToSortSetFileOnly(object sender, RoutedEventArgs e)
        {
            if (_clickedTree.Tree.Checked == RvTreeRow.TreeSelect.Locked)
            {
                _clickedTree.Tree.SetChecked(RvTreeRow.TreeSelect.Selected, true);
            }
            if (_clickedTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortPrimary))
                return;
            if (_clickedTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortCache))
                return;

            _clickedTree.ToSortStatusSet(RvFile.ToSortDirType.ToSortFileOnly);

            DB.Write();
            ctrRvTree.InvalidateVisual();
        }

        private void MnuToSortClearFileOnly(object sender, RoutedEventArgs e)
        {
            _clickedTree.ToSortStatusClear(RvFile.ToSortDirType.ToSortFileOnly);
            ctrRvTree.Setup(ref DB.DirRoot);
            DB.Write();
        }

        private void MnuToSortUp(object sender, RoutedEventArgs e)
        {
            DB.MoveToSortUp(_clickedTree);
            ctrRvTree.Setup(ref DB.DirRoot);
            DB.Write();
        }

        private void MnuToSortDown(object sender, RoutedEventArgs e)
        {
            DB.MoveToSortDown(_clickedTree);
            ctrRvTree.Setup(ref DB.DirRoot);
            DB.Write();
        }

        #endregion

        #region TopMenu

        private async void updateNewDATsToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_working) return;
            await UpdateDats();
        }

        private async void updateAllDATsToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_working) return;
            DatUpdate.CheckAllDats(DB.DirRoot.Child(0), @"DatRoot" + System.IO.Path.DirectorySeparatorChar);
            await UpdateDats();
        }

        private async void AddToSortToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_working) return;

            var dialog = new OpenFolderDialog
            {
                Title = "Select new ToSort Folder"
            };

            string result = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(result))
                return;

            string relPath = RelativePath.MakeRelative(AppDomain.CurrentDomain.BaseDirectory, result);

            RvFile ts = new RvFile(FileType.Dir)
            {
                Name = relPath,
                DatStatus = DatStatus.InToSort,
                Tree = new RvTreeRow()
            };
            ts.Tree.SetChecked(RvTreeRow.TreeSelect.Locked, false);

            DB.DirRoot.ChildAdd(ts, DB.DirRoot.ChildCount);

            RepairStatus.ReportStatusReset(DB.DirRoot);
            ctrRvTree.Setup(ref DB.DirRoot);
            DatSetSelected(ts);

            DB.Write();
        }

        private void TsmScanLevel1Click(object sender, RoutedEventArgs e)
        {
            if (_working) return;
            ScanRoms(EScanLevel.Level1);
        }

        private void TsmScanLevel2Click(object sender, RoutedEventArgs e)
        {
            if (_working) return;
            ScanRoms(EScanLevel.Level2);
        }

        private void TsmScanLevel3Click(object sender, RoutedEventArgs e)
        {
            if (_working) return;
            ScanRoms(EScanLevel.Level3);
        }

        private void TsmFindFixesClick(object sender, RoutedEventArgs e)
        {
            if (_working) return;
            FindFixes();
        }

        private void FixFilesToolStripMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (_working) return;
            FixFiles();
        }

        private void RomVaultSettingsToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_working) return;
            var fcfg = new FrmSettings();
            fcfg.ShowDialog(this);
        }

        private async void DirectorySettingsToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_working) return;
            var sd = new FrmDirectorySettings();
            string tDir = "RomVault";
            sd.SetLocation(tDir);
            sd.SetDisplayType(false);
            await sd.ShowDialog(this);

            if (sd.ChangesMade)
                await UpdateDats();
        }

        private void directoryMappingsToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_working) return;
            var sd = new FrmDirectoryMappings();
            string tDir = "RomVault";
            sd.SetLocation(tDir);
            sd.SetDisplayType(false);
            sd.ShowDialog(this);
        }

        private void fixDatReportToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_working) return;
            MakeFixDat(DB.DirRoot.Child(0), true);
        }

        private void fullReportToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_working) return;
            Report.GenerateReport();
        }

        private void fixReportToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_working) return;
            Report.GenerateFixReport();
        }

        private void colorKeyToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_fk == null || !_fk.IsVisible)
            {
                _fk = new FrmKey();
            }
            _fk.Show(this);
        }

        private void AboutRomVaultToolStripMenuItemClick(object sender, RoutedEventArgs e)
        {
            var fha = new FrmHelpAbout();
            fha.ShowDialog(this);
        }

        private void visitHelpWikiToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://wiki.romvault.com/doku.php?id=help");
        }

        private void whatsNewToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://wiki.romvault.com/doku.php?id=whats_new");
        }

        private void torrentZipToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var trrntZipWindow = new FrmTrrntzip();
            trrntZipWindow.Show(this);
        }

        #endregion

        #region SideButtons

        private void BtnUpdateDatsClick(object sender, RoutedEventArgs e)
        {
            // handled by pointer released for shift detection
        }

        private async void BtnUpdateDatsPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                DatUpdate.CheckAllDats(DB.DirRoot.Child(0), @"DatRoot" + System.IO.Path.DirectorySeparatorChar);
            }
            RootDirsCreate.CheckDatRoot();
            Start();
            await UpdateDats();
            Finish();
        }

        private void BtnScanRomsClick(object sender, RoutedEventArgs e)
        {
            ScanRoms(EScanLevel.Level2);
        }

        private void btnFindFixes_Click(object sender, RoutedEventArgs e)
        {
            // handled by pointer released
        }

        private void BtnFindFixesPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            bool showLog = e.KeyModifiers.HasFlag(KeyModifiers.Shift) && e.KeyModifiers.HasFlag(KeyModifiers.Control);
            FindFixes(showLog);
        }

        private void BtnFixFilesClick(object sender, RoutedEventArgs e)
        {
            // handled by pointer released
        }

        private void BtnFixFilesPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                Automate.AutoScanFix();
                return;
            }

            FixFiles();
        }

        private void BtnReportPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            MakeFixDat(DB.DirRoot.Child(0), e.InitialPressMouseButton == MouseButton.Left);
        }

        #endregion

        #region TopRight

        private void ChkBoxShowCompleteCheckedChanged(object sender, RoutedEventArgs e)
        {
            bool val = chkBoxShowComplete.IsChecked == true;
            if (Settings.rvSettings.chkBoxShowComplete != val)
            {
                Settings.rvSettings.chkBoxShowComplete = val;
                Settings.WriteConfig(Settings.rvSettings);
                DatSetSelected(ctrRvTree.Selected);
            }
        }

        private void ChkBoxShowPartialCheckedChanged(object sender, RoutedEventArgs e)
        {
            bool val = chkBoxShowPartial.IsChecked == true;
            if (Settings.rvSettings.chkBoxShowPartial != val)
            {
                Settings.rvSettings.chkBoxShowPartial = val;
                Settings.WriteConfig(Settings.rvSettings);
                DatSetSelected(ctrRvTree.Selected);
            }
        }

        private void chkBoxShowEmptyCheckedChanged(object sender, RoutedEventArgs e)
        {
            bool val = chkBoxShowEmpty.IsChecked == true;
            if (Settings.rvSettings.chkBoxShowEmpty != val)
            {
                Settings.rvSettings.chkBoxShowEmpty = val;
                Settings.WriteConfig(Settings.rvSettings);
                DatSetSelected(ctrRvTree.Selected);
            }
        }

        private void ChkBoxShowFixesCheckedChanged(object sender, RoutedEventArgs e)
        {
            bool val = chkBoxShowFixes.IsChecked == true;
            if (Settings.rvSettings.chkBoxShowFixes != val)
            {
                Settings.rvSettings.chkBoxShowFixes = val;
                Settings.WriteConfig(Settings.rvSettings);
                DatSetSelected(ctrRvTree.Selected);
            }
        }

        private void chkBoxShowMIA_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool val = chkBoxShowMIA.IsChecked == true;
            if (Settings.rvSettings.chkBoxShowMIA != val)
            {
                Settings.rvSettings.chkBoxShowMIA = val;
                Settings.WriteConfig(Settings.rvSettings);
                DatSetSelected(ctrRvTree.Selected);
            }
        }

        private void ChkBoxShowMergedCheckedChanged(object sender, RoutedEventArgs e)
        {
            bool val = chkBoxShowMerged.IsChecked == true;
            if (Settings.rvSettings.chkBoxShowMerged != val)
            {
                Settings.rvSettings.chkBoxShowMerged = val;
                Settings.WriteConfig(Settings.rvSettings);
                DatSetSelected(ctrRvTree.Selected);
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            txtFilter.Text = "";
        }

        private void TxtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (gameGridSource != null)
                UpdateGameGrid(gameGridSource);
            txtFilter.Focus();
        }

        #endregion

        #region CoreFunctions

        public async System.Threading.Tasks.Task UpdateDats()
        {
            RvFile selected = ctrRvTree.Selected;
            List<RvFile> parents = new List<RvFile>();
            while (selected != null)
            {
                parents.Add(selected);
                selected = selected.Parent;
            }

            FrmProgressWindow progress = new FrmProgressWindow(this, "Scanning Dats", DatUpdate.UpdateDat, null);
            progress.HideCancelButton();
            await progress.ShowDialog(this);

            ctrRvTree.Setup(ref DB.DirRoot);

            while (parents.Count > 1 && parents[0].Parent == null)
                parents.RemoveAt(0);

            if (parents.Count > 0)
                selected = parents[0];
            else
                selected = null;

            ctrRvTree.SetSelected(selected);
            DatSetSelected(selected);
        }

        public FrmProgressWindow frmScanRoms;
        public void ScanRoms(EScanLevel sd, RvFile StartAt = null, EventHandler closedHandler = null)
        {
            FileScanning.StartAt = StartAt;
            FileScanning.EScanLevel = sd;
            frmScanRoms = new FrmProgressWindow(this, "Scanning Dirs", FileScanning.ScanFiles, Finish);
            Start();
            if (closedHandler != null)
                frmScanRoms.Closed += closedHandler;
            frmScanRoms.Show(this);
        }

        public FrmProgressWindow frmFindFixes;
        public void FindFixes(bool showLog = false, EventHandler closedHandler = null)
        {
            frmFindFixes = new FrmProgressWindow(this, "Finding Fixes", RomVaultCore.FindFix.FindFixes.ScanFiles, Finish);
            frmFindFixes.ShowTimeLog = showLog;
            Start();
            if (closedHandler != null)
                frmFindFixes.Closed += closedHandler;
            frmFindFixes.Show(this);
        }

        FrmProgressWindowFix frmFixFiles;
        public void FixFiles(bool closeOnExit = false, EventHandler closedHandler = null)
        {
            frmFixFiles = new FrmProgressWindowFix(this, closeOnExit, Finish);
            Start();
            if (closedHandler != null)
                frmFixFiles.Closed += closedHandler;
            frmFixFiles.Show(this);
        }

        internal bool _working = false;
        private void Start()
        {
            _working = true;
            _timer1.IsEnabled = true;
            ctrRvTree.Working = true;

            foreach (var item in menuStrip1.Items)
            {
                if (item is MenuItem menuItem)
                {
                    if (menuItem.Header?.ToString() == "Help")
                        continue;
                    menuItem.IsEnabled = false;
                }
            }

            btnUpdateDats.IsEnabled = false;
            btnScanRoms.IsEnabled = false;
            btnFindFixes.IsEnabled = false;
            btnFixFiles.IsEnabled = false;
            btnReport.IsEnabled = false;
            btnDefault1.IsEnabled = false;
            btnDefault2.IsEnabled = false;
            btnDefault3.IsEnabled = false;
            btnDefault4.IsEnabled = false;

            imgUpdateDats.Source = rvImages.GetBitmap("btnUpdateDats_Disabled");
            imgScanRoms.Source = rvImages.GetBitmap("btnScanRoms_Disabled");
            imgFindFixes.Source = rvImages.GetBitmap("btnFindFixes_Disabled");
            imgFixFiles.Source = rvImages.GetBitmap("btnFixFiles_Disabled");
            imgReport.Source = rvImages.GetBitmap("btnReport_Disabled");
        }

        private void Finish()
        {
            _working = false;
            ctrRvTree.Working = false;

            foreach (var item in menuStrip1.Items)
            {
                if (item is MenuItem menuItem)
                    menuItem.IsEnabled = true;
            }

            imgUpdateDats.Source = rvImages.GetBitmap("btnUpdateDats_Enabled");
            imgScanRoms.Source = rvImages.GetBitmap("btnScanRoms_Enabled");
            imgFindFixes.Source = rvImages.GetBitmap("btnFindFixes_Enabled");
            imgFixFiles.Source = rvImages.GetBitmap("btnFixFiles_Enabled");
            imgReport.Source = rvImages.GetBitmap("btnReport_Enabled");

            btnDefault1.IsEnabled = true;
            btnDefault2.IsEnabled = true;
            btnDefault3.IsEnabled = true;
            btnDefault4.IsEnabled = true;
            btnUpdateDats.IsEnabled = true;
            btnScanRoms.IsEnabled = true;
            btnFindFixes.IsEnabled = true;
            btnFixFiles.IsEnabled = true;
            btnReport.IsEnabled = true;

            _timer1.IsEnabled = false;

            // Mirror WinForms completion behavior, but defensively handle stale/invalid
            // selected nodes so a scan completion cannot crash the UI.
            try
            {
                RvFile selected = ctrRvTree.Selected;

                // If selection no longer maps to a visible tree node, clear selection and
                // refresh from root to keep tree/grid in a valid state.
                if (selected != null && selected.Tree == null)
                {
                    ctrRvTree.SetSelected(null);
                    selected = null;
                }

                DatSetSelected(selected);
            }
            catch
            {
                DatSetSelected(null);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            ctrRvTree.InvalidateVisual();
            UpdateGameGrid(true);
            if (ctrRvTree.Selected != null)
                UpdateDatMetaData(ctrRvTree.Selected);
        }

        #endregion

        #region DatDisplay

        private void DatSetSelected(RvFile cf)
        {
            ctrRvTree.InvalidateVisual();
            ClearGameGrid();

            if (cf == null)
                return;

            UpdateDatMetaData(cf);
            UpdateGameGrid(cf);
        }

        private void UpdateDatMetaData(RvFile tDir)
        {
            lblDITName.Text = tDir.Name;

            RvDat tDat = null;
            if (tDir.Dat != null)
                tDat = tDir.Dat;
            else if (tDir.DirDatCount == 1)
                tDat = tDir.DirDat(0);

            if (tDat != null)
            {
                if (lblDITName.Text != tDat.GetData(RvDat.DatData.DatName))
                    lblDITName.Text += $":  {tDat.GetData(RvDat.DatData.DatName)}";

                string DatId = tDat.GetData(RvDat.DatData.Id);
                if (!string.IsNullOrWhiteSpace(DatId))
                    lblDITName.Text += $" (ID:{DatId})";

                lblDITDescription.Text = tDat.GetData(RvDat.DatData.Description);
                lblDITCategory.Text = tDat.GetData(RvDat.DatData.Category);
                lblDITVersion.Text = tDat.GetData(RvDat.DatData.Version);
                lblDITAuthor.Text = tDat.GetData(RvDat.DatData.Author);
                lblDITDate.Text = tDat.GetData(RvDat.DatData.Date);
                string header = tDat.GetData(RvDat.DatData.Header);
                if (!string.IsNullOrWhiteSpace(header))
                    lblDITName.Text += " (" + header + ")";
            }
            else
            {
                lblDITDescription.Text = "";
                lblDITCategory.Text = "";
                lblDITVersion.Text = "";
                lblDITAuthor.Text = "";
                lblDITDate.Text = "";
            }

            lblDITPath.Text = tDir.FullName;

            lblDITRomsGot.Text = tDir.DirStatus.CountCorrect().ToString(CultureInfo.InvariantCulture);
            if (tDir.DirStatus.CountFoundMIA() > 0) { lblDITRomsGot.Text += $"  -  {tDir.DirStatus.CountFoundMIA()} Found MIA"; }

            lblDITRomsMissing.Text = tDir.DirStatus.CountMissing().ToString(CultureInfo.InvariantCulture);
            if (tDir.DirStatus.CountMIA() > 0) { lblDITRomsMissing.Text += $"  -  {tDir.DirStatus.CountMIA()} MIA"; }

            lblDITRomsFixable.Text = tDir.DirStatus.CountFixesNeeded().ToString(CultureInfo.InvariantCulture);
            lblDITRomsUnknown.Text = (tDir.DirStatus.CountUnknown() + tDir.DirStatus.CountInToSort()).ToString(CultureInfo.InvariantCulture);
        }

        #endregion

        #region Defaults

        public void treeDefault(bool set, int index)
        {
            DatTreeStatusStore dtss = new DatTreeStatusStore();
            if (set)
            {
                dtss.write(index);
                return;
            }
            dtss.read(index);
            ctrRvTree.Setup(ref DB.DirRoot, true);
        }

        #endregion

        #region Helpers

        internal static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }

        internal static void OpenFolder(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch { }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (_working)
            {
                e.Cancel = true;
                return;
            }

            if (_fk != null && _fk.IsVisible)
                _fk.Close();

            base.OnClosing(e);
            Environment.Exit(0);
        }

        #endregion
    }
}

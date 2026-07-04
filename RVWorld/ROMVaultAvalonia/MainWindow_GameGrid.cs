/******************************************************
 *     ROMVault3 is written by Gordon J.              *
 *     Contact gordon@romvault.com                    *
 *     Copyright 2025                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Compress;
using DarkAvalonia;
using System.Reflection;
using RomVaultCore;
using RomVaultCore.RvDB;
using StorageList;
using RVIO;

namespace ROMVault
{
    public enum GameGridColumns
    {
        CType = 0,
        CGame = 1,
        CDescription = 2,
        CDateTime = 3,
        CRomStatus = 4
    }

    /// <summary>
    /// View-model item for each row in the GameGrid DataGrid.
    /// Properties are bound in XAML via DataGridTextColumn / DataGridTemplateColumn.
    /// </summary>
    public class GameGridItem
    {
        public RvFile RvFile { get; set; }

        public Bitmap TypeBitmap { get; set; }
        public string GameName { get; set; }
        public string Description { get; set; }
        public string DateTime { get; set; }
        public Bitmap StatusBitmap { get; set; }

        // Color info for row formatting
        public Color BackColor { get; set; }
        public Color ForeColor { get; set; }
    }

    public partial class MainWindow
    {
        internal RvFile gameGridSource;
        private RvFile[] gameGrid;

        private int gameSortIndex = -1;
        private bool gameSortAscending = true;

        private bool showDescription;

        // Game grid context menu
        private ContextMenu _mnuGameGrid;
        private MenuItem mnuGameScan1;
        private MenuItem mnuGameScan2;
        private MenuItem mnuGameScan3;
        private MenuItem mnuOpenDir;
        private MenuItem mnuOpenParentDir;
        private MenuItem mnuLaunchEmulator;
        private MenuItem mnuOpenPage;

        private RvFile _gameGridContextFile; // the file the context menu was opened on

        private void InitGameGridMenu()
        {
            _mnuGameGrid = new ContextMenu();

            mnuGameScan1 = new MenuItem { Header = "Scan Quick (Headers Only)", Tag = EScanLevel.Level1 };
            mnuGameScan2 = new MenuItem { Header = "Scan", Tag = EScanLevel.Level2 };
            mnuGameScan3 = new MenuItem { Header = "Scan Full (Complete Re-Scan)", Tag = EScanLevel.Level3 };

            mnuGameScan1.Click += MnuGameScan;
            mnuGameScan2.Click += MnuGameScan;
            mnuGameScan3.Click += MnuGameScan;

            mnuOpenDir = new MenuItem { Header = "Open Dir" };
            mnuOpenDir.Click += MnuOpenDir;

            mnuOpenParentDir = new MenuItem { Header = "Open Parent" };
            mnuOpenParentDir.Click += MnuOpenParentDir;

            mnuLaunchEmulator = new MenuItem { Header = "Launch emulator" };
            mnuLaunchEmulator.Click += LaunchEmulatorClick;

            mnuOpenPage = new MenuItem { Header = "Open Web Page" };
            mnuOpenPage.Click += OpenWebPage;
        }

        internal void ClearGameGrid()
        {
            _gameGridItems.Clear();
            _romGridItems.Clear();
            gameGrid = null;

            gameSortIndex = -1;
            gameSortAscending = true;
        }

        internal void UpdateGameGrid(RvFile tDir)
        {
            gameGridSource = tDir;
            _updatingGameGrid = true;

            ClearGameGrid();
            UpdateGameGrid();
        }

        internal void UpdateGameGrid(bool onTimer = false)
        {
            if (gameGridSource == null)
                return;

            try
            {
                _updatingGameGrid = true;
                showDescription = false;

                List<RvFile> gameList = new List<RvFile>();
                _gameGridColumnXPositions = new int[(int)RepStatus.EndValue];

                bool wideTypeColumn = false;

                string searchLowerCase = (txtFilter.Text ?? "").ToLower();
                for (int j = 0; j < gameGridSource.ChildCount; j++)
                {
                    RvFile tChildDir = gameGridSource.Child(j);
                    if (!tChildDir.IsDirectory)
                        continue;

                    if (!string.IsNullOrEmpty(txtFilter.Text) && !tChildDir.Name.ToLower().Contains(searchLowerCase))
                        continue;

                    if (!showDescription && tChildDir.Game != null)
                    {
                        string desc = tChildDir.Game.GetData(RvGame.GameData.Description);
                        if (!string.IsNullOrWhiteSpace(desc) && desc != "\u00A4")
                            showDescription = true;
                    }

                    ReportStatus tDirStat = tChildDir.DirStatus;

                    bool gCorrect = tDirStat.HasCorrect();
                    bool gMissing = tDirStat.HasMissing(false);
                    bool gUnknown = tDirStat.HasUnknown();
                    bool gInToSort = tDirStat.HasInToSort();
                    bool gFixes = tDirStat.HasFixesNeeded();
                    bool gMIA = tDirStat.HasMIA();
                    bool gAllMerged = tDirStat.HasAllMerged();

                    bool show = (chkBoxShowComplete.IsChecked == true) && gCorrect && !gMissing && !gFixes;
                    show = show || (chkBoxShowPartial.IsChecked == true) && gMissing && gCorrect;
                    show = show || (chkBoxShowEmpty.IsChecked == true) && gMissing && !gCorrect;
                    show = show || (chkBoxShowFixes.IsChecked == true) && gFixes;
                    show = show || (chkBoxShowMIA.IsChecked == true) && gMIA;
                    show = show || (chkBoxShowMerged.IsChecked == true) && gAllMerged;
                    show = show || gUnknown;
                    show = show || gInToSort;
                    show = show || tChildDir.GotStatus == GotStatus.Corrupt;
                    show = show || !(gCorrect || gMissing || gUnknown || gInToSort || gFixes || gMIA || gAllMerged);

                    if (!show)
                        continue;

                    if (!wideTypeColumn)
                    {
                        string bitmapNameDat = null;
                        if (tChildDir.DatStatus != DatStatus.NotInDat && tChildDir.DatStatus != DatStatus.InToSort)
                            bitmapNameDat = GetBitmapFromType(tChildDir.FileType, tChildDir.ZipDatStruct);

                        string bitmapName = null;
                        if (tChildDir.GotStatus != GotStatus.NotGot)
                            bitmapName = GetBitmapFromType(tChildDir.FileType, tChildDir.ZipStruct);

                        if (bitmapNameDat != null && bitmapName != null && bitmapNameDat != bitmapName)
                            wideTypeColumn = true;
                    }

                    gameList.Add(tChildDir);

                    int columnIndex = 0;
                    for (int l = 0; l < RepairStatus.DisplayOrder.Length; l++)
                    {
                        if (l >= 13) columnIndex = l;
                        if (tDirStat.Get(RepairStatus.DisplayOrder[l]) <= 0) continue;

                        int len = DigitLength(tDirStat.Get(RepairStatus.DisplayOrder[l])) * 7 + 26;
                        if (len > _gameGridColumnXPositions[columnIndex])
                            _gameGridColumnXPositions[columnIndex] = len;

                        columnIndex++;
                    }
                }

                int t = 0;
                for (int l = 0; l < (int)RepStatus.EndValue; l++)
                {
                    int colWidth = _gameGridColumnXPositions[l];
                    _gameGridColumnXPositions[l] = t;
                    t += colWidth;
                }

                gameGrid = gameList.ToArray();

                if (onTimer && gameSortIndex >= 0)
                {
                    IComparer<RvFile> tSort = new GameUiCompare((GameGridColumns)gameSortIndex, gameSortAscending);
                    gameGrid = FastArraySort.SortArray(gameGrid, tSort.Compare);
                }

                // Update the Description column visibility
                if (GameGrid.Columns.Count > (int)GameGridColumns.CDescription)
                    GameGrid.Columns[(int)GameGridColumns.CDescription].IsVisible = showDescription;

                // Set Type column width
                int typeWidth = wideTypeColumn ? 90 : 44;
                if (GameGrid.Columns.Count > (int)GameGridColumns.CType)
                    GameGrid.Columns[(int)GameGridColumns.CType].Width = new DataGridLength(typeWidth);

                // Rebuild observable collection
                RebuildGameGridItems();

                _updatingGameGrid = false;
                UpdateSelectedGame(onTimer);
            }
            catch { }
        }

        private void RebuildGameGridItems()
        {
            _gameGridItems.Clear();
            if (gameGrid == null) return;

            foreach (RvFile tRvDir in gameGrid)
            {
                var item = new GameGridItem
                {
                    RvFile = tRvDir,
                    TypeBitmap = BuildTypeBitmap(tRvDir),
                    GameName = BuildGameName(tRvDir),
                    Description = BuildDescription(tRvDir),
                    DateTime = SetCell(CompressUtils.zipDateTimeToString(tRvDir.FileModTimeStamp), tRvDir, FileStatus.DateFromDAT, 0, 0),
                    StatusBitmap = BuildStatusBitmap(tRvDir),
                };

                // Determine row colors
                ReportStatus tDirStat = tRvDir.DirStatus;
                if (tRvDir.GotStatus == GotStatus.FileLocked)
                {
                    item.BackColor = dark.Down(_displayColor[(int)RepStatus.UnScanned]);
                    item.ForeColor = _fontColor[(int)RepStatus.UnScanned];
                }
                else
                {
                    foreach (RepStatus t1 in RepairStatus.DisplayOrder)
                    {
                        if (tDirStat.Get(t1) <= 0) continue;
                        item.BackColor = dark.Down(_displayColor[(int)t1]);
                        item.ForeColor = _fontColor[(int)t1];
                        break;
                    }
                }

                _gameGridItems.Add(item);
            }
        }

        private static string BuildGameName(RvFile tRvDir)
        {
            if (string.IsNullOrEmpty(tRvDir.FileName))
                return tRvDir.Name;
            return tRvDir.Name + " (Found: " + tRvDir.FileName + ")";
        }

        private static string BuildDescription(RvFile tRvDir)
        {
            if (tRvDir.Game == null) return "";
            string desc = tRvDir.Game.GetData(RvGame.GameData.Description);
            if (desc == "\u00A4") desc = RVIO.Path.GetFileNameWithoutExtension(tRvDir.Name);
            return desc ?? "";
        }

        private Bitmap BuildTypeBitmap(RvFile tRvDir)
        {
            try
            {
                string bitmapNameDat = null;
                if (tRvDir.DatStatus != DatStatus.NotInDat && tRvDir.DatStatus != DatStatus.InToSort)
                    bitmapNameDat = GetBitmapFromType(tRvDir.FileType, tRvDir.ZipDatStruct);

                string bitmapName = null;
                if (tRvDir.GotStatus != GotStatus.NotGot)
                    bitmapName = GetBitmapFromType(tRvDir.FileType, tRvDir.ZipStruct);

                if (tRvDir.GotStatus == GotStatus.Corrupt && bitmapName != null)
                    bitmapName += "Corrupt";

                // Return the primary bitmap
                if (bitmapNameDat != null && bitmapName != null && bitmapNameDat == bitmapName)
                    return rvImages.GetBitmap(bitmapName);
                if (bitmapNameDat != null && bitmapName == null)
                    return rvImages.GetBitmap(bitmapNameDat + "Missing");
                if (bitmapName != null)
                    return rvImages.GetBitmap(bitmapName);
                if (bitmapNameDat != null)
                    return rvImages.GetBitmap(bitmapNameDat);

                return null;
            }
            catch { return null; }
        }

        private Bitmap BuildStatusBitmap(RvFile tRvDir)
        {
            try
            {
                // For status column, just return first relevant status icon
                ReportStatus tDirStat = tRvDir.DirStatus;
                for (int l = 0; l < RepairStatus.DisplayOrder.Length; l++)
                {
                    if (tDirStat.Get(RepairStatus.DisplayOrder[l]) > 0)
                    {
                        return rvImages.GetBitmap("G_" + RepairStatus.DisplayOrder[l]);
                    }
                }
                return null;
            }
            catch { return null; }
        }

        private static int DigitLength(int number)
        {
            int textNumber = number;
            int len = 0;
            while (textNumber > 0)
            {
                textNumber /= 10;
                len++;
            }
            return len;
        }

        private void GameGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectedGame();
        }

        private void UpdateSelectedGame(bool onTimer = false)
        {
            if (_updatingGameGrid)
                return;

            if (GameGrid.SelectedIndex < 0 || GameGrid.SelectedIndex >= _gameGridItems.Count)
            {
                UpdateGameMetaData(new RvFile(FileType.Dir));
                UpdateRomGrid(gameGridSource);
                return;
            }

            RvFile tGame = _gameGridItems[GameGrid.SelectedIndex].RvFile;
            UpdateGameMetaData(tGame);
            UpdateRomGrid(tGame, onTimer);
        }

        private static string GetBitmapFromType(FileType ft, ZipStructure zs)
        {
            switch (ft)
            {
                case FileType.Zip:
                    if (zs == ZipStructure.None) return "Zip";
                    if (zs == ZipStructure.ZipTrrnt) return "ZipTrrnt";
                    if (zs == ZipStructure.ZipTDC) return "ZipTDC";
                    if (zs == ZipStructure.ZipZSTD) return "ZipZSTD";
                    return null;
                case FileType.SevenZip:
                    if (zs == ZipStructure.None) return "SevenZip";
                    if (zs == ZipStructure.SevenZipTrrnt) return "SevenZipTrrnt";
                    if (zs == ZipStructure.SevenZipSLZMA) return "SevenZipSLZMA";
                    if (zs == ZipStructure.SevenZipNLZMA) return "SevenZipNLZMA";
                    if (zs == ZipStructure.SevenZipSZSTD) return "SevenZipSZSTD";
                    if (zs == ZipStructure.SevenZipNZSTD) return "SevenZipNZSTD";
                    return null;
                case FileType.Dir:
                    return "Dir";
            }
            return null;
        }

        internal static string SetCell(string txt, RvFile tRomTable, FileStatus dat, FileStatus file, FileStatus verified)
        {
            string flags = "";
            if (dat != 0 && tRomTable.FileStatusIs(dat))
                flags += "D";
            if (file != 0 && tRomTable.FileStatusIs(file))
                flags += "F";
            if (verified != 0 && tRomTable.FileStatusIs(verified))
                flags += "V";

            if (!string.IsNullOrEmpty(flags))
                flags = " (" + flags + ")";

            return txt + flags;
        }

        // LoadingRow event to set row background/foreground colors
        private void GameGridLoadingRow(object sender, DataGridRowEventArgs e)
        {
            try
            {
                if (e.Row.DataContext is GameGridItem item)
                {
                    var bgColor = dark.StatusColor(item.BackColor);
                    var fgColor = Contrasty(bgColor);
                    e.Row.Background = new SolidColorBrush(bgColor);
                    TextElement.SetForeground(e.Row, new SolidColorBrush(fgColor));
                }
            }
            catch { }
        }

        // Sorting handler
        private void GameGridSorting(object sender, DataGridColumnEventArgs e)
        {
            try
            {
                if (gameGrid == null) return;

                int colIndex = GameGrid.Columns.IndexOf(e.Column);
                if (colIndex < 0) return;
                if (colIndex == (int)GameGridColumns.CRomStatus) return; // Not sortable

                if (gameSortIndex != colIndex)
                {
                    gameSortIndex = colIndex;
                    gameSortAscending = true;
                }
                else
                {
                    gameSortAscending = !gameSortAscending;
                }

                IComparer<RvFile> tSort = new GameUiCompare((GameGridColumns)gameSortIndex, gameSortAscending);
                gameGrid = FastArraySort.SortArray(gameGrid, tSort.Compare);

                RebuildGameGridItems();
                UpdateSelectedGame();
            }
            catch { }
        }

        private class GameUiCompare : IComparer<RvFile>
        {
            private readonly GameGridColumns _colIndex;
            private readonly bool _ascending;

            public GameUiCompare(GameGridColumns colIndex, bool ascending)
            {
                _colIndex = colIndex;
                _ascending = ascending;
            }

            public int Compare(RvFile x, RvFile y)
            {
                try
                {
                    int retVal = 0;
                    switch (_colIndex)
                    {
                        case GameGridColumns.CGame:
                            retVal = string.Compare(x.Name ?? "", y.Name ?? "", StringComparison.Ordinal);
                            break;
                        case GameGridColumns.CDescription:
                            string descX = x.Game?.GetData(RvGame.GameData.Description) ?? "";
                            string descY = y.Game?.GetData(RvGame.GameData.Description) ?? "";
                            if (descX == "\u00A4") descX = RVIO.Path.GetFileNameWithoutExtension(x.Name);
                            if (descY == "\u00A4") descY = RVIO.Path.GetFileNameWithoutExtension(y.Name);
                            retVal = string.Compare(descX, descY, StringComparison.Ordinal);
                            if (retVal != 0) break;
                            retVal = string.Compare(x.Name ?? "", y.Name ?? "", StringComparison.Ordinal);
                            break;
                        case GameGridColumns.CType:
                            retVal = x.FileType - y.FileType;
                            if (retVal != 0) break;
                            retVal = y.ZipStruct - x.ZipStruct;
                            if (retVal != 0) break;
                            retVal = x.RepStatus - y.RepStatus;
                            if (retVal != 0) break;
                            retVal = string.Compare(x.Name ?? "", y.Name ?? "", StringComparison.Ordinal);
                            break;
                        case GameGridColumns.CDateTime:
                            string time1 = CompressUtils.zipDateTimeToString(x.FileModTimeStamp);
                            string time2 = CompressUtils.zipDateTimeToString(y.FileModTimeStamp);
                            retVal = string.Compare(time1 ?? "", time2 ?? "", StringComparison.Ordinal);
                            if (retVal != 0) break;
                            retVal = string.Compare(x.Name ?? "", y.Name ?? "", StringComparison.Ordinal);
                            break;
                    }

                    if (!_ascending)
                        retVal = -retVal;

                    return retVal;
                }
                catch { return 0; }
            }
        }

        // Pointer released on GameGrid - handles right-click context menu and clipboard
        private void GameGridPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            try
            {
                if (e.InitialPressMouseButton == MouseButton.Right)
                {
                    int mouseRow = GameGrid.SelectedIndex;
                    if (mouseRow < 0 || mouseRow >= _gameGridItems.Count)
                        return;

                    bool shiftPressed = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
                    if (shiftPressed)
                    {
                        ShowGameGridContextMenu(mouseRow, e);
                        return;
                    }

                    // Right click without shift = copy to clipboard
                    GameGridItem item = _gameGridItems[mouseRow];
                    string clipText = $"Name : {item.GameName}\nDesc : {item.Description}\n";

                    if (!string.IsNullOrEmpty(clipText))
                    {
                        try
                        {
                            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                            clipboard?.SetTextAsync(clipText);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void ShowGameGridContextMenu(int mouseRow, PointerReleasedEventArgs e)
        {
            var items = new List<object>();
            RvFile thisGame = _gameGridItems[mouseRow].RvFile;
            _gameGridContextFile = thisGame;

            if (thisGame.FileType == FileType.Dir && !_working)
            {
                items.Add(mnuGameScan2);
                items.Add(mnuGameScan1);
                items.Add(mnuGameScan3);
                items.Add(new Separator());
            }

            if (thisGame.Game != null && thisGame.Dat?.GetData(RvDat.DatData.HomePage) == "No-Intro")
            {
                string gameId = thisGame.Game.GetData(RvGame.GameData.Id);
                string datId = thisGame.Dat.GetData(RvDat.DatData.Id);
                if (!string.IsNullOrWhiteSpace(gameId) && !string.IsNullOrWhiteSpace(datId))
                    items.Add(mnuOpenPage);
            }
            if (thisGame.Game != null && thisGame.Dat?.GetData(RvDat.DatData.HomePage) == "redump.org")
            {
                string gameId = thisGame.Game.GetData(RvGame.GameData.Id);
                if (!string.IsNullOrWhiteSpace(gameId))
                    items.Add(mnuOpenPage);
            }

            bool found = false;
            if (thisGame.FileType == FileType.Dir)
            {
                string folderPath = thisGame.FullNameCase;
                if (Directory.Exists(folderPath))
                {
                    found = true;
                    mnuOpenDir.Header = "Open Dir";
                    items.Add(mnuOpenDir);
                }
            }

            if (thisGame.FileType == FileType.Zip || thisGame.FileType == FileType.SevenZip)
            {
                string zipPath = thisGame.FullNameCase;
                if (File.Exists(zipPath))
                {
                    found = true;
                    mnuOpenDir.Header = thisGame.FileType == FileType.Zip ? "Open Zip" : "Open 7Zip";
                    items.Add(mnuOpenDir);
                }
            }

            {
                string parentPath = thisGame.Parent.FullName;
                if (Directory.Exists(parentPath))
                {
                    found = true;
                    mnuOpenParentDir.Header = "Open Parent";
                    items.Add(mnuOpenParentDir);
                }
            }

            if (FindEmulatorInfo(thisGame) != null && found)
                items.Add(mnuLaunchEmulator);

            if (items.Count == 0)
                return;

            // Remove trailing separator
            if (items[items.Count - 1] is Separator)
                items.RemoveAt(items.Count - 1);

            _mnuGameGrid.Items.Clear();
            foreach (var item in items)
                _mnuGameGrid.Items.Add(item);

            _mnuGameGrid.Open(GameGrid);
        }

        private void MnuGameScan(object sender, RoutedEventArgs e)
        {
            if (_working) return;
            if (sender is MenuItem mi && mi.Tag is EScanLevel level)
                ScanRoms(level, _gameGridContextFile);
        }

        private void MnuOpenDir(object sender, RoutedEventArgs e)
        {
            RvFile thisFile = _gameGridContextFile;
            if (thisFile == null) return;

            if (thisFile.FileType == FileType.Dir)
            {
                string folderPath = thisFile.FullNameCase;
                if (Directory.Exists(folderPath))
                    OpenFolder(folderPath);
                return;
            }
            if (thisFile.FileType == FileType.Zip || thisFile.FileType == FileType.SevenZip)
            {
                string zipPath = thisFile.FullNameCase;
                if (File.Exists(zipPath))
                    OpenFolder(zipPath);
            }
        }

        private void MnuOpenParentDir(object sender, RoutedEventArgs e)
        {
            RvFile thisFile = _gameGridContextFile?.Parent;
            if (thisFile == null) return;
            if (thisFile.FileType == FileType.Dir)
            {
                string folderPath = thisFile.FullNameCase;
                if (Directory.Exists(folderPath))
                    OpenFolder(folderPath);
            }
        }

        private void LaunchEmulatorClick(object sender, RoutedEventArgs e)
        {
            if (_gameGridContextFile != null)
                LaunchEmulator(_gameGridContextFile);
        }

        private EmulatorInfo FindEmulatorInfo(RvFile tGame)
        {
            string path = tGame.Parent.DatTreeFullName;
            if (Settings.rvSettings?.EInfo == null)
                return null;
            if (path == "Error")
                return null;
            if (path.Length <= 8)
                return null;

            foreach (EmulatorInfo ei in Settings.rvSettings.EInfo)
            {
                if (!string.Equals(path.Substring(8), ei.TreeDir, StringComparison.CurrentCultureIgnoreCase))
                    continue;
                if (string.IsNullOrWhiteSpace(ei.CommandLine))
                    continue;
                if (!File.Exists(ei.ExeName))
                    continue;
                return ei;
            }
            return null;
        }

        private void OpenWebPage(object sender, RoutedEventArgs e)
        {
            RvFile thisGame = _gameGridContextFile;
            if (thisGame == null) return;

            if (thisGame.Game != null && thisGame.Dat?.GetData(RvDat.DatData.HomePage) == "No-Intro")
            {
                string gameId = thisGame.Game.GetData(RvGame.GameData.Id);
                string datId = thisGame.Dat.GetData(RvDat.DatData.Id);
                if (!string.IsNullOrWhiteSpace(gameId) && !string.IsNullOrWhiteSpace(datId))
                    OpenUrl($"https://datomatic.no-intro.org/index.php?page=show_record&s={datId}&n={gameId}");
            }
            if (thisGame.Game != null && thisGame.Dat?.GetData(RvDat.DatData.HomePage) == "redump.org")
            {
                string gameId = thisGame.Game.GetData(RvGame.GameData.Id);
                if (!string.IsNullOrWhiteSpace(gameId))
                    OpenUrl($"http://redump.org/disc/{gameId}/");
            }
        }

        private void LaunchEmulator(RvFile tGame)
        {
            EmulatorInfo ei = FindEmulatorInfo(tGame);
            if (ei == null) return;

            string commandLineOptions = ei.CommandLine;
            string dirname = tGame.Parent.FullName;
            if (dirname.StartsWith("RomRoot" + System.IO.Path.DirectorySeparatorChar))
                dirname = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                    dirname);

            commandLineOptions = commandLineOptions.Replace("{gamename}", RVIO.Path.GetFileNameWithoutExtension(tGame.Name));
            commandLineOptions = commandLineOptions.Replace("{gamefilename}", tGame.Name);
            commandLineOptions = commandLineOptions.Replace("{gamedirectory}", dirname);

            string workingDir = ei.WorkingDirectory;
            if (string.IsNullOrWhiteSpace(workingDir))
                workingDir = System.IO.Path.GetDirectoryName(ei.ExeName);

            try
            {
                using (Process exeProcess = new Process())
                {
                    exeProcess.StartInfo.WorkingDirectory = workingDir;
                    exeProcess.StartInfo.FileName = ei.ExeName;
                    exeProcess.StartInfo.Arguments = commandLineOptions;
                    exeProcess.StartInfo.UseShellExecute = false;
                    exeProcess.StartInfo.CreateNoWindow = true;
                    exeProcess.Start();
                }
            }
            catch { }
        }

        private void GameGridMouseDoubleClick(object sender, TappedEventArgs e)
        {
            if (_updatingGameGrid) return;

            if (GameGrid.SelectedIndex < 0 || GameGrid.SelectedIndex >= _gameGridItems.Count)
                return;

            RvFile tGame = _gameGridItems[GameGrid.SelectedIndex].RvFile;
            if (tGame.Game == null && tGame.FileType == FileType.Dir)
            {
                UpdateGameGrid(tGame);
                ctrRvTree.SetSelected(tGame);
                UpdateDatMetaData(tGame);
            }
            else
            {
                LaunchEmulator(tGame);
            }
        }
    }
}

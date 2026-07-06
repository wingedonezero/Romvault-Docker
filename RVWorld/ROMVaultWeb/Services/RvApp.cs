using System.Text;
using RomVaultCore;
using RomVaultCore.FindFix;
using RomVaultCore.FixFile;
using RomVaultCore.FixFile.FixAZipCore;
using RomVaultCore.ReadDat;
using RomVaultCore.RvDB;
using RomVaultCore.Scanner;
using RomVaultCore.Utils;
using Compress;
using RVUtils;

namespace ROMVaultWeb.Services;

// ============================================================================
// RvApp: the single shared application state. This is the web equivalent of
// FrmMain - it owns selection, grid models, tree operations and runs the core
// operations on background ThreadWorkers exactly like the desktop UI.
// ============================================================================
public class RvApp
{
    // ------------------------------------------------------------------ state
    public bool Ready;                  // DB loaded, UI usable
    public int InitPercent;
    public int InitMax = 100;
    public string InitText = "Loading Database";
    public string SettingsLoadError;

    public bool Working;                // an operation is running
    public RvFile Selected;             // tree selection
    public RvFile GameGridSource;       // dir whose children fill the game grid
    public RvFile SelectedGame;         // game grid selection (fills rom grid)

    public string FilterText = "";

    public readonly List<GameRow> GameRows = new();
    public readonly List<RomRow> RomRows = new();
    public bool ShowDescriptionCol;
    public bool RomShowMerge, RomShowAlt, RomShowStatus, RomShowDate;

    public int GameSortIndex = -1;
    public bool GameSortAscending = true;

    // progress modal state
    public ProgressModel Progress { get; } = new();
    public FixProgressModel FixProgress { get; } = new();

    // simple message dialog queue (ReportError.Dialog / .ErrorForm)
    public readonly List<(string Text, string Caption, bool IsError)> Messages = new();

    public event Action OnChange;
    public void Notify() => OnChange?.Invoke();

    // ---------------------------------------------------------------- startup
    public void BeginStartup()
    {
        ReportError.ErrorForm += msg => { lock (Messages) Messages.Add((msg, "RomVault Error report", true)); Notify(); };
        ReportError.Dialog += (text, caption) => { lock (Messages) Messages.Add((text, caption, false)); Notify(); };

        ThreadWorker tw = new(StartUpCode)
        {
            wReport = obj =>
            {
                switch (obj)
                {
                    case int p: InitPercent = p; break;
                    case bgwSetRange r: InitMax = r.MaxVal > 0 ? r.MaxVal : 100; break;
                    case bgwText t: InitText = t.Text; break;
                }
                Notify();
            },
            wFinal = () =>
            {
                FindSourceFile.SetFixOrderSettings();
                RootDirsCreate.CheckDatRoot();
                RootDirsCreate.CheckRomRoot();
                RootDirsCreate.CheckToSort();
                Ready = true;
                if (!string.IsNullOrWhiteSpace(SettingsLoadError))
                    lock (Messages) Messages.Add((SettingsLoadError, "Error Reading Settings", false));
                Notify();
            }
        };
        tw.StartAsync();
    }

    private static void StartUpCode(ThreadWorker thWrk)
    {
        RepairStatus.InitStatusCheck();
        DB.Read(thWrk);
    }

    // ============================================================== tree model
    // Children shown in the tree: directories that carry a Tree row.
    public static IEnumerable<RvFile> TreeChildren(RvFile node)
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            RvFile c = node.Child(i);
            if (c.IsDirectory && c.Tree != null)
                yield return c;
        }
    }

    public static bool TreeHasChildren(RvFile node) => TreeChildren(node).Any();

    public static string TreeIcon(RvFile pTree)
    {
        int icon = 2;
        if (pTree.DirStatus.HasInToSort()) icon = 4;
        else if (!pTree.DirStatus.HasCorrect() && pTree.DirStatus.HasMissing()) icon = 1;
        else if (!pTree.DirStatus.HasMissing() && pTree.DirStatus.HasMIA()) icon = 5;
        else if (!pTree.DirStatus.HasMissing()) icon = 3;

        bool dirAboveDats = pTree.Dat == null && pTree.DirDatCount == 0;
        return (dirAboveDats ? "DirectoryTree" : "Tree") + icon;
    }

    public static string TreeCheckIcon(RvFile pTree) => pTree.Tree.Checked switch
    {
        RvTreeRow.TreeSelect.Locked => "TickBoxLocked",
        RvTreeRow.TreeSelect.Selected => "TickBoxTicked",
        _ => "TickBoxUnTicked",
    };

    public static string TreeLabel(RvFile pTree)
    {
        string thistxt;
        if (pTree.Dat == null && pTree.DirDatCount == 1)
            thistxt = pTree.Name + ": " + pTree.DirDat(0).GetData(RvDat.DatData.Description);
        else if (pTree.Dat != null && pTree.Dat.Flag(DatFlags.AutoAddedDirectory) && pTree.Parent.DirDatCount > 1)
            thistxt = pTree.Name + ": ";
        else
            thistxt = pTree.Name;

        if (pTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortPrimary))
            thistxt += " (Primary)";
        else if (pTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortCache))
            thistxt += " (Cache)";
        else if (pTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortFileOnly))
            thistxt += " (File Only)";
        return thistxt;
    }

    public static string TreeSubLabel(RvFile pTree)
    {
        if (pTree.IsInToSort)
            return $"(Files: {pTree.DirStatus.CountInToSort().ToRvString()})";

        int intMIA = pTree.DirStatus.CountMIA();
        int intFoundMIA = pTree.DirStatus.CountFoundMIA();
        string strMIA = intMIA > 0 ? $" \\ MIA: {intMIA.ToRvString()}" : "";
        string strFoundMIA = intFoundMIA > 0 ? $" \\ Found MIA: {intFoundMIA.ToRvString()}" : "";
        return $"( Have: {pTree.DirStatus.CountCorrect().ToRvString()}{strFoundMIA} \\ Missing: {pTree.DirStatus.CountMissing().ToRvString()}{strMIA} )";
    }

    // 3.7.5 tree logic (mirrors RvTreeControl / rvTree)
    public void TreeToggleChecked(RvFile pTree, bool shiftPressed)
    {
        RvTreeRow.TreeSelect ns = pTree.Tree.Checked == RvTreeRow.TreeSelect.Selected
            ? RvTreeRow.TreeSelect.UnSelected
            : RvTreeRow.TreeSelect.Selected;
        SetChecked(pTree, ns, shiftPressed);
        Notify();
    }

    public void SetChecked(RvFile pTree, RvTreeRow.TreeSelect nSelection, bool shiftPressed)
    {
        if (!Working) RvTreeRow.OpenStream();
        SetCheckedRecurse(pTree, nSelection, shiftPressed);
        if (!Working) RvTreeRow.CloseStream();
    }

    private void SetCheckedRecurse(RvFile pTree, RvTreeRow.TreeSelect nSelection, bool shiftPressed)
    {
        pTree.Tree.SetChecked(nSelection, Working);
        if (shiftPressed)
            return;
        for (int i = 0; i < pTree.ChildCount; i++)
        {
            RvFile d = pTree.Child(i);
            if (d.IsDirectory && d.Tree != null)
                SetCheckedRecurse(d, nSelection, false);
        }
    }

    public void SetExpanded(RvFile pTree, bool rightClick)
    {
        if (!rightClick)
        {
            pTree.Tree.SetTreeExpanded(!pTree.Tree.TreeExpanded, Working);
            Notify();
            return;
        }
        if (!Working) RvTreeRow.OpenStream();
        RvTreeRow valueToUse = null;
        for (int i = 0; i < pTree.ChildCount; i++)
        {
            RvFile d = pTree.Child(i);
            if (d.IsDirectory && d.Tree != null) { valueToUse = d.Tree; break; }
        }
        if (valueToUse != null)
        {
            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvFile d = pTree.Child(i);
                if (d.IsDirectory && d.Tree != null)
                    d.Tree.SetTreeExpanded(!valueToUse.TreeExpanded, Working);
            }
        }
        if (!Working) RvTreeRow.CloseStream();
        Notify();
    }

    // ======================================================== selection chain
    public void TreeSelect(RvFile node)
    {
        Selected = node;
        DatSetSelected(node);
        Notify();
    }

    public void DatSetSelected(RvFile cf)
    {
        GameRows.Clear();
        RomRows.Clear();
        SelectedGame = null;
        GameGridSource = null;
        if (cf == null)
            return;
        GameGridSource = cf;
        UpdateGameGrid();
    }

    public void SelectGame(RvFile game)
    {
        SelectedGame = game;
        UpdateRomGrid(game);
        Notify();
    }

    public void GameDoubleClick(GameRow row)
    {
        RvFile tGame = row.File;
        if (tGame.Game == null && tGame.FileType == FileType.Dir)
        {
            Selected = tGame;
            DatSetSelected(tGame);
            Notify();
        }
    }

    public void GameNavigateUp()
    {
        RvFile tParent = GameGridSource?.Parent;
        if (tParent == null) return;
        Selected = tParent;
        DatSetSelected(tParent);
        Notify();
    }

    // ============================================================== game grid
    public void UpdateGameGrid()
    {
        GameRows.Clear();
        RomRows.Clear();
        SelectedGame = null;
        ShowDescriptionCol = false;
        if (GameGridSource == null)
            return;

        string searchLowerCase = (FilterText ?? "").ToLower();
        Settings s = Settings.rvSettings;

        List<RvFile> gameList = new();
        for (int j = 0; j < GameGridSource.ChildCount; j++)
        {
            RvFile tChildDir = GameGridSource.Child(j);
            if (!tChildDir.IsDirectory)
                continue;
            if (!string.IsNullOrEmpty(FilterText) && !tChildDir.Name.ToLower().Contains(searchLowerCase))
                continue;

            if (!ShowDescriptionCol && tChildDir.Game != null)
            {
                string desc = tChildDir.Game.GetData(RvGame.GameData.Description);
                if (!string.IsNullOrWhiteSpace(desc) && desc != "¤")
                    ShowDescriptionCol = true;
            }

            ReportStatus tDirStat = tChildDir.DirStatus;
            bool gCorrect = tDirStat.HasCorrect();
            bool gMissing = tDirStat.HasMissing(false);
            bool gUnknown = tDirStat.HasUnknown();
            bool gInToSort = tDirStat.HasInToSort();
            bool gFixes = tDirStat.HasFixesNeeded();
            bool gMIA = tDirStat.HasMIA();
            bool gAllMerged = tDirStat.HasAllMerged();

            bool show = s.chkBoxShowComplete && gCorrect && !gMissing && !gFixes;
            show = show || s.chkBoxShowPartial && gMissing && gCorrect;
            show = show || s.chkBoxShowEmpty && gMissing && !gCorrect;
            show = show || s.chkBoxShowFixes && gFixes;
            show = show || s.chkBoxShowMIA && gMIA;
            show = show || s.chkBoxShowMerged && gAllMerged;
            show = show || gUnknown || gInToSort || tChildDir.GotStatus == GotStatus.Corrupt;
            show = show || !(gCorrect || gMissing || gUnknown || gInToSort || gFixes || gMIA || gAllMerged);
            if (!show)
                continue;

            gameList.Add(tChildDir);
        }

        RvFile[] arr = gameList.ToArray();
        if (GameSortIndex >= 0)
            Array.Sort(arr, new GameUiCompare(GameSortIndex, GameSortAscending));

        foreach (RvFile f in arr)
            GameRows.Add(BuildGameRow(f));
    }

    public void SortGameGrid(int colIndex)
    {
        if (GameSortIndex != colIndex) { GameSortIndex = colIndex; GameSortAscending = true; }
        else GameSortAscending = !GameSortAscending;
        UpdateGameGrid();
        Notify();
    }

    private static string GetTypeIconFromType(FileType ft, ZipStructure zs)
    {
        switch (ft)
        {
            case FileType.Zip:
                return zs switch
                {
                    ZipStructure.None => "Zip",
                    ZipStructure.ZipTrrnt => "ZipTrrnt",
                    ZipStructure.ZipTDC => "ZipTDC",
                    ZipStructure.ZipZSTD => "ZipZSTD",
                    ZipStructure.ZipDTD => "ZipDTD",
                    ZipStructure.ZipDTZ => "ZipDTZ",
                    _ => null,
                };
            case FileType.SevenZip:
                return zs switch
                {
                    ZipStructure.None => "SevenZip",
                    ZipStructure.SevenZipTrrnt => "SevenZipTrrnt",
                    ZipStructure.SevenZipSLZMA => "SevenZipSLZMA",
                    ZipStructure.SevenZipNLZMA => "SevenZipNLZMA",
                    ZipStructure.SevenZipSZSTD => "SevenZipSZSTD",
                    ZipStructure.SevenZipNZSTD => "SevenZipNZSTD",
                    _ => null,
                };
            case FileType.Dir:
                return "Dir";
        }
        return null;
    }

    private GameRow BuildGameRow(RvFile tRvDir)
    {
        GameRow row = new() { File = tRvDir };

        // type icons: dat-format icon + actual-format icon (+ convert arrow)
        string bitmapNameDat = null;
        if (tRvDir.DatStatus != DatStatus.NotInDat && tRvDir.DatStatus != DatStatus.InToSort)
            bitmapNameDat = GetTypeIconFromType(tRvDir.FileType, tRvDir.ZipDatStruct);
        string bitmapName = null;
        if (tRvDir.GotStatus != GotStatus.NotGot)
            bitmapName = GetTypeIconFromType(tRvDir.FileType, tRvDir.ZipStruct);

        if (bitmapNameDat != null && bitmapName != null)
        {
            if (bitmapNameDat == bitmapName)
                row.TypeIcons.Add(bitmapName);
            else
            {
                row.TypeIcons.Add(bitmapName);
                row.TypeIcons.Add(tRvDir.ZipDatStructFix ? "ZipConvert" : "ZipConvert1");
                row.TypeIcons.Add(bitmapNameDat + "Missing");
            }
        }
        else if (bitmapNameDat != null) row.TypeIcons.Add(bitmapNameDat + "Missing");
        else if (bitmapName != null) row.TypeIcons.Add(bitmapName);
        if (tRvDir.GotStatus == GotStatus.Corrupt && row.TypeIcons.Count > 0 && bitmapName != null)
            row.TypeIcons[0] = bitmapName + "Corrupt";

        row.Name = string.IsNullOrEmpty(tRvDir.FileName) ? tRvDir.Name : tRvDir.Name + " (Found: " + tRvDir.FileName + ")";
        if (tRvDir.Game != null)
        {
            string desc = tRvDir.Game.GetData(RvGame.GameData.Description);
            if (desc == "¤") desc = RVIO.Path.GetFileNameWithoutExtension(tRvDir.Name);
            row.Description = desc ?? "";
        }
        row.DateTime = SetCell(Compress.CompressUtils.zipDateTimeToString(tRvDir.FileModTimeStamp), tRvDir, FileStatus.DateFromDAT, 0, 0);

        // status chips with counts (the composite ROM Status column)
        ReportStatus tDirStat = tRvDir.DirStatus;
        foreach (RepStatus rs in RepairStatus.DisplayOrder)
        {
            int count = tDirStat.Get(rs);
            if (count > 0)
                row.StatusChips.Add((rs.ToString(), count.ToRvString()));
        }

        // row color
        if (tRvDir.GotStatus == GotStatus.FileLocked)
            row.StatusKey = RepStatus.UnScanned.ToString();
        else
            foreach (RepStatus t1 in RepairStatus.DisplayOrder)
            {
                if (tDirStat.Get(t1) <= 0) continue;
                row.StatusKey = t1.ToString();
                break;
            }
        return row;
    }

    // =============================================================== rom grid
    public void UpdateRomGrid(RvFile tGame)
    {
        RomRows.Clear();
        RomShowMerge = RomShowAlt = RomShowStatus = RomShowDate = false;
        if (tGame == null)
            return;

        List<RvFile> fileList = new();
        AddDir(tGame, "", fileList);
        foreach (RvFile f in fileList)
            RomRows.Add(BuildRomRow(f));
    }

    private void AddDir(RvFile tGame, string pathAdd, List<RvFile> fileList)
    {
        if (tGame == null) return;
        for (int l = 0; l < tGame.ChildCount; l++)
        {
            RvFile tFile = tGame.Child(l);
            if (tFile.IsFile)
                AddRom(tFile, pathAdd, fileList);
            if (tGame.Dat == null)
                continue;
            if (!tFile.IsDirectory)
                continue;
            if (tFile.Game == null)
                AddDir(tFile, pathAdd + tFile.Name + "/", fileList);
        }
    }

    private void AddRom(RvFile tFile, string pathAdd, List<RvFile> fileList)
    {
        if (tFile.DatStatus != DatStatus.InDatMerged || tFile.RepStatus != RepStatus.NotCollected ||
            Settings.rvSettings.chkBoxShowMerged)
        {
            tFile.UiDisplayName = pathAdd + tFile.Name;
            fileList.Add(tFile);

            if (!RomShowMerge) RomShowMerge = !string.IsNullOrWhiteSpace(tFile.Merge);
            if (!RomShowAlt) RomShowAlt = tFile.AltSize != null || tFile.AltCRC != null || tFile.AltSHA1 != null || tFile.AltMD5 != null;
            RomShowStatus |= !string.IsNullOrWhiteSpace(tFile.Status);
            RomShowDate |=
                tFile.FileModTimeStamp != 0 &&
                tFile.FileModTimeStamp != long.MinValue &&
                tFile.FileModTimeStamp != Compress.StructuredZip.StructuredZip.TrrntzipDateTime &&
                tFile.FileModTimeStamp != Compress.StructuredZip.StructuredZip.TrrntzipDosDateTime;
        }
    }

    private static RomRow BuildRomRow(RvFile tFile)
    {
        string bitPlusMIA = "";
        if (tFile.MIAStatusIs(MIAStatus.MIA)) bitPlusMIA = "_MIA";
        else if (tFile.MIAStatusIs(MIAStatus.MIAFromDat)) bitPlusMIA = "_MIA";
        else if (tFile.MIAStatusIs(MIAStatus.New)) bitPlusMIA = "_NEW";

        string fname = tFile.UiDisplayName;
        if (!string.IsNullOrEmpty(tFile.FileName)) fname += " (Found: " + tFile.FileName + ")";
        if (tFile.CHDVersion != null) fname += " (V" + tFile.CHDVersion + ")";
        string D = tFile.FileStatusIs(FileStatus.HeaderFileTypeFromDAT) ? "D" : "";
        string F = tFile.FileStatusIs(FileStatus.HeaderFileTypeFromHeader) ? "F" : "";
        if (tFile.HeaderFileType != HeaderFileType.Nothing || !string.IsNullOrWhiteSpace(D) || !string.IsNullOrWhiteSpace(F))
        {
            string req = tFile.HeaderFileTypeRequired ? ",Required" : "";
            fname += $" ({tFile.HeaderFileType}{req} {D}{F})";
        }

        return new RomRow
        {
            File = tFile,
            GotIcon = "R_" + tFile.DatStatus + "_" + tFile.RepStatus + bitPlusMIA,
            Name = fname,
            Merge = tFile.Merge ?? "",
            Size = SetCell(tFile.Size == null ? "" : ((ulong)tFile.Size).ToRvString(), tFile, FileStatus.SizeFromDAT, FileStatus.SizeFromHeader, FileStatus.SizeVerified),
            CRC32 = SetCell(tFile.CRC.ToHexString(), tFile, FileStatus.CRCFromDAT, FileStatus.CRCFromHeader, FileStatus.CRCVerified),
            SHA1 = SetCell(tFile.SHA1.ToHexString(), tFile, FileStatus.SHA1FromDAT, FileStatus.SHA1FromHeader, FileStatus.SHA1Verified),
            MD5 = SetCell(tFile.MD5.ToHexString(), tFile, FileStatus.MD5FromDAT, FileStatus.MD5FromHeader, FileStatus.MD5Verified),
            AltSize = SetCell(tFile.AltSize == null ? "" : ((ulong)tFile.AltSize).ToRvString(), tFile, FileStatus.AltSizeFromDAT, FileStatus.AltSizeFromHeader, FileStatus.AltSizeVerified),
            AltCRC32 = SetCell(tFile.AltCRC.ToHexString(), tFile, FileStatus.AltCRCFromDAT, FileStatus.AltCRCFromHeader, FileStatus.AltCRCVerified),
            AltSHA1 = SetCell(tFile.AltSHA1.ToHexString(), tFile, FileStatus.AltSHA1FromDAT, FileStatus.AltSHA1FromHeader, FileStatus.AltSHA1Verified),
            AltMD5 = SetCell(tFile.AltMD5.ToHexString(), tFile, FileStatus.AltMD5FromDAT, FileStatus.AltMD5FromHeader, FileStatus.AltMD5Verified),
            Status = tFile.Status ?? "",
            DateModFile = BuildDateModFile(tFile),
            ZipIndex = tFile.FileType == FileType.FileZip ? (tFile.ZipFileIndex == -1 ? "" : tFile.ZipFileIndex.ToString()) : "",
            DupeCount = tFile.FileGroup != null ? tFile.FileGroup.Files.Count.ToString() : "",
            StatusKey = ReportStatus.UIStatus(tFile.MIAStatus, tFile.RepStatus).ToString(),
        };
    }

    private static string BuildDateModFile(RvFile tFile)
    {
        if (tFile.FileModTimeStamp == 0 || tFile.FileModTimeStamp == long.MinValue)
            return "";
        return Compress.CompressUtils.zipDateTimeToString(tFile.FileModTimeStamp);
    }

    internal static string SetCell(string txt, RvFile tRomTable, FileStatus dat, FileStatus file, FileStatus verified)
    {
        string flags = "";
        if (dat != 0 && tRomTable.FileStatusIs(dat)) flags += "D";
        if (file != 0 && tRomTable.FileStatusIs(file)) flags += "F";
        if (verified != 0 && tRomTable.FileStatusIs(verified)) flags += "V";
        if (!string.IsNullOrEmpty(flags)) flags = " (" + flags + ")";
        return txt + flags;
    }

    private class GameUiCompare : IComparer<RvFile>
    {
        private readonly int _col;
        private readonly bool _asc;
        public GameUiCompare(int col, bool asc) { _col = col; _asc = asc; }
        public int Compare(RvFile x, RvFile y)
        {
            int retVal = _col switch
            {
                1 => string.Compare(x.Name ?? "", y.Name ?? "", StringComparison.OrdinalIgnoreCase),
                2 => string.Compare(x.Game?.GetData(RvGame.GameData.Description) ?? "", y.Game?.GetData(RvGame.GameData.Description) ?? "", StringComparison.OrdinalIgnoreCase),
                3 => x.FileModTimeStamp.CompareTo(y.FileModTimeStamp),
                _ => string.Compare(x.Name ?? "", y.Name ?? "", StringComparison.OrdinalIgnoreCase),
            };
            if (retVal == 0)
                retVal = string.Compare(x.Name ?? "", y.Name ?? "", StringComparison.OrdinalIgnoreCase);
            return _asc ? retVal : -retVal;
        }
    }

    // ============================================================= operations
    public async Task RunUpdateDats(bool fullRefresh)
    {
        if (Working || !Ready) return;
        if (fullRefresh)
            DatUpdate.InvalidateAllDATs(DB.DirRoot.Child(0), @"DatRoot" + Path.DirectorySeparatorChar);

        RvFile selected = Selected;
        List<RvFile> parents = new();
        while (selected != null) { parents.Add(selected); selected = selected.Parent; }

        await RunOp("Scanning Dats", DatUpdate.UpdateDat, hideCancel: true);

        while (parents.Count > 1 && parents[0].Parent == null)
            parents.RemoveAt(0);
        Selected = parents.Count > 0 ? parents[0] : null;
        DatSetSelected(Selected);
        Notify();
    }

    public async Task RunScan(EScanLevel level, RvFile startAt = null)
    {
        if (Working || !Ready) return;
        FileScanning.StartAt = startAt;
        FileScanning.EScanLevel = level;
        await RunOp("Scanning Dirs", FileScanning.ScanFiles);
        RefreshAfterOp();
    }

    public async Task RunFindFixes(bool showLog = false)
    {
        if (Working || !Ready) return;
        await RunOp("Finding Fixes", FindFixes.ScanFiles, showTimeLog: showLog);
        RefreshAfterOp();
    }

    public async Task RunFix()
    {
        if (Working || !Ready) return;
        Working = true;
        Notify();
        FixProgress.Reset();
        FixProgress.Visible = true;
        Notify();

        TaskCompletionSource done = new();
        ThreadWorker tw = new(Fix.PerformFixes)
        {
            wReport = FixProgress.Handle,
            wFinal = () => { FixProgress.Done(); done.TrySetResult(); }
        };
        FixProgress.Worker = tw;
        FixProgress.Changed = Notify;
        tw.StartAsync();
        await done.Task;

        Working = false;
        RefreshAfterOp();
        Notify();
    }

    public async Task RunAutoScanFix()
    {
        // toolbar right-click: scan -> find fixes -> fix
        if (Working || !Ready) return;
        await RunScan(EScanLevel.Level2);
        if (Progress.Cancelled) return;
        await RunFindFixes();
        if (Progress.Cancelled) return;
        await RunFix();
    }

    public void MakeFixDat(RvFile targetDir, bool redOnly)
    {
        if (Working || !Ready) return;
        targetDir ??= DB.DirRoot.Child(0);

        string outPath = Settings.rvSettings.FixDatOutPath;
        if (string.IsNullOrWhiteSpace(outPath))
        {
            outPath = Path.Combine(Environment.CurrentDirectory, "FixDats");
            if (!Directory.Exists(outPath))
                Directory.CreateDirectory(outPath);
            Settings.rvSettings.FixDatOutPath = outPath;
            Settings.WriteConfig();
        }
        FixDatReport.RecursiveDatTree(outPath, targetDir, redOnly);
        lock (Messages) Messages.Add(($"Fix DAT written to {outPath}", "RomVault", false));
        Notify();
    }

    private async Task RunOp(string title, WorkerStart func, bool hideCancel = false, bool showTimeLog = false)
    {
        Working = true;
        Progress.Reset(title, hideCancel, showTimeLog);
        Progress.Visible = true;
        Notify();

        TaskCompletionSource done = new();
        ThreadWorker tw = new(func)
        {
            wReport = Progress.Handle,
            wFinal = () => { Progress.Done(); done.TrySetResult(); }
        };
        Progress.Worker = tw;
        Progress.Changed = Notify;
        tw.StartAsync();
        await done.Task;

        Working = false;
        Notify();
    }

    private void RefreshAfterOp()
    {
        if (Selected == null || Selected.Parent == null)
        {
            // node may have been replaced during the op; reselect root child 0
            if (Selected != null && DB.DirRoot.ChildCount > 0 && Selected != DB.DirRoot.Child(0))
            {
                bool found = false;
                for (int i = 0; i < DB.DirRoot.ChildCount; i++)
                    if (DB.DirRoot.Child(i) == Selected) { found = true; break; }
                if (!found) Selected = DB.DirRoot.Child(0);
            }
        }
        DatSetSelected(Selected);
    }

    // ================================================================ helpers
    public void SaveShowCheckbox(string key, bool val)
    {
        Settings s = Settings.rvSettings;
        switch (key)
        {
            case "complete": s.chkBoxShowComplete = val; break;
            case "partial": s.chkBoxShowPartial = val; break;
            case "empty": s.chkBoxShowEmpty = val; break;
            case "fixes": s.chkBoxShowFixes = val; break;
            case "mia": s.chkBoxShowMIA = val; break;
            case "merged": s.chkBoxShowMerged = val; break;
        }
        Settings.WriteConfig();
        UpdateGameGrid();
        Notify();
    }

    public string GameInfoText(RvFile g)
    {
        if (g?.Game == null) return "";
        StringBuilder sb = new();
        foreach (RvGame.GameData gd in Enum.GetValues<RvGame.GameData>())
        {
            string v = g.Game.GetData(gd);
            if (!string.IsNullOrWhiteSpace(v) && v != "¤")
                sb.AppendLine($"{gd}: {v}");
        }
        return sb.ToString();
    }
}

// ============================================================== row models
public class GameRow
{
    public RvFile File;
    public readonly List<string> TypeIcons = new();
    public string Name = "";
    public string Description = "";
    public string DateTime = "";
    public readonly List<(string Status, string Count)> StatusChips = new();
    public string StatusKey = "";
}

public class RomRow
{
    public RvFile File;
    public string GotIcon = "";
    public string Name = "";
    public string Merge = "";
    public string Size = "";
    public string CRC32 = "";
    public string SHA1 = "";
    public string MD5 = "";
    public string AltSize = "";
    public string AltCRC32 = "";
    public string AltSHA1 = "";
    public string AltMD5 = "";
    public string Status = "";
    public string DateModFile = "";
    public string ZipIndex = "";
    public string DupeCount = "";
    public string StatusKey = "";
}

// ====================================================== progress modal model
public class ProgressModel
{
    public bool Visible;
    public string Title = "";
    public string Label1 = "", Label2 = "", Label3 = "";
    public double Bar1, Bar1Max = 100, Bar2, Bar2Max = 100;
    public bool Bar2Visible;
    public bool HideCancel;
    public bool ShowTimeLog;
    public bool Finished;
    public bool Cancelled;
    public bool ErrorOpen;
    public readonly List<(string A, string B, bool IsError)> ErrorRows = new();
    public ThreadWorker Worker;
    public Action Changed;

    private DateTime _start, _last;
    private string _lastMessage;

    public void Reset(string title, bool hideCancel, bool showTimeLog)
    {
        Visible = false; Title = title; Label1 = "Initializing"; Label2 = ""; Label3 = "";
        Bar1 = 0; Bar1Max = 100; Bar2 = 0; Bar2Max = 100; Bar2Visible = false;
        HideCancel = hideCancel; ShowTimeLog = showTimeLog;
        Finished = false; Cancelled = false; ErrorOpen = false;
        ErrorRows.Clear();
        _start = DateTime.Now; _last = _start; _lastMessage = "Initializing";
    }

    public void Handle(object obj)
    {
        switch (obj)
        {
            case int v: Bar1 = v; break;
            case bgwText t:
                Label1 = t.Text;
                if (ShowTimeLog) TimeLog(t.Text);
                break;
            case bgwTextError te:
                ErrorOpen = true;
                Label1 = te.Text;
                if (ShowTimeLog) TimeLog(te.Text);
                break;
            case bgwSetRange r: Bar1 = 0; Bar1Max = r.MaxVal >= 0 ? r.MaxVal : 0; break;
            case bgwText2 t2: Label2 = t2.Text; break;
            case bgwValue2 v2: Bar2 = v2.Value; break;
            case bgwSetRange2 r2: Bar2 = 0; Bar2Max = r2.MaxVal >= 0 ? r2.MaxVal : 0; break;
            case bgwRange2Visible rv: Bar2Visible = rv.Visible; break;
            case bgwText3 t3: Label3 = t3.Text; break;
            case bgwShowError se:
                ErrorOpen = true;
                lock (ErrorRows) ErrorRows.Add((se.error, se.filename, true));
                break;
        }
        Changed?.Invoke();
    }

    private void TimeLog(string message)
    {
        ErrorOpen = true;
        DateTime now = DateTime.Now;
        string total = Math.Round((now - _start).TotalSeconds, 3).ToString();
        string part = Math.Round((now - _last).TotalSeconds, 3).ToString();
        _last = now;
        lock (ErrorRows) ErrorRows.Add(($"{total} s  ,  ({part} s)", $"Completed: {_lastMessage}", false));
        _lastMessage = message;
    }

    public (string A, string B, bool IsError)[] ErrorRowsSnapshot()
    {
        lock (ErrorRows) return ErrorRows.ToArray();
    }

    public void Done() { Finished = true; if (!ErrorOpen) Visible = false; Changed?.Invoke(); }
    public void Cancel()
    {
        Cancelled = true;
        Worker?.Cancel();
        Changed?.Invoke();
    }
    public void Close() { Visible = false; Changed?.Invoke(); }
}

public class FixProgressModel
{
    public bool Visible;
    public string Label1 = "";
    public double Bar1, Bar1Max = 100;
    public bool Finished;
    public bool Cancelled;
    public readonly List<bgwShowFix> Rows = new();
    public readonly List<string> Errors = new();
    public ThreadWorker Worker;
    public Action Changed;

    public void Reset()
    {
        Visible = false; Label1 = ""; Bar1 = 0; Bar1Max = 100;
        Finished = false; Cancelled = false;
        Rows.Clear(); Errors.Clear();
    }

    public void Handle(object obj)
    {
        switch (obj)
        {
            case int v: Bar1 = v; break;
            case bgwText t: Label1 = t.Text; break;
            case bgwSetRange r: Bar1 = 0; Bar1Max = r.MaxVal >= 0 ? r.MaxVal : 0; break;
            case bgwShowFix sf: lock (Rows) Rows.Add(sf); break;
            case bgwShowFixError fe: lock (Errors) Errors.Add(fe.FixError); break;
        }
        Changed?.Invoke();
    }

    public bgwShowFix[] RowsSnapshot()
    {
        lock (Rows) return Rows.ToArray();
    }

    public string[] ErrorsSnapshot()
    {
        lock (Errors) return Errors.ToArray();
    }

    public void Done() { Finished = true; Changed?.Invoke(); }
    public void Cancel() { Cancelled = true; Worker?.Cancel(); Changed?.Invoke(); }
    public void Close() { Visible = false; Changed?.Invoke(); }
}

using System.Globalization;
using DATReader.DatStore;
using DATReader.DatWriter;
using RomVaultCore;
using RomVaultCore.ReadDat;
using RomVaultCore.RvDB;

namespace ROMVaultWeb.Services;

// Side panels, ToSort tree actions, presets, reports - the finish-out pass.
public partial class RvApp
{
    // ============================================================ dat info panel
    public class DatPanelModel
    {
        public string Name = "", Description = "", Category = "", Version = "", Author = "", Date = "";
        public string Path = "", RomsGot = "", RomsMissing = "", RomsFixable = "", RomsUnknown = "";
    }

    public DatPanelModel DatPanel { get; } = new();

    // exact port of UpdateDatMetaData
    private void UpdateDatPanel(RvFile tDir)
    {
        DatPanelModel d = DatPanel;
        d.Name = tDir.Name;

        RvDat tDat = null;
        if (tDir.Dat != null)
            tDat = tDir.Dat;
        else if (tDir.DirDatCount == 1)
            tDat = tDir.DirDat(0);

        if (tDat != null)
        {
            if (d.Name != tDat.GetData(RvDat.DatData.DatName))
                d.Name += $":  {tDat.GetData(RvDat.DatData.DatName)}";

            string datId = tDat.GetData(RvDat.DatData.Id);
            if (!string.IsNullOrWhiteSpace(datId))
                d.Name += $" (ID:{datId})";

            d.Description = tDat.GetData(RvDat.DatData.Description);
            d.Category = tDat.GetData(RvDat.DatData.Category);
            d.Version = tDat.GetData(RvDat.DatData.Version);
            d.Author = tDat.GetData(RvDat.DatData.Author);
            d.Date = tDat.GetData(RvDat.DatData.Date);
            string header = tDat.GetData(RvDat.DatData.Header);
            if (!string.IsNullOrWhiteSpace(header))
                d.Name += " (" + header + ")";
        }
        else
        {
            d.Description = d.Category = d.Version = d.Author = d.Date = "";
        }

        d.Path = tDir.FullName;

        d.RomsGot = tDir.DirStatus.CountCorrect().ToString("N0", CultureInfo.InvariantCulture);
        if (tDir.DirStatus.CountFoundMIA() > 0) d.RomsGot += $"  -  {tDir.DirStatus.CountFoundMIA()} Found MIA";

        d.RomsMissing = tDir.DirStatus.CountMissing().ToString("N0", CultureInfo.InvariantCulture);
        if (tDir.DirStatus.CountMIA() > 0) d.RomsMissing += $"  -  {tDir.DirStatus.CountMIA()} MIA";

        d.RomsFixable = tDir.DirStatus.CountFixesNeeded().ToString("N0", CultureInfo.InvariantCulture);
        d.RomsUnknown = (tDir.DirStatus.CountUnknown() + tDir.DirStatus.CountInToSort()).ToString("N0", CultureInfo.InvariantCulture);
    }

    // =========================================================== game info panel
    public class GamePanelModel
    {
        public bool Visible;
        public string Name = "";
        public readonly List<(string Label, string Value)> Fields = new();
    }

    public GamePanelModel GamePanel { get; } = new();
    public ArtPanel Art { get; } = new();

    // exact port of UpdateGameMetaData incl. the artwork dispatch
    public void UpdateGamePanel(RvFile tGame)
    {
        GamePanelModel g = GamePanel;
        g.Fields.Clear();
        g.Visible = tGame != null;
        if (tGame == null)
        {
            Art.Clear();
            return;
        }

        g.Name = tGame.Name;
        string gameId = tGame.Game?.GetData(RvGame.GameData.Id);
        if (!string.IsNullOrWhiteSpace(gameId))
            g.Name += $" (ID:{gameId})";

        if (tGame.Game == null)
        {
            Art.Clear();
            return;
        }

        string desc = tGame.Game.GetData(RvGame.GameData.Description);
        if (desc == "¤") desc = RVIO.Path.GetFileNameWithoutExtension(tGame.Name);

        if (tGame.Game.GetData(RvGame.GameData.EmuArc) == "yes")
        {
            g.Fields.Add(("Description", desc));
            g.Fields.Add(("Publisher", tGame.Game.GetData(RvGame.GameData.Publisher)));
            g.Fields.Add(("Developer", tGame.Game.GetData(RvGame.GameData.Developer)));
            g.Fields.Add(("Title Id", tGame.Game.GetData(RvGame.GameData.Id)));
            g.Fields.Add(("Source", tGame.Game.GetData(RvGame.GameData.Source)));
            g.Fields.Add(("Clone Of", tGame.Game.GetData(RvGame.GameData.CloneOf)));
            g.Fields.Add(("Related To", tGame.Game.GetData(RvGame.GameData.RelatedTo)));
            g.Fields.Add(("Year", tGame.Game.GetData(RvGame.GameData.Year)));
            g.Fields.Add(("Players", tGame.Game.GetData(RvGame.GameData.Players)));
            g.Fields.Add(("Genre", tGame.Game.GetData(RvGame.GameData.Genre)));
            g.Fields.Add(("Sub Genre", tGame.Game.GetData(RvGame.GameData.SubGenre)));
            g.Fields.Add(("Ratings", tGame.Game.GetData(RvGame.GameData.Ratings)));
            g.Fields.Add(("Score", tGame.Game.GetData(RvGame.GameData.Score)));

            SidePanel.LoadTruRip(Art, tGame);
        }
        else
        {
            bool found = false;
            string path = tGame.Parent.DatTreeFullName;
            foreach (EmulatorInfo ei in Settings.rvSettings.EInfo ?? new List<EmulatorInfo>())
            {
                if (path.Length <= 8)
                    continue;
                if (!string.Equals(path.Substring(8), ei.TreeDir, StringComparison.CurrentCultureIgnoreCase))
                    continue;
                if (string.IsNullOrWhiteSpace(ei.ExtraPath))
                    continue;

                found = true;
                if (ei.ExtraPath.Substring(0, 1) == "%")
                    SidePanel.LoadMameSL(Art, tGame, ei.ExtraPath.Substring(1));
                else
                    SidePanel.LoadMame(Art, tGame, ei.ExtraPath);
                break;
            }

            if (!found)
                found = SidePanel.LoadNfoPanel(Art, tGame);
            if (!found)
                found = SidePanel.LoadC64(Art, tGame);
            if (!found)
                Art.Clear();

            g.Fields.Add(("Description", desc));
            g.Fields.Add(("Manufacturer", tGame.Game.GetData(RvGame.GameData.Manufacturer)));
            g.Fields.Add(("Clone Of", tGame.Game.GetData(RvGame.GameData.CloneOf)));
            g.Fields.Add(("Rom Of", tGame.Game.GetData(RvGame.GameData.RomOf)));
            g.Fields.Add(("Year", tGame.Game.GetData(RvGame.GameData.Year)));
            g.Fields.Add(("Category", tGame.Game.GetData(RvGame.GameData.Category)));
        }
    }

    // rom grid selection: image files preview in the side panel (desktop parity)
    public void SelectRom(RomRow row)
    {
        if (row?.File != null)
            SidePanel.LoadFromRom(Art, row.File);
        Notify();
    }

    // ============================================================= tosort actions
    public void ToSortSetPrimary(RvFile clicked)
    {
        if (clicked.Tree.Checked == RvTreeRow.TreeSelect.Locked)
            clicked.Tree.SetChecked(RvTreeRow.TreeSelect.Selected, true);

        RvFile t = DB.GetToSortPrimary();
        bool wasCache = t.ToSortStatusIs(RvFile.ToSortDirType.ToSortCache);
        t.ToSortStatusClear(RvFile.ToSortDirType.ToSortPrimary | RvFile.ToSortDirType.ToSortCache);

        clicked.ToSortStatusSet(RvFile.ToSortDirType.ToSortPrimary);
        if (wasCache)
            clicked.ToSortStatusSet(RvFile.ToSortDirType.ToSortCache);

        DB.Write();
        Notify();
    }

    public void ToSortSetCache(RvFile clicked)
    {
        if (clicked.Tree.Checked == RvTreeRow.TreeSelect.Locked)
            clicked.Tree.SetChecked(RvTreeRow.TreeSelect.Selected, true);

        RvFile t = DB.GetToSortCache();
        t.ToSortStatusClear(RvFile.ToSortDirType.ToSortCache);
        clicked.ToSortStatusSet(RvFile.ToSortDirType.ToSortCache);

        DB.Write();
        Notify();
    }

    public void ToSortSetFileOnly(RvFile clicked)
    {
        if (clicked.Tree.Checked == RvTreeRow.TreeSelect.Locked)
            clicked.Tree.SetChecked(RvTreeRow.TreeSelect.Selected, true);

        if (clicked.ToSortStatusIs(RvFile.ToSortDirType.ToSortPrimary))
        {
            lock (Messages) Messages.Add(("Primary Directory Cannot be File Only.", "RomVault", false));
            Notify();
            return;
        }
        if (clicked.ToSortStatusIs(RvFile.ToSortDirType.ToSortCache))
        {
            lock (Messages) Messages.Add(("Cache Directory Cannot be File Only.", "RomVault", false));
            Notify();
            return;
        }

        clicked.ToSortStatusSet(RvFile.ToSortDirType.ToSortFileOnly);
        DB.Write();
        Notify();
    }

    public void ToSortClearFileOnly(RvFile clicked)
    {
        clicked.ToSortStatusClear(RvFile.ToSortDirType.ToSortFileOnly);
        DB.Write();
        Notify();
    }

    public void ToSortRemove(RvFile clicked)
    {
        for (int i = 0; i < DB.DirRoot.ChildCount; i++)
        {
            if (DB.DirRoot.Child(i) != clicked)
                continue;
            DB.DirRoot.ChildRemove(i);
            RepairStatus.ReportStatusReset(DB.DirRoot);
            TreeSelect(DB.DirRoot.Child(i - 1));
            DB.Write();
            return;
        }
    }

    public void ToSortMove(RvFile clicked, bool up)
    {
        if (up) DB.MoveToSortUp(clicked);
        else DB.MoveToSortDown(clicked);
        DB.Write();
        Notify();
    }

    public static int ToSortIndex(RvFile clicked)
    {
        for (int i = 0; i < DB.DirRoot.ChildCount; i++)
            if (DB.DirRoot.Child(i) == clicked)
                return i;
        return -1;
    }

    // ============================================================== tree presets
    public void TreePreset(bool save, int index)
    {
        DatTreeStatusStore dtss = new();
        if (save)
        {
            dtss.write(index);
            lock (Messages) Messages.Add(($"Tree state saved to preset {index}.", "RomVault", false));
            Notify();
            return;
        }
        dtss.read(index);
        Notify();
    }

    // ============================================================ reports / dats
    public string ReportsDir
    {
        get
        {
            string dir = Path.Combine(Environment.CurrentDirectory, "Reports");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }
    }

    private static string CleanTime() => " (" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ")";

    public void RunFullReport()
    {
        if (Working || !Ready) return;
        string file = Path.Combine(ReportsDir, "RVFullReport" + CleanTime() + ".txt");
        WebReport.GenerateReport(file);
        ReportDone(file);
    }

    public void RunFixReport()
    {
        if (Working || !Ready) return;
        string file = Path.Combine(ReportsDir, "RVFixReport" + CleanTime() + ".txt");
        WebReport.GenerateFixReport(file);
        ReportDone(file);
    }

    public void SaveFullDat(RvFile targetDir)
    {
        if (Working || !Ready || targetDir == null) return;
        string file = Path.Combine(ReportsDir, targetDir.Name + CleanTime() + ".dat");
        DatHeader dh = new ExternalDatConverterTo().ConvertToExternalDat(targetDir);
        DatXMLWriter.WriteDat(file, dh);
        ReportDone(file);
    }

    private void ReportDone(string file)
    {
        string name = Path.GetFileName(file);
        lock (Messages) Messages.Add(($"Saved to {file}\n\nDownload: /dl/{Uri.EscapeDataString(name)}", "RomVault", false));
        Notify();
    }

    // =============================================================== game extras
    public static string GameWebUrl(RvFile thisGame)
    {
        if (thisGame?.Game == null)
            return null;
        if (thisGame.Dat?.GetData(RvDat.DatData.HomePage) == "No-Intro")
        {
            string gameId = thisGame.Game.GetData(RvGame.GameData.Id);
            string datId = thisGame.Dat.GetData(RvDat.DatData.Id);
            if (!string.IsNullOrWhiteSpace(gameId) && !string.IsNullOrWhiteSpace(datId))
                return $"https://datomatic.no-intro.org/index.php?page=show_record&s={datId}&n={gameId}";
        }
        if (thisGame.Dat?.GetData(RvDat.DatData.HomePage) == "redump.org")
        {
            string gameId = thisGame.Game.GetData(RvGame.GameData.Id);
            if (!string.IsNullOrWhiteSpace(gameId))
                return $"http://redump.org/disc/{gameId}/";
        }
        return null;
    }

    // FrmRomInfo: the rom occurrence list for a FileGroup
    public static List<(string Status, string Path)> RomOccurrences(RvFile tFile)
    {
        List<(string, string)> list = new();
        if (tFile?.FileGroup?.Files == null)
            return list;
        foreach (RvFile f in tFile.FileGroup.Files)
            list.Add((f.GotStatus.ToString(), f.FullName));
        return list;
    }
}

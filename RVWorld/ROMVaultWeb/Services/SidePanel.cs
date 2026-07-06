using System.Text.RegularExpressions;
using CodePage;
using Compress;
using Compress.ZipFile;
using RomVaultCore;
using RomVaultCore.RvDB;

namespace ROMVaultWeb.Services;

// The artwork/info side panel (desktop tabSideArtwork). Ported from
// HelperSideInfo + MainWindow_Artwork: loads images/text straight out of the
// game's zip (or dir) and exposes them for the /art/{slot} endpoint.
public class ArtPanel
{
    public bool Visible;
    public long Version;                       // cache buster for <img> urls
    public readonly Dictionary<string, byte[]> Images = new(); // Artwork, Logo, Medium1, Medium2, ScreenTitle, ScreenShot
    public string InfoTab, InfoText;           // "NFO" / "Story.txt"
    public string Info2Tab, Info2Text;         // "DIZ"

    public bool HasArt => Images.ContainsKey("Artwork") || Images.ContainsKey("Logo");
    public bool HasMedium => Images.ContainsKey("Medium1") || Images.ContainsKey("Medium2");
    public bool HasScreens => Images.ContainsKey("ScreenTitle") || Images.ContainsKey("ScreenShot");

    public void Clear()
    {
        Images.Clear();
        InfoTab = InfoText = Info2Tab = Info2Text = null;
        Visible = false;
        Version++;
    }
}

public static class SidePanel
{
    // ------------------------------------------------------ byte-level loader
    private static Regex WildcardToRegex(string pattern)
    {
        if (pattern.ToLower().StartsWith("regex:"))
            return new Regex(pattern.Substring(6), RegexOptions.IgnoreCase);
        return new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$",
            RegexOptions.IgnoreCase);
    }

    private static bool LoadBytes(RvFile tGame, string filename, out byte[] memBuffer)
    {
        memBuffer = null;
        Regex rSearch = WildcardToRegex(filename);

        int cCount = tGame.ChildCount;
        if (cCount == 0)
            return false;

        int found = -1;
        for (int i = 0; i < cCount; i++)
        {
            RvFile rvf = tGame.Child(i);
            if (rvf.GotStatus != GotStatus.Got)
                continue;
            if (!rSearch.IsMatch(rvf.Name))
                continue;
            found = i;
            break;
        }
        if (found == -1)
            return false;

        try
        {
            switch (tGame.FileType)
            {
                case FileType.Zip:
                    {
                        RvFile imagefile = tGame.Child(found);
                        if (imagefile.ZipFileHeaderPosition == null)
                            return false;

                        Zip zf = new();
                        if (zf.ZipFileOpen(tGame.FullNameCase, tGame.FileModTimeStamp, false) != ZipReturn.ZipGood)
                            return false;

                        if (zf.ZipFileOpenReadStreamFromLocalHeaderPointer((ulong)imagefile.ZipFileHeaderPosition, false,
                                out Stream stream, out ulong streamSize, out ushort _) != ZipReturn.ZipGood)
                        {
                            zf.ZipFileClose();
                            return false;
                        }

                        memBuffer = new byte[streamSize];
                        stream.Read(memBuffer, 0, (int)streamSize);
                        zf.ZipFileClose();
                        return true;
                    }
                case FileType.Dir:
                    {
                        RvFile imagefile = tGame.Child(found);
                        string artwork = imagefile.FullNameCase;
                        if (!RVIO.File.Exists(artwork))
                            return false;

                        RVIO.FileStream.OpenFileRead(artwork, out Stream stream);
                        memBuffer = new byte[stream.Length];
                        stream.Read(memBuffer, 0, memBuffer.Length);
                        stream.Close();
                        stream.Dispose();
                        return true;
                    }
                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private static bool TryImage(ArtPanel p, string slot, RvFile tGame, string pattern)
    {
        // desktop TryLoadImage: pattern without extension, tries png then jpg
        foreach (string ext in new[] { ".png", ".jpg" })
        {
            if (LoadBytes(tGame, pattern + ext, out byte[] buf))
            {
                p.Images[slot] = buf;
                return true;
            }
        }
        return false;
    }

    private static bool TryImageExact(ArtPanel p, string slot, RvFile tGame, string pattern)
    {
        if (!LoadBytes(tGame, pattern, out byte[] buf))
            return false;
        p.Images[slot] = buf;
        return true;
    }

    private static string NfoText(byte[] memBuffer)
    {
        string txt = CodePage437.GetStringLF(memBuffer);
        txt = txt.Replace("\r\n", "\n").Replace("\r", "\n");
        return txt;
    }

    private static bool TryNfo(ArtPanel p, bool second, RvFile tGame, string pattern, string tabName)
    {
        if (!LoadBytes(tGame, pattern, out byte[] buf))
            return false;
        if (second) { p.Info2Tab = tabName; p.Info2Text = NfoText(buf); }
        else { p.InfoTab = tabName; p.InfoText = NfoText(buf); }
        return true;
    }

    // ------------------------------------------------------------ the loaders
    public static void LoadTruRip(ArtPanel p, RvFile tGame)
    {
        p.Clear();
        bool art = TryImage(p, "Artwork", tGame, "Artwork/artwork_front");
        bool logo = TryImage(p, "Logo", tGame, "Artwork/logo");
        if (!logo) logo = TryImage(p, "Logo", tGame, "Artwork/artwork_back");
        bool m1 = TryImage(p, "Medium1", tGame, "Artwork/medium_front*");
        bool m2 = TryImage(p, "Medium2", tGame, "Artwork/medium_back*");
        bool title = TryImage(p, "ScreenTitle", tGame, "Artwork/screentitle");
        bool screen = TryImage(p, "ScreenShot", tGame, "Artwork/screenshot");
        bool story = TryNfo(p, false, tGame, "Artwork/story.txt", "Story.txt");
        if (!story) story = TryNfo(p, false, tGame, "*.nfo", "NFO");

        p.Visible = art || logo || m1 || m2 || title || screen || story;
        p.Version++;
    }

    public static bool LoadNfoPanel(ArtPanel p, RvFile tGame)
    {
        p.Clear();
        bool a = TryNfo(p, false, tGame, "*.nfo", "NFO");
        bool b = TryNfo(p, true, tGame, "*.diz", "DIZ");
        p.Visible = a || b;
        p.Version++;
        return p.Visible;
    }

    public static bool LoadC64(ArtPanel p, RvFile tGame)
    {
        p.Clear();
        bool art = TryImage(p, "Artwork", tGame, "Front");
        bool logo = TryImage(p, "Logo", tGame, "Extras/Cassette");
        bool title = TryImage(p, "ScreenTitle", tGame, "Extras/Inlay");
        bool screen = TryImage(p, "ScreenShot", tGame, "Extras/Inlay_back");
        p.Visible = art || logo || title || screen;
        p.Version++;
        return p.Visible;
    }

    public static void LoadMame(ArtPanel p, RvFile tGame, string extraPath)
    {
        p.Clear();
        RvFile fExtra = FindExtraDir(extraPath);
        if (fExtra == null) { p.Version++; return; }

        string gname = RVIO.Path.GetFileNameWithoutExtension(tGame.Name);

        LoadMameSlot(p, "Artwork", fExtra, "artpreview.zip", "artpreviewsnap", gname);
        LoadMameSlot(p, "Logo", fExtra, "marquees.zip", "marquees", gname);
        LoadMameSlot(p, "ScreenShot", fExtra, "snap.zip", "snap", gname);
        LoadMameSlot(p, "ScreenTitle", fExtra, "cabinets.zip", "cabinets", gname);

        p.Visible = p.Images.Count > 0;
        p.Version++;
    }

    public static void LoadMameSL(ArtPanel p, RvFile tGame, string extraPath)
    {
        p.Clear();
        RvFile fExtra = FindExtraDir(extraPath);
        if (fExtra == null) { p.Version++; return; }

        string fname = tGame.Parent.Name + "/" + RVIO.Path.GetFileNameWithoutExtension(tGame.Name);

        LoadMameSlot(p, "Artwork", fExtra, "covers_SL.zip", null, fname);
        LoadMameSlot(p, "Logo", fExtra, "snap_SL.zip", null, fname);
        LoadMameSlot(p, "ScreenShot", fExtra, "titles_SL.zip", null, fname);

        p.Visible = p.Images.Count > 0;
        p.Version++;
    }

    public static bool LoadFromRom(ArtPanel p, RvFile tRom)
    {
        string ext = RVIO.Path.GetExtension(tRom.Name).ToLower();
        if (ext != ".png" && ext != ".jpg")
            return false;
        p.Clear();
        if (TryImageExact(p, "Artwork", tRom.Parent, tRom.Name))
        {
            p.Visible = true;
            p.Version++;
            return true;
        }
        p.Version++;
        return false;
    }

    private static RvFile FindExtraDir(string extraPath)
    {
        string[] path = extraPath.Split('\\');
        RvFile fExtra = DB.DirRoot.Child(0);
        foreach (string part in path)
        {
            if (fExtra.ChildNameSearch(FileType.Dir, part, out int pIndex) != 0)
                return null;
            fExtra = fExtra.Child(pIndex);
        }
        return fExtra;
    }

    private static void LoadMameSlot(ArtPanel p, string slot, RvFile fExtra, string zipName, string dirName, string gameName)
    {
        if (fExtra.ChildNameSearch(FileType.Zip, zipName, out int index) == 0)
        {
            TryImage(p, slot, fExtra.Child(index), gameName.Replace("\\", "/"));
            return;
        }
        if (dirName != null && fExtra.ChildNameSearch(FileType.Dir, dirName, out index) == 0)
            TryImage(p, slot, fExtra.Child(index), gameName);
    }
}

/******************************************************
 *     ROMVault3 is written by Gordon J.              *
 *     Contact gordon@romvault.com                    *
 *     Copyright 2025                                 *
 ******************************************************/

using System;
using System.IO;
using Avalonia.Controls;
using RomVaultCore.RvDB;

namespace ROMVault
{
    public partial class MainWindow
    {
        private void TabArtworkInitialize()
        {
            // Start with artwork panel hidden
            TabEmuArc.IsVisible = false;
            artworkSplitter.IsVisible = false;
        }

        private void ShowArtworkPanel()
        {
            TabEmuArc.IsVisible = true;
            artworkSplitter.IsVisible = true;
        }

        private void HideArtworkPanel()
        {
            TabEmuArc.IsVisible = false;
            artworkSplitter.IsVisible = false;
        }

        private void RemoveAllArtworkTabs()
        {
            // Remove all artwork tab items from TabControl
            TabEmuArc.Items.Clear();
        }

        private void AddTabItem(TabItem tab)
        {
            if (!TabEmuArc.Items.Contains(tab))
                TabEmuArc.Items.Add(tab);
        }

        private void LoadMamePannels(RvFile tGame, string extraPath)
        {
            RemoveAllArtworkTabs();

            string[] path = extraPath.Split('\\');

            RvFile fExtra = DB.DirRoot.Child(0);

            foreach (string p in path)
            {
                if (fExtra.ChildNameSearch(FileType.Dir, p, out int pIndex) != 0)
                    return;
                fExtra = fExtra.Child(pIndex);
            }

            bool artLoaded = false;
            bool logoLoaded = false;

            bool titleLoaded = false;
            bool screenLoaded = false;

            bool storyLoaded = false;

            int index;

            if (fExtra.ChildNameSearch(FileType.Zip, "artpreview.zip", out index) == 0)
            {
                artLoaded = picArtwork.TryLoadImage(fExtra.Child(index), Path.GetFileNameWithoutExtension(tGame.Name));
            }
            else if (fExtra.ChildNameSearch(FileType.Dir, "artpreviewsnap", out index) == 0)
            {
                artLoaded = picArtwork.TryLoadImage(fExtra.Child(index), Path.GetFileNameWithoutExtension(tGame.Name));
            }

            if (fExtra.ChildNameSearch(FileType.Zip, "marquees.zip", out index) == 0)
            {
                logoLoaded = picLogo.TryLoadImage(fExtra.Child(index), Path.GetFileNameWithoutExtension(tGame.Name));
            }
            else if (fExtra.ChildNameSearch(FileType.Dir, "marquees", out index) == 0)
            {
                logoLoaded = picLogo.TryLoadImage(fExtra.Child(index), Path.GetFileNameWithoutExtension(tGame.Name));
            }

            if (fExtra.ChildNameSearch(FileType.Zip, "snap.zip", out index) == 0)
            {
                screenLoaded = picScreenShot.TryLoadImage(fExtra.Child(index), Path.GetFileNameWithoutExtension(tGame.Name));
            }
            else if (fExtra.ChildNameSearch(FileType.Dir, "snap", out index) == 0)
            {
                screenLoaded = picScreenShot.TryLoadImage(fExtra.Child(index), Path.GetFileNameWithoutExtension(tGame.Name));
            }

            if (fExtra.ChildNameSearch(FileType.Zip, "cabinets.zip", out index) == 0)
            {
                titleLoaded = picScreenTitle.TryLoadImage(fExtra.Child(index), Path.GetFileNameWithoutExtension(tGame.Name));
            }
            else if (fExtra.ChildNameSearch(FileType.Dir, "cabinets", out index) == 0)
            {
                titleLoaded = picScreenTitle.TryLoadImage(fExtra.Child(index), Path.GetFileNameWithoutExtension(tGame.Name));
            }

            if (artLoaded || logoLoaded) AddTabItem(tabArtWork);
            if (titleLoaded || screenLoaded) AddTabItem(tabScreens);
            if (storyLoaded) AddTabItem(tabInfo);

            if (artLoaded || logoLoaded || titleLoaded || screenLoaded || storyLoaded)
            {
                ShowArtworkPanel();
            }
            else
            {
                HideArtworkPanel();
            }
        }

        private void LoadMameSLPannels(RvFile tGame, string extraPath)
        {
            RemoveAllArtworkTabs();

            string[] path = extraPath.Split('\\');

            RvFile fExtra = DB.DirRoot.Child(0);

            foreach (string p in path)
            {
                if (fExtra.ChildNameSearch(FileType.Dir, p, out int pIndex) != 0)
                    return;
                fExtra = fExtra.Child(pIndex);
            }

            bool artLoaded = false;
            bool logoLoaded = false;

            bool titleLoaded = false;
            bool screenLoaded = false;

            bool storyLoaded = false;

            int index;

            string fname = tGame.Parent.Name + "/" + Path.GetFileNameWithoutExtension(tGame.Name);

            if (fExtra.ChildNameSearch(FileType.Zip, "covers_SL.zip", out index) == 0)
            {
                artLoaded = picArtwork.TryLoadImage(fExtra.Child(index), fname);
            }

            if (fExtra.ChildNameSearch(FileType.Zip, "snap_SL.zip", out index) == 0)
            {
                logoLoaded = picLogo.TryLoadImage(fExtra.Child(index), fname);
            }

            if (fExtra.ChildNameSearch(FileType.Zip, "titles_SL.zip", out index) == 0)
            {
                screenLoaded = picScreenShot.TryLoadImage(fExtra.Child(index), fname);
            }

            if (artLoaded || logoLoaded) AddTabItem(tabArtWork);
            if (titleLoaded || screenLoaded) AddTabItem(tabScreens);
            if (storyLoaded) AddTabItem(tabInfo);

            if (artLoaded || logoLoaded || titleLoaded || screenLoaded || storyLoaded)
            {
                ShowArtworkPanel();
            }
            else
            {
                HideArtworkPanel();
            }
        }

        // need to only load new image if the RvFile has changed
        // to stop flickering on screen while system is processing
        private void LoadPannelFromRom(RvFile tRom)
        {
            RemoveAllArtworkTabs();

            string ext = Path.GetExtension(tRom.Name).ToLower();
            if (ext != ".png" && ext != ".jpg")
            {
                HideArtworkPanel();
                return;
            }
            bool loaded = picArtwork.LoadImage(tRom.Parent, tRom.Name);
            if (loaded)
            {
                AddTabItem(tabArtWork);
                ShowArtworkPanel();
            }
            else
            {
                HideArtworkPanel();
            }
        }

        private bool LoadC64Pannel(RvFile tGame)
        {
            RemoveAllArtworkTabs();

            bool artLoaded = picArtwork.TryLoadImage(tGame, "Front");
            bool logoLoaded = picLogo.TryLoadImage(tGame, "Extras/Cassette");

            bool titleLoaded = picScreenTitle.TryLoadImage(tGame, "Extras/Inlay");
            bool screenLoaded = picScreenShot.TryLoadImage(tGame, "Extras/Inlay_back");

            if (artLoaded || logoLoaded) AddTabItem(tabArtWork);
            if (titleLoaded || screenLoaded) AddTabItem(tabScreens);

            if (artLoaded || logoLoaded || titleLoaded || screenLoaded)
            {
                ShowArtworkPanel();
                return true;
            }
            else
            {
                HideArtworkPanel();
                return false;
            }
        }

        private bool LoadNFOPannel(RvFile tGame)
        {
            RemoveAllArtworkTabs();

            bool storyLoaded = txtInfo.LoadNFO(tGame, "*.nfo");
            if (storyLoaded)
            {
                tabInfo.Header = "NFO";
                AddTabItem(tabInfo);
            }

            bool storyLoaded2 = txtInfo2.LoadNFO(tGame, "*.diz");
            if (storyLoaded2)
            {
                tabInfo2.Header = "DIZ";
                AddTabItem(tabInfo2);
            }
            if (storyLoaded || storyLoaded2)
            {
                ShowArtworkPanel();
                return true;
            }
            else
            {
                HideArtworkPanel();
                return false;
            }
        }

        private void LoadTruRipPannel(RvFile tGame)
        {
            RemoveAllArtworkTabs();

            /*
             * artwork_front.png
             * artowrk_back.png
             * logo.png
             * medium_front.png
             * screentitle.png
             * screenshot.png
             * story.txt
             */

            bool artLoaded = picArtwork.TryLoadImage(tGame, "Artwork/artwork_front");
            bool logoLoaded = picLogo.TryLoadImage(tGame, "Artwork/logo");
            if (!logoLoaded)
                logoLoaded = picArtwork.TryLoadImage(tGame, "Artwork/artwork_back");

            bool medium1Loaded = picMedium1.TryLoadImage(tGame, "Artwork/medium_front*");
            bool medium2Loaded = picMedium2.TryLoadImage(tGame, "Artwork/medium_back*");
            bool titleLoaded = picScreenTitle.TryLoadImage(tGame, "Artwork/screentitle");
            bool screenLoaded = picScreenShot.TryLoadImage(tGame, "Artwork/screenshot");
            bool storyLoaded = txtInfo.LoadText(tGame, "Artwork/story.txt");
            if (storyLoaded)
                tabInfo.Header = "Story.txt";

            if (!storyLoaded)
            {
                storyLoaded = txtInfo.LoadNFO(tGame, "*.nfo");
                if (storyLoaded)
                    tabInfo.Header = "NFO";
            }

            if (artLoaded || logoLoaded) AddTabItem(tabArtWork);
            if (medium1Loaded || medium2Loaded) AddTabItem(tabMedium);
            if (titleLoaded || screenLoaded) AddTabItem(tabScreens);
            if (storyLoaded) AddTabItem(tabInfo);

            if (artLoaded || logoLoaded || titleLoaded || screenLoaded || storyLoaded || medium1Loaded || medium2Loaded)
            {
                ShowArtworkPanel();
            }
            else
            {
                HideArtworkPanel();
            }
        }

        private void HidePannel()
        {
            HideArtworkPanel();

            picArtwork.ClearImage();
            picLogo.ClearImage();
            picMedium1.ClearImage();
            picMedium2.ClearImage();
            picScreenTitle.ClearImage();
            picScreenShot.ClearImage();
            txtInfo.ClearText();
            txtInfo2.ClearText();
        }
    }
}

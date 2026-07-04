using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CodePage;
using Compress;
using Compress.ZipFile;
using RomVaultCore.RvDB;
using File = RVIO.File;
using Path = RVIO.Path;

namespace ROMVault
{
    public static class HelperSideInfo
    {
        private static Regex WildcardToRegex(string pattern)
        {
            if (pattern.ToLower().StartsWith("regex:"))
                return new Regex(pattern.Substring(6), RegexOptions.IgnoreCase);

            return new Regex("^" + Regex.Escape(pattern).
            Replace("\\*", ".*").
            Replace("\\?", ".") + "$", RegexOptions.IgnoreCase);
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

                            Zip zf = new Zip();
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
                            if (!File.Exists(artwork))
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
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

        }


        /// <summary>
        /// Try loading a PNG or JPG image from a game archive, setting the Image source on the target control.
        /// Returns true if an image was loaded successfully.
        /// </summary>
        public static bool TryLoadImage(this Image imageControl, RvFile tGame, string filename)
        {
            return imageControl.LoadImage(tGame, filename + ".png") || imageControl.LoadImage(tGame, filename + ".jpg");
        }

        /// <summary>
        /// Load an image by exact filename from a game archive and set it on an Avalonia Image control.
        /// Returns true if successful.
        /// </summary>
        public static bool LoadImage(this Image imageControl, RvFile tGame, string filename)
        {
            imageControl.ClearImage();
            if (!LoadBytes(tGame, filename, out byte[] memBuffer))
                return false;
            using (MemoryStream ms = new MemoryStream(memBuffer, false))
            {
                imageControl.Source = new Bitmap(ms);
            }

            return true;
        }

        /// <summary>
        /// Clear the image from an Avalonia Image control, disposing the previous bitmap.
        /// </summary>
        public static void ClearImage(this Image imageControl)
        {
            if (imageControl.Source is Bitmap bmp)
            {
                bmp.Dispose();
            }
            imageControl.Source = null;
        }

        /// <summary>
        /// Load a Bitmap from an archive without assigning it to a control.
        /// Useful when the caller needs the Bitmap directly.
        /// Returns null if loading fails.
        /// </summary>
        public static Bitmap LoadBitmapFromArchive(RvFile tGame, string filename)
        {
            if (!LoadBytes(tGame, filename, out byte[] memBuffer))
                return null;
            using (MemoryStream ms = new MemoryStream(memBuffer, false))
            {
                return new Bitmap(ms);
            }
        }

        /// <summary>
        /// Try loading a PNG or JPG bitmap from an archive.
        /// Returns null if loading fails.
        /// </summary>
        public static Bitmap TryLoadBitmapFromArchive(RvFile tGame, string filename)
        {
            return LoadBitmapFromArchive(tGame, filename + ".png")
                   ?? LoadBitmapFromArchive(tGame, filename + ".jpg");
        }


        /// <summary>
        /// Load text content from a game archive into a TextBox.
        /// Returns true if successful.
        /// </summary>
        public static bool LoadText(this TextBox txtBox, RvFile tGame, string filename)
        {
            txtBox.ClearText();
            if (!LoadBytes(tGame, filename, out byte[] memBuffer))
                return false;

            string txt = Encoding.ASCII.GetString(memBuffer);
            txt = txt.Replace("\r\n", "\r\n\r\n");
            txtBox.Text = txt;

            return true;
        }

        /// <summary>
        /// Load NFO text (CodePage 437) from a game archive into a TextBox.
        /// Returns true if successful.
        /// </summary>
        public static bool LoadNFO(this TextBox txtBox, RvFile tGame, string search)
        {
            if (!LoadBytes(tGame, search, out byte[] memBuffer))
                return false;

            string txt = CodePage437.GetStringLF(memBuffer);
            txt = txt.Replace("\r\n", "\n");
            txt = txt.Replace("\r", "\n");
            txt = txt.Replace("\n", "\r\n");
            txtBox.Text = txt;

            return true;
        }

        /// <summary>
        /// Clear the text content of a TextBox.
        /// </summary>
        public static void ClearText(this TextBox txtBox)
        {
            txtBox.Text = "";
        }
    }
}

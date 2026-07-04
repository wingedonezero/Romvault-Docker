using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Compress.ZipFile;

namespace ROMVault
{
    public static class rvImages
    {
        private static Dictionary<string, Bitmap> bmps = new Dictionary<string, Bitmap>();

        public static Bitmap GetBitmap(string bitmapName, bool duplicate = false)
        {
            if (bmps.TryGetValue(bitmapName, out Bitmap bmp))
            {
                return bmp;
            }

            // Tier 1: graphics.zip
            if (File.Exists("graphics.zip"))
            {
                Zip zf = new Zip();
                zf.ZipFileOpen("graphics.zip", -1, true);
                for (int i = 0; i < zf.LocalFilesCount; i++)
                {
                    if (zf.GetFileHeader(i).Filename == bitmapName + ".png")
                    {
                        zf.ZipFileOpenReadStream(i, out Stream stream, out ulong streamSize);
                        byte[] bBmp = new byte[(int)streamSize];
                        stream.Read(bBmp, 0, (int)streamSize);
                        using MemoryStream ms = new MemoryStream(bBmp);
                        var bmpf = new Bitmap(ms);
                        bmps[bitmapName] = bmpf;
                        zf.ZipFileCloseReadStream();
                        zf.ZipFileClose();
                        return bmpf;
                    }
                }
                zf.ZipFileClose();
            }

            // Tier 2: graphics/ folder
            string sep = Path.DirectorySeparatorChar.ToString();
            string path = $"graphics{sep}{bitmapName}.png";
            if (File.Exists(path))
            {
                var bmpf = new Bitmap(path);
                bmps[bitmapName] = bmpf;
                return bmpf;
            }

            // Tier 3: Embedded Avalonia resources
            try
            {
                var uri = new Uri($"avares://ROMVault37/Assets/{bitmapName}.png");
                using var assetStream = AssetLoader.Open(uri);
                var bm = new Bitmap(assetStream);
                bmps[bitmapName] = bm;
                return bm;
            }
            catch
            {
                return null;
            }
        }
    }
}

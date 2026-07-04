/******************************************************
 *     ROMVault3 is written by Gordon J.              *
 *     Contact gordon@romvault.com                    *
 *     Copyright 2025                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Compress;
using DarkAvalonia;
using RomVaultCore;
using RomVaultCore.RvDB;
using RomVaultCore.Utils;

namespace ROMVault
{
    public enum eRomGrid
    {
        Got = 0,
        Rom = 1,
        Merge = 2,
        Size = 3,
        CRC32 = 4,
        SHA1 = 5,
        MD5 = 6,
        AltSize = 7,
        AltCRC32 = 8,
        AltSHA1 = 9,
        AltMD5 = 10,
        Status = 11,
        DateModFile = 12,
        ZipIndex = 13,
        DupeCount = 14
    }

    /// <summary>
    /// View-model item for each row in the RomGrid DataGrid.
    /// </summary>
    public class RomGridItem
    {
        public RvFile RvFile { get; set; }

        public Bitmap GotBitmap { get; set; }
        public string RomName { get; set; }
        public string Merge { get; set; }
        public string Size { get; set; }
        public string CRC32 { get; set; }
        public string SHA1 { get; set; }
        public string MD5 { get; set; }
        public string AltSize { get; set; }
        public string AltCRC32 { get; set; }
        public string AltSHA1 { get; set; }
        public string AltMD5 { get; set; }
        public string Status { get; set; }
        public string DateModFile { get; set; }
        public string ZipIndex { get; set; }
        public string DupeCount { get; set; }

        // Row color
        public Color BackColor { get; set; }
        public Color ForeColor { get; set; }
    }

    public partial class MainWindow
    {
        private bool altFound = false;
        private RvFile[] romGrid;
        private int romSortIndex = -1;
        private bool romSortAscending = true;

        private bool showStatus;
        private bool showFileModDate;

        internal void UpdateRomGrid(RvFile tGame, bool onTimer = false)
        {
            if (romSortIndex != -1)
            {
                // Reset sort state
            }

            romSortIndex = -1;
            romSortAscending = true;

            altFound = false;
            showStatus = false;
            showFileModDate = false;

            List<RvFile> fileList = new List<RvFile>();
            AddDir(tGame, "", ref fileList);
            romGrid = fileList.ToArray();

            // Set visibility of Alt columns
            if (RomGrid.Columns.Count > (int)eRomGrid.AltSize)
                RomGrid.Columns[(int)eRomGrid.AltSize].IsVisible = altFound;
            if (RomGrid.Columns.Count > (int)eRomGrid.AltCRC32)
                RomGrid.Columns[(int)eRomGrid.AltCRC32].IsVisible = altFound;
            if (RomGrid.Columns.Count > (int)eRomGrid.AltSHA1)
                RomGrid.Columns[(int)eRomGrid.AltSHA1].IsVisible = altFound;
            if (RomGrid.Columns.Count > (int)eRomGrid.AltMD5)
                RomGrid.Columns[(int)eRomGrid.AltMD5].IsVisible = altFound;

            if (RomGrid.Columns.Count > (int)eRomGrid.Status)
                RomGrid.Columns[(int)eRomGrid.Status].IsVisible = showStatus;
            if (RomGrid.Columns.Count > (int)eRomGrid.DateModFile)
                RomGrid.Columns[(int)eRomGrid.DateModFile].IsVisible = showFileModDate;

            RebuildRomGridItems();
        }

        private void AddDir(RvFile tGame, string pathAdd, ref List<RvFile> fileList)
        {
            if (tGame == null)
                return;

            try
            {
                for (int l = 0; l < tGame.ChildCount; l++)
                {
                    RvFile tBase = tGame.Child(l);
                    RvFile tFile = tBase;
                    if (tFile.IsFile)
                    {
                        AddRom(tFile, pathAdd, ref fileList);
                    }

                    if (tGame.Dat == null)
                        continue;

                    RvFile tDir = tBase;
                    if (!tDir.IsDirectory)
                        continue;

                    if (tDir.Game == null)
                    {
                        AddDir(tDir, pathAdd + tDir.Name + "/", ref fileList);
                    }
                }
            }
            catch { }
        }

        private void AddRom(RvFile tFile, string pathAdd, ref List<RvFile> fileList)
        {
            try
            {
                if (tFile.DatStatus != DatStatus.InDatMerged || tFile.RepStatus != RepStatus.NotCollected ||
                    chkBoxShowMerged.IsChecked == true)
                {
                    tFile.UiDisplayName = pathAdd + tFile.Name;
                    fileList.Add(tFile);
                    if (!altFound)
                    {
                        altFound = (tFile.AltSize != null) || (tFile.AltCRC != null) || (tFile.AltSHA1 != null) || (tFile.AltMD5 != null);
                    }
                    showStatus |= !string.IsNullOrWhiteSpace(tFile.Status);

                    showFileModDate |=
                        (tFile.FileModTimeStamp != 0) &&
                        (tFile.FileModTimeStamp != long.MinValue) &&
                        (tFile.FileModTimeStamp != Compress.StructuredZip.StructuredZip.TrrntzipDateTime) &&
                        (tFile.FileModTimeStamp != Compress.StructuredZip.StructuredZip.TrrntzipDosDateTime);
                }
            }
            catch { }
        }

        private void RebuildRomGridItems()
        {
            _romGridItems.Clear();
            if (romGrid == null) return;

            foreach (RvFile tFile in romGrid)
            {
                var item = new RomGridItem
                {
                    RvFile = tFile,
                    GotBitmap = BuildRomGotBitmap(tFile),
                    RomName = BuildRomName(tFile),
                    Merge = tFile.Merge ?? "",
                    Size = SetCell(tFile.Size == null ? "" : ((ulong)tFile.Size).ToString("N0"), tFile, FileStatus.SizeFromDAT, FileStatus.SizeFromHeader, FileStatus.SizeVerified),
                    CRC32 = SetCell(tFile.CRC.ToHexString(), tFile, FileStatus.CRCFromDAT, FileStatus.CRCFromHeader, FileStatus.CRCVerified),
                    SHA1 = SetCell(tFile.SHA1.ToHexString(), tFile, FileStatus.SHA1FromDAT, FileStatus.SHA1FromHeader, FileStatus.SHA1Verified),
                    MD5 = SetCell(tFile.MD5.ToHexString(), tFile, FileStatus.MD5FromDAT, FileStatus.MD5FromHeader, FileStatus.MD5Verified),
                    AltSize = SetCell(tFile.AltSize == null ? "" : ((ulong)tFile.AltSize).ToString("N0"), tFile, FileStatus.AltSizeFromDAT, FileStatus.AltSizeFromHeader, FileStatus.AltSizeVerified),
                    AltCRC32 = SetCell(tFile.AltCRC.ToHexString(), tFile, FileStatus.AltCRCFromDAT, FileStatus.AltCRCFromHeader, FileStatus.AltCRCVerified),
                    AltSHA1 = SetCell(tFile.AltSHA1.ToHexString(), tFile, FileStatus.AltSHA1FromDAT, FileStatus.AltSHA1FromHeader, FileStatus.AltSHA1Verified),
                    AltMD5 = SetCell(tFile.AltMD5.ToHexString(), tFile, FileStatus.AltMD5FromDAT, FileStatus.AltMD5FromHeader, FileStatus.AltMD5Verified),
                    Status = tFile.Status ?? "",
                    DateModFile = BuildDateModFile(tFile),
                    ZipIndex = tFile.FileType == FileType.FileZip ? (tFile.ZipFileIndex == -1 ? "" : tFile.ZipFileIndex.ToString()) : "",
                    DupeCount = tFile.FileGroup != null ? tFile.FileGroup.Files.Count.ToString() : "",
                    BackColor = dark.Down(_displayColor[(int)tFile.RepStatus]),
                    ForeColor = _fontColor[(int)tFile.RepStatus]
                };

                _romGridItems.Add(item);
            }
        }

        private static Bitmap BuildRomGotBitmap(RvFile tFile)
        {
            try
            {
                string bitmapName = "R_" + tFile.DatStatus + "_" + tFile.RepStatus;
                return rvImages.GetBitmap(bitmapName);
            }
            catch { return null; }
        }

        private static string BuildRomName(RvFile tFile)
        {
            string fname = tFile.UiDisplayName;
            if (!string.IsNullOrEmpty(tFile.FileName))
                fname += " (Found: " + tFile.FileName + ")";

            if (tFile.CHDVersion != null)
                fname += " (V" + tFile.CHDVersion + ")";

            string D = tFile.FileStatusIs(FileStatus.HeaderFileTypeFromDAT) ? "D" : "";
            string F = tFile.FileStatusIs(FileStatus.HeaderFileTypeFromHeader) ? "F" : "";
            if (tFile.HeaderFileType != HeaderFileType.Nothing || !string.IsNullOrWhiteSpace(D) || !string.IsNullOrWhiteSpace(F))
            {
                string req = tFile.HeaderFileTypeRequired ? ",Required" : "";
                fname += $" ({tFile.HeaderFileType}{req} {D}{F})";
            }

            return fname;
        }

        private static string BuildDateModFile(RvFile tFile)
        {
            if (tFile.FileModTimeStamp == 0 || tFile.FileModTimeStamp == long.MinValue)
                return "";
            if (tFile.FileModTimeStamp == Compress.StructuredZip.StructuredZip.TrrntzipDateTime ||
                tFile.FileModTimeStamp == Compress.StructuredZip.StructuredZip.TrrntzipDosDateTime)
            {
                return SetCell("Trrntziped", tFile, FileStatus.DateFromDAT, 0, 0);
            }
            return SetCell(CompressUtils.zipDateTimeToString(tFile.FileModTimeStamp), tFile, FileStatus.DateFromDAT, 0, 0);
        }

        private void RomGridLoadingRow(object sender, DataGridRowEventArgs e)
        {
            try
            {
                if (e.Row.DataContext is RomGridItem item)
                {
                    var bgColor = dark.StatusColor(item.BackColor);
                    var fgColor = Contrasty(bgColor);
                    e.Row.Background = new SolidColorBrush(bgColor);
                    TextElement.SetForeground(e.Row, new SolidColorBrush(fgColor));
                }
            }
            catch { }
        }

        private void RomGridSorting(object sender, DataGridColumnEventArgs e)
        {
            try
            {
                if (romGrid == null) return;

                int colIndex = RomGrid.Columns.IndexOf(e.Column);
                if (colIndex < 0) return;

                if (romSortIndex != colIndex)
                {
                        romSortIndex = colIndex;
                    romSortAscending = true;
                }
                else
                {
                    romSortAscending = !romSortAscending;
                }

                IComparer<RvFile> t = new RomUiCompare(colIndex, romSortAscending);
                Array.Sort(romGrid, t);
                RebuildRomGridItems();
            }
            catch { }
        }

        private class RomUiCompare : IComparer<RvFile>
        {
            private readonly int _colIndex;
            private readonly bool _ascending;

            public RomUiCompare(int colIndex, bool ascending)
            {
                _colIndex = colIndex;
                _ascending = ascending;
            }

            public int Compare(RvFile x, RvFile y)
            {
                try
                {
                    int retVal = 0;
                    switch ((eRomGrid)_colIndex)
                    {
                        case eRomGrid.Got:
                            retVal = x.GotStatus - y.GotStatus;
                            if (retVal != 0) break;
                            retVal = x.RepStatus - y.RepStatus;
                            if (retVal != 0) break;
                            retVal = string.Compare(x.UiDisplayName ?? "", y.UiDisplayName ?? "", StringComparison.Ordinal);
                            break;
                        case eRomGrid.Rom:
                            retVal = string.Compare(x.UiDisplayName ?? "", y.UiDisplayName ?? "", StringComparison.Ordinal);
                            break;
                        case eRomGrid.Merge:
                            retVal = string.Compare(x.Merge ?? "", y.Merge ?? "", StringComparison.Ordinal);
                            break;
                        case eRomGrid.Size:
                            retVal = ULong.iCompareNull(x.Size, y.Size);
                            break;
                        case eRomGrid.CRC32:
                            retVal = ArrByte.ICompare(x.CRC, y.CRC);
                            break;
                        case eRomGrid.SHA1:
                            retVal = ArrByte.ICompare(x.SHA1, y.SHA1);
                            break;
                        case eRomGrid.MD5:
                            retVal = ArrByte.ICompare(x.MD5, y.MD5);
                            break;
                        case eRomGrid.AltSize:
                            retVal = ULong.iCompareNull(x.AltSize, y.AltSize);
                            break;
                        case eRomGrid.AltCRC32:
                            retVal = ArrByte.ICompare(x.AltCRC, y.AltCRC);
                            break;
                        case eRomGrid.AltSHA1:
                            retVal = ArrByte.ICompare(x.AltSHA1, y.AltSHA1);
                            break;
                        case eRomGrid.AltMD5:
                            retVal = ArrByte.ICompare(x.AltMD5, y.AltMD5);
                            break;
                        case eRomGrid.Status:
                            retVal = string.Compare(x.Status ?? "", y.Status ?? "", StringComparison.Ordinal);
                            break;
                        case eRomGrid.DateModFile:
                            string time1 = CompressUtils.zipDateTimeToString(x.FileModTimeStamp);
                            string time2 = CompressUtils.zipDateTimeToString(y.FileModTimeStamp);
                            retVal = string.Compare(time1 ?? "", time2 ?? "", StringComparison.Ordinal);
                            break;
                        case eRomGrid.ZipIndex:
                            retVal = x.ZipFileIndex - y.ZipFileIndex;
                            break;
                        case eRomGrid.DupeCount:
                            if (x.FileGroup != null && y.FileGroup != null)
                                retVal = x.FileGroup.Files.Count - y.FileGroup.Files.Count;
                            else
                                retVal = 0;
                            break;
                    }

                    if (!_ascending)
                        retVal = -retVal;

                    if (retVal == 0 && _colIndex != 1)
                        retVal = string.Compare(x.UiDisplayName ?? "", y.UiDisplayName ?? "", StringComparison.Ordinal);

                    return retVal;
                }
                catch { return 0; }
            }
        }

        private void RomGridPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            try
            {
                if (e.InitialPressMouseButton == MouseButton.Left)
                {
                    // Check if clicking on DupeCount column
                    int mouseRow = RomGrid.SelectedIndex;
                    if (mouseRow < 0 || mouseRow >= _romGridItems.Count)
                        return;

                    // DupeCount click opens RomInfo dialog
                    RvFile tFile = _romGridItems[mouseRow].RvFile;
                    if (tFile.FileGroup != null)
                    {
                        // For now, show rom info dialog
                        var fri = new FrmRomInfo();
                        fri.SetRom(tFile);
                        fri.ShowDialog(this);
                    }
                    return;
                }

                if (e.InitialPressMouseButton != MouseButton.Right)
                    return;

                int row = RomGrid.SelectedIndex;
                if (row < 0 || row >= _romGridItems.Count)
                    return;

                RomGridItem item = _romGridItems[row];
                RvFile romFile = item.RvFile;

                string name = item.RomName ?? "";
                string size = item.Size ?? "";
                if (size.Contains(" "))
                    size = size.Substring(0, size.IndexOf(" "));

                string crc = item.CRC32 ?? "";
                if (crc.Length > 8)
                    crc = crc.Substring(0, 8);

                string sha1 = item.SHA1 ?? "";
                if (sha1.Length > 40)
                    sha1 = sha1.Substring(0, 40);

                string md5 = item.MD5 ?? "";
                if (md5.Length > 32)
                    md5 = md5.Substring(0, 32);

                string clipText = $"Name : {name}\nSize : {size}\nCRC32: {crc}\n";
                if (!string.IsNullOrWhiteSpace(sha1))
                    clipText += $"SHA1 : {sha1}\n";
                if (!string.IsNullOrWhiteSpace(md5))
                    clipText += $"MD5  : {md5}\n";

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
            catch { }
        }

        private void RomGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Original WinForms cleared selection; in Avalonia we just allow single selection
            // Optionally could load a panel from the selected rom
        }
    }
}

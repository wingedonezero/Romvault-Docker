/******************************************************
 *     ROMVault3 is written by Gordon J.              *
 *     Contact gordon@romvault.com                    *
 *     Copyright 2025                                 *
 ******************************************************/

using System;
using RomVaultCore;
using RomVaultCore.RvDB;

namespace ROMVault
{
    internal static class Automate
    {
        enum AutoStat
        {
            Start_Scanning,
            Scanning,
            FindFix,
            Fixing,
            Done
        }

        private static AutoStat fixStat;

        public static void AutoScanFix()
        {
            fixStat = AutoStat.Start_Scanning;
            AutoNext();
        }

        private static void FinishedFC()
        {
            AutoNext();
        }

        private static void fceh(object sender, EventArgs e)
        {
            AutoNext();
        }

        // 2: Find Fix / Fix
        // 3: PreScan
        // 4: FC
        // Post FC is just first ToSort

        public static void AutoNext()
        {
            switch (fixStat)
            {
                case AutoStat.Start_Scanning:
                    fixStat = AutoStat.Scanning;
                    Program.frmMain.ScanRoms(EScanLevel.Level2, null, fceh);
                    return;

                case AutoStat.Scanning:
                    if (Program.frmMain.frmScanRoms.Cancelled)
                    {
                        fixStat = AutoStat.Done;
                        return;
                    }
                    fixStat = AutoStat.FindFix;
                    Program.frmMain.FindFixes(false, fceh);
                    return;

                case AutoStat.FindFix:
                    if (Program.frmMain.frmFindFixes.Cancelled)
                    {
                        fixStat = AutoStat.Done;
                        return;
                    }
                    fixStat = AutoStat.Fixing;
                    Program.frmMain.FixFiles(false, fceh);
                    return;

                case AutoStat.Fixing:
                    fixStat = AutoStat.Done;
                    return;
            }
        }
    }
}

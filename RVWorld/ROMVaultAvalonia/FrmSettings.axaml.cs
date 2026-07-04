/******************************************************
 *     ROMVault3 is written by Gordon J.              *
 *     Contact gordon@romvault.com                    *
 *     Copyright 2025                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using RomVaultCore;
using RomVaultCore.Utils;

namespace ROMVault
{
    public partial class FrmSettings : Window
    {
        public FrmSettings()
        {
            InitializeComponent();

            cboFixLevel.ItemsSource = new List<string>
            {
                "Level 1 - Fast copy Match on CRC",
                "Level 2 - Fast copy if SHA1 scanned",
                "Level 3 - Uncompress/Hash/Compress"
            };

            var coreItems = new List<string> { "Auto" };
            for (int i = 1; i <= 64; i++)
                coreItems.Add(i.ToString());
            cboCores.ItemsSource = coreItems;

            cbo7zStruct.ItemsSource = new List<string>
            {
                "LZMA Solid - rv7z",
                "LZMA Non-Solid",
                "ZSTD Solid",
                "ZSTD Non-Solid"
            };

            Opened += FrmConfigLoad;

            chkSendFoundMIA.IsCheckedChanged += ChkSendFoundMIA_CheckedChanged;
        }

        private void FrmConfigLoad(object sender, EventArgs e)
        {
            lblDATRoot.Text = Settings.rvSettings.DatRoot;
            cboFixLevel.SelectedIndex = (int)Settings.rvSettings.FixLevel;

            textBox1.Text = "";
            foreach (string file in Settings.rvSettings.IgnoreFiles)
            {
                textBox1.Text += file + Environment.NewLine;
            }

            chkSendFoundMIA.IsChecked = Settings.rvSettings.MIACallback;
            chkSendFoundMIAAnon.IsChecked = Settings.rvSettings.MIAAnon;

            chkDetailedReporting.IsChecked = Settings.rvSettings.DetailedFixReporting;
            chkDoubleCheckDelete.IsChecked = Settings.rvSettings.DoubleCheckDelete;
            chkCacheSaveTimer.IsChecked = Settings.rvSettings.CacheSaveTimerEnabled;
            upTime.Value = Settings.rvSettings.CacheSaveTimePeriod;
            chkDebugLogs.IsChecked = Settings.rvSettings.DebugLogsEnabled;
            chkDeleteOldCueFiles.IsChecked = Settings.rvSettings.DeleteOldCueFiles;

            var coreItems = (List<string>)cboCores.ItemsSource;
            cboCores.SelectedIndex = Settings.rvSettings.zstdCompCount >= coreItems.Count ? 0 : Settings.rvSettings.zstdCompCount;

            cbo7zStruct.SelectedIndex = Settings.rvSettings.sevenZDefaultStruct;
            chkDarkMode.IsChecked = Settings.rvSettings.Darkness;
            chkDoNotReportFeedback.IsChecked = Settings.rvSettings.DoNotReportFeedback;

            // Set initial enabled state for anonymous checkbox
            chkSendFoundMIAAnon.IsEnabled = chkSendFoundMIA.IsChecked == true;
        }

        private void BtnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnOkClick(object sender, RoutedEventArgs e)
        {
            Settings.rvSettings.DatRoot = lblDATRoot.Text;
            Settings.rvSettings.FixLevel = (EFixLevel)cboFixLevel.SelectedIndex;

            string strtxt = textBox1.Text ?? "";
            strtxt = strtxt.Replace("\r", "");
            string[] strsplit = strtxt.Split('\n');

            Settings.rvSettings.IgnoreFiles = new List<string>(strsplit);
            for (int i = 0; i < Settings.rvSettings.IgnoreFiles.Count; i++)
            {
                Settings.rvSettings.IgnoreFiles[i] = Settings.rvSettings.IgnoreFiles[i].Trim();
                if (string.IsNullOrEmpty(Settings.rvSettings.IgnoreFiles[i]))
                {
                    Settings.rvSettings.IgnoreFiles.RemoveAt(i);
                    i--;
                }
            }
            Settings.rvSettings.SetRegExRules();

            Settings.rvSettings.DetailedFixReporting = chkDetailedReporting.IsChecked == true;
            Settings.rvSettings.DoubleCheckDelete = chkDoubleCheckDelete.IsChecked == true;
            Settings.rvSettings.DebugLogsEnabled = chkDebugLogs.IsChecked == true;
            Settings.rvSettings.CacheSaveTimerEnabled = chkCacheSaveTimer.IsChecked == true;
            Settings.rvSettings.CacheSaveTimePeriod = (int)(upTime.Value ?? 10);

            Settings.rvSettings.MIACallback = chkSendFoundMIA.IsChecked == true;
            Settings.rvSettings.MIAAnon = chkSendFoundMIAAnon.IsChecked == true;
            Settings.rvSettings.DeleteOldCueFiles = chkDeleteOldCueFiles.IsChecked == true;

            Settings.rvSettings.zstdCompCount = cboCores.SelectedIndex;

            Settings.rvSettings.sevenZDefaultStruct = cbo7zStruct.SelectedIndex;
            Settings.rvSettings.Darkness = chkDarkMode.IsChecked == true;

            Settings.rvSettings.DoNotReportFeedback = chkDoNotReportFeedback.IsChecked == true;

            Settings.WriteConfig(Settings.rvSettings);
            Close();
        }

        private async void BtnDatClick(object sender, RoutedEventArgs e)
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select a folder for DAT Root",
                AllowMultiple = false
            });

            if (folders == null || folders.Count == 0)
                return;

            string selectedPath = folders[0].Path.LocalPath;
            lblDATRoot.Text = RelativePath.MakeRelative(AppDomain.CurrentDomain.BaseDirectory, selectedPath);
        }

        private void ChkSendFoundMIA_CheckedChanged(object sender, RoutedEventArgs e)
        {
            chkSendFoundMIAAnon.IsEnabled = chkSendFoundMIA.IsChecked == true;
        }
    }
}

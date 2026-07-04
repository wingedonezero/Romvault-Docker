using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using DarkAvalonia;
using RomVaultCore;
using RomVaultCore.ReadDat;
using RomVaultCore.RvDB;
using RomVaultCore.Utils;
using RVIO;
using Directory = RVIO.Directory;

namespace ROMVault
{
    public partial class FrmDirectoryMappings : Window
    {
        // Light-mode status colors
        private static readonly Color ColorMagenta = Color.FromRgb(255, 214, 255);
        private static readonly Color ColorGreen = Color.FromRgb(214, 255, 214);
        private static readonly Color ColorYellow = Color.FromRgb(255, 255, 214);
        private static readonly Color ColorRed = Color.FromRgb(255, 214, 214);

        private DirMapping _rule;
        private bool _displayType;

        private readonly ObservableCollection<DirMapping> _gridItems = new ObservableCollection<DirMapping>();

        public FrmDirectoryMappings()
        {
            InitializeComponent();

            DGDirectoryMappingRules.ItemsSource = _gridItems;

            Activated += FrmSetDirActivated;
        }

        public void SetLocation(string dLocation)
        {
            _rule = FindRule(dLocation);
            SetDisplay();
            UpdateGrid();
        }

        public void SetDisplayType(bool type)
        {
            _displayType = type;
            btnDelete.IsVisible = type;

            // Hide the bottom section (grid and bottom buttons) when in compact mode
            lblDelete.IsVisible = !type;
            DGDirectoryMappingRules.IsVisible = !type;
            btnDeleteSelected.IsVisible = !type;
            btnResetAll.IsVisible = !type;
            btnClose.IsVisible = !type;

            MinHeight = type ? 150 : 300;
            Height = type ? 155 : 428;
            CanResize = !type;
        }

        private static DirMapping FindRule(string dLocation)
        {
            foreach (DirMapping t in Settings.rvSettings.DirMappings)
            {
                if (string.Compare(t.DirKey, dLocation, StringComparison.Ordinal) == 0)
                    return t;
            }

            return new DirMapping { DirKey = dLocation };
        }

        private void SetDisplay()
        {
            txtDATLocation.Text = _rule.DirKey;
            txtROMLocation.Text = _rule.DirPath;
        }

        private void UpdateGrid()
        {
            _gridItems.Clear();
            foreach (DirMapping t in Settings.rvSettings.DirMappings)
            {
                _gridItems.Add(t);
            }

            DGDirectoryMappingRules.SelectedIndex = -1;
        }

        private void DGDirectoryMappingRules_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.DataContext is not DirMapping mapping)
                return;

            Color? rowColor = null;

            if (mapping.DirPath == "ToSort")
            {
                rowColor = ColorMagenta;
            }
            else if (_rule != null && mapping == _rule)
            {
                rowColor = ColorGreen;
            }
            else if (_rule != null && mapping.DirKey.Length > _rule.DirKey.Length)
            {
                string separator = OperatingSystem.IsWindows() ? "\\" : "/";
                if (mapping.DirKey.Substring(0, _rule.DirKey.Length + 1) == _rule.DirKey + separator)
                {
                    rowColor = ColorYellow;
                }
            }

            // Check if directory does not exist - override the location cell color
            bool dirMissing = !string.IsNullOrEmpty(mapping.DirPath) && !Directory.Exists(mapping.DirPath);

            if (dirMissing)
                rowColor = ColorRed;

            if (rowColor.HasValue)
            {
                var bgColor = dark.StatusColor(rowColor.Value);
                var fgColor = MainWindow.Contrasty(bgColor);
                e.Row.Background = new SolidColorBrush(bgColor);
                TextElement.SetForeground(e.Row, new SolidColorBrush(fgColor));
            }
            else
            {
                e.Row.ClearValue(DataGridRow.BackgroundProperty);
                e.Row.ClearValue(TextElement.ForegroundProperty);
            }
        }

        private void btnClearROMLocation_Click(object sender, RoutedEventArgs e)
        {
            if (_rule.DirKey == "RomVault")
            {
                txtROMLocation.Text = "RomRoot";
                return;
            }

            if (_rule.DirKey == "ToSort")
            {
                txtROMLocation.Text = "ToSort";
                return;
            }

            txtROMLocation.Text = null;
        }

        private async void BtnSetROMLocationClick(object sender, RoutedEventArgs e)
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Please select a folder for This Rom Set",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                string selectedPath = folders[0].Path.LocalPath;
                string relPath = RelativePath.MakeRelative(AppContext.BaseDirectory, selectedPath);
                txtROMLocation.Text = relPath;
            }
        }

        private async void BtnApplyClick(object sender, RoutedEventArgs e)
        {
            string newDir = txtROMLocation.Text;
            if (string.IsNullOrWhiteSpace(newDir))
            {
                await ShowMessage("No Directory Selected", "You must select a directory.");
                return;
            }
            if (!Directory.Exists(newDir))
            {
                await ShowMessage("Directory does not exist", "The directory you have selected does not exist.");
                return;
            }

            _rule.DirPath = newDir;

            bool updatingRule = false;
            int i;
            for (i = 0; i < Settings.rvSettings.DirMappings.Count; i++)
            {
                if (Settings.rvSettings.DirMappings[i] == _rule)
                {
                    updatingRule = true;
                    break;
                }

                if (string.Compare(Settings.rvSettings.DirMappings[i].DirKey, _rule.DirKey, StringComparison.Ordinal) > 0)
                {
                    break;
                }
            }

            if (!updatingRule)
                Settings.rvSettings.DirMappings.Insert(i, _rule);

            UpdateGrid();
            Settings.WriteConfig(Settings.rvSettings);

            if (_displayType)
                Close();
        }

        private async void BtnDeleteClick(object sender, RoutedEventArgs e)
        {
            string datLocation = _rule.DirKey;

            if (datLocation == "RomVault")
            {
                await ShowMessage("RomVault Rom Location", "You cannot delete the base RomVault directory mapping");
                return;
            }

            DatUpdate.CheckAllDats(DB.DirRoot.Child(0), datLocation);
            for (int i = 0; i < Settings.rvSettings.DirMappings.Count; i++)
            {
                if (Settings.rvSettings.DirMappings[i].DirKey == datLocation)
                {
                    Settings.rvSettings.DirMappings.RemoveAt(i);
                    i--;
                }
            }

            Settings.WriteConfig(Settings.rvSettings);

            UpdateGrid();
            Close();
        }

        private async void BtnDeleteSelectedClick(object sender, RoutedEventArgs e)
        {
            var selectedItems = DGDirectoryMappingRules.SelectedItems;
            if (selectedItems == null || selectedItems.Count == 0)
                return;

            // Collect items to process (iterate a copy since we modify the source)
            var toProcess = new System.Collections.Generic.List<DirMapping>();
            foreach (var item in selectedItems)
            {
                if (item is DirMapping dm)
                    toProcess.Add(dm);
            }

            foreach (var dm in toProcess)
            {
                string datLocation = dm.DirKey;

                if (datLocation == "RomVault")
                {
                    await ShowMessage("RomVault Rom Location", "You cannot delete the " + datLocation + " Directory Settings");
                }
                else
                {
                    for (int i = 0; i < Settings.rvSettings.DirMappings.Count; i++)
                    {
                        if (Settings.rvSettings.DirMappings[i].DirKey == datLocation)
                        {
                            Settings.rvSettings.DirMappings.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
            Settings.WriteConfig(Settings.rvSettings);

            UpdateGrid();
        }

        private void BtnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DataGridGamesDoubleClick(object sender, Avalonia.Input.TappedEventArgs e)
        {
            if (DGDirectoryMappingRules.SelectedItem is not DirMapping selected)
                return;

            Title = "Edit Existing Directory / DATs Mapping";
            _rule = selected;
            UpdateGrid();
            SetDisplay();
        }

        private void FrmSetDirActivated(object sender, EventArgs e)
        {
            DGDirectoryMappingRules.SelectedIndex = -1;
        }

        private void BtnResetAllClick(object sender, RoutedEventArgs e)
        {
            Settings.rvSettings.ResetDirMappings();
            Settings.WriteConfig(Settings.rvSettings);
            _rule = Settings.rvSettings.DirMappings[0];
            UpdateGrid();
            SetDisplay();
        }

        private async System.Threading.Tasks.Task ShowMessage(string title, string message)
        {
            var msgWindow = new Window
            {
                Title = title,
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(16),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = TextWrapping.Wrap
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            Margin = new Avalonia.Thickness(0, 16, 0, 0)
                        }
                    }
                }
            };
            ((Button)((StackPanel)msgWindow.Content).Children[1]).Click += (s, ev) => msgWindow.Close();
            await msgWindow.ShowDialog(this);
        }
    }
}

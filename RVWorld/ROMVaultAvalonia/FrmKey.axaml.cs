using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using RomVaultCore;

namespace ROMVault
{
    public partial class FrmKey : Window
    {
        public FrmKey()
        {
            InitializeComponent();
            Opened += FrmKey_Load;
        }

        private void AddSectionLabel(string text)
        {
            mainPanel.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 4, 0, 4)
            });
        }

        private void FrmKey_Load(object sender, EventArgs e)
        {
            List<RepStatus> displayList = new List<RepStatus>
            {
                RepStatus.Correct,
                RepStatus.CorrectMIA,
                RepStatus.Missing,
                RepStatus.MissingMIA,
                RepStatus.Unknown,
                RepStatus.UnNeeded,
                RepStatus.NotCollected,
                RepStatus.InToSort,
                RepStatus.Ignore,

                RepStatus.CanBeFixed,
                RepStatus.CanBeFixedMIA,
                RepStatus.NeededForFix,
                RepStatus.Rename,
                RepStatus.MoveToSort,
                RepStatus.Incomplete,
                RepStatus.Delete,

                RepStatus.Corrupt,
                RepStatus.UnScanned,
            };

            // Cap height so it doesn't exceed screen; ScrollViewer handles overflow
            Height = Math.Min(displayList.Count * 46 + 110, 700);
            AddSectionLabel("Basic Statuses");

            for (int i = 0; i < displayList.Count; i++)
            {
                if (i == 9)
                    AddSectionLabel("Fix Statuses");

                if (i == 16)
                    AddSectionLabel("Problem Statuses");

                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2) };

                var img = new Image
                {
                    Width = 48,
                    Height = 42,
                    Stretch = Stretch.None
                };

                Bitmap bmp = rvImages.GetBitmap("G_" + displayList[i]);
                if (bmp != null)
                    img.Source = bmp;

                var border = new Border
                {
                    BorderBrush = this.FindResource("SystemControlForegroundBaseMediumBrush") as IBrush ?? Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Background = this.FindResource("RvBgBrush0") as IBrush ?? Brushes.White,
                    Child = img
                };
                row.Children.Add(border);

                string text = displayList[i] switch
                {
                    RepStatus.Missing => "Red - This ROM is missing.",
                    RepStatus.MissingMIA => "Salmon - This ROM is known to be private or missing in action (MIA).",
                    RepStatus.Correct => "Green - This ROM is Correct.",
                    RepStatus.CorrectMIA => "SuperGreen - The ROM was known to be MIA (Missing In Action), but you found it. (Good Job!)",
                    RepStatus.NotCollected => "Gray - The ROM is not collected here because it belongs in the parent or primary deduped set.",
                    RepStatus.UnNeeded => "Light Cyan - The ROM is not needed here because it belongs in the parent or primary deduped set.",
                    RepStatus.Unknown => "Cyan - The ROM is not needed here. Use 'Find Fixes' to see what should be done with the ROM.",
                    RepStatus.InToSort => "Magenta - The ROM is in a ToSort directory.",
                    RepStatus.Corrupt => "Red - This file is corrupt.",
                    RepStatus.UnScanned => "Blue - The file could not be scanned. The file could be locked or have incompatible permissions.",
                    RepStatus.Ignore => "GreyBlue - The file matches an ignore rule.",
                    RepStatus.CanBeFixed => "Yellow - The ROM is missing here, but it's available elsewhere. The ROM will be fixed.",
                    RepStatus.CanBeFixedMIA => "SuperYellow - The MIA ROM is missing here, but it's available elsewhere. The ROM will be fixed.",
                    RepStatus.MoveToSort => "Purple - The ROM is not needed here, but a copy isn't located elsewhere. The ROM will be moved to the Primary ToSort.",
                    RepStatus.Delete => "Brown - The ROM is not needed here, but a copy is located elsewhere. The ROM will be deleted.",
                    RepStatus.NeededForFix => "Orange - The ROM is not needed here, but it's needed elsewhere. The ROM will be moved.",
                    RepStatus.Rename => "Light Orange - The ROM is needed here, but has the incorrect name. The ROM will be renamed.",
                    RepStatus.Incomplete => "Pink - This is a ROM that could be fixed, but will not be because it is part of an incomplete set.",
                    _ => ""
                };

                var label = new Border
                {
                    BorderBrush = this.FindResource("SystemControlForegroundBaseMediumBrush") as IBrush ?? Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    MinWidth = 538,
                    MinHeight = 42,
                    Margin = new Thickness(2, 0, 0, 0),
                    Child = new TextBlock
                    {
                        Text = text,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(4),
                        Foreground = Brushes.White
                    }
                };
                row.Children.Add(label);

                mainPanel.Children.Add(row);
            }
        }
    }
}

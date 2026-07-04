using Avalonia;
using Avalonia.Controls;

namespace ROMVault
{
    public partial class MainWindow
    {
        // Width of the artwork side panel; splitListArt_pos persists this value
        // (WinForms persists the SplitContainer distance; the art panel width is
        // the deterministic equivalent in the Avalonia Grid layout).
        internal double artPanelWidth = 172;

        private void ReadDefaults()
        {
            defaults defaults = defaults.ReadDefaults();
            if (defaults != null)
            {
                if (defaults.mainX > -30000 && defaults.mainY > -30000 && defaults.mainHeight > 50)
                {
                    WindowStartupLocation = WindowStartupLocation.Manual;
                    Position = new PixelPoint(defaults.mainX, defaults.mainY);
                    Width = defaults.mainWidth;
                    Height = defaults.mainHeight;
                }

                if (defaults.splitDatInfoGameInfo_pos != int.MinValue) splitDatInfoGameInfo.ColumnDefinitions[0].Width = new GridLength(defaults.splitDatInfoGameInfo_pos);
                if (defaults.splitGameListRomList_pos != int.MinValue) splitGameListRomList.RowDefinitions[0].Height = new GridLength(defaults.splitGameListRomList_pos);
                if (defaults.splitListArt_pos != int.MinValue) artPanelWidth = defaults.splitListArt_pos;

                if (defaults.gg0_width != int.MinValue) GameGrid.Columns[0].Width = new DataGridLength(defaults.gg0_width);
                if (defaults.gg1_width != int.MinValue) GameGrid.Columns[1].Width = new DataGridLength(defaults.gg1_width);
                if (defaults.gg2_width != int.MinValue) GameGrid.Columns[2].Width = new DataGridLength(defaults.gg2_width);
                if (defaults.gg3_width != int.MinValue) GameGrid.Columns[3].Width = new DataGridLength(defaults.gg3_width);

                if (defaults.rg0_width != int.MinValue) RomGrid.Columns[0].Width = new DataGridLength(defaults.rg0_width);
                if (defaults.rg1_width != int.MinValue) RomGrid.Columns[1].Width = new DataGridLength(defaults.rg1_width);
                if (defaults.rg2_width != int.MinValue) RomGrid.Columns[2].Width = new DataGridLength(defaults.rg2_width);
                if (defaults.rg3_width != int.MinValue) RomGrid.Columns[3].Width = new DataGridLength(defaults.rg3_width);
                if (defaults.rg4_width != int.MinValue) RomGrid.Columns[4].Width = new DataGridLength(defaults.rg4_width);
                if (defaults.rg5_width != int.MinValue) RomGrid.Columns[5].Width = new DataGridLength(defaults.rg5_width);
                if (defaults.rg6_width != int.MinValue) RomGrid.Columns[6].Width = new DataGridLength(defaults.rg6_width);
                if (defaults.rg7_width != int.MinValue) RomGrid.Columns[7].Width = new DataGridLength(defaults.rg7_width);
                if (defaults.rg8_width != int.MinValue) RomGrid.Columns[8].Width = new DataGridLength(defaults.rg8_width);
                if (defaults.rg9_width != int.MinValue) RomGrid.Columns[9].Width = new DataGridLength(defaults.rg9_width);
                if (defaults.rg10_width != int.MinValue) RomGrid.Columns[10].Width = new DataGridLength(defaults.rg10_width);
                if (defaults.rg11_width != int.MinValue) RomGrid.Columns[11].Width = new DataGridLength(defaults.rg11_width);
                if (defaults.rg12_width != int.MinValue) RomGrid.Columns[12].Width = new DataGridLength(defaults.rg12_width);
                if (defaults.rg13_width != int.MinValue) RomGrid.Columns[13].Width = new DataGridLength(defaults.rg13_width);
                if (defaults.rg14_width != int.MinValue) RomGrid.Columns[14].Width = new DataGridLength(defaults.rg14_width);

                if (defaults.nfo_FontSize != int.MinValue)
                {
                    trbFontSize.Value = defaults.nfo_FontSize;
                    trbFontSize2.Value = defaults.nfo_FontSize;
                }
            }
        }

        private void WriteDefaults()
        {
            defaults df = new defaults();
            if (WindowState == WindowState.Normal)
            {
                df.mainX = Position.X;
                df.mainY = Position.Y;
                df.mainWidth = (int)Width;
                df.mainHeight = (int)Height;
            }
            else
            {
                // Minimized/maximized: Avalonia has no RestoreBounds, so carry
                // over the last saved normal geometry instead of the current one.
                defaults prev = defaults.ReadDefaults();
                if (prev != null)
                {
                    df.mainX = prev.mainX;
                    df.mainY = prev.mainY;
                    df.mainWidth = prev.mainWidth;
                    df.mainHeight = prev.mainHeight;
                }
                else
                {
                    df.mainX = Position.X;
                    df.mainY = Position.Y;
                    df.mainWidth = (int)Width;
                    df.mainHeight = (int)Height;
                }
            }

            df.splitDatInfoGameInfo_pos = (int)splitDatInfoGameInfo.ColumnDefinitions[0].ActualWidth;
            df.splitGameListRomList_pos = (int)splitGameListRomList.RowDefinitions[0].ActualHeight;
            df.splitListArt_pos = TabEmuArc.IsVisible ? (int)splitListArt.ColumnDefinitions[2].ActualWidth : (int)artPanelWidth;

            df.gg0_width = (int)GameGrid.Columns[0].ActualWidth;
            df.gg1_width = (int)GameGrid.Columns[1].ActualWidth;
            df.gg2_width = (int)GameGrid.Columns[2].ActualWidth;
            df.gg3_width = (int)GameGrid.Columns[3].ActualWidth;

            df.rg0_width = (int)RomGrid.Columns[0].ActualWidth;
            df.rg1_width = (int)RomGrid.Columns[1].ActualWidth;
            df.rg2_width = (int)RomGrid.Columns[2].ActualWidth;
            df.rg3_width = (int)RomGrid.Columns[3].ActualWidth;
            df.rg4_width = (int)RomGrid.Columns[4].ActualWidth;
            df.rg5_width = (int)RomGrid.Columns[5].ActualWidth;
            df.rg6_width = (int)RomGrid.Columns[6].ActualWidth;
            df.rg7_width = (int)RomGrid.Columns[7].ActualWidth;
            df.rg8_width = (int)RomGrid.Columns[8].ActualWidth;
            df.rg9_width = (int)RomGrid.Columns[9].ActualWidth;
            df.rg10_width = (int)RomGrid.Columns[10].ActualWidth;
            df.rg11_width = (int)RomGrid.Columns[11].ActualWidth;
            df.rg12_width = (int)RomGrid.Columns[12].ActualWidth;
            df.rg13_width = (int)RomGrid.Columns[13].ActualWidth;
            df.rg14_width = (int)RomGrid.Columns[14].ActualWidth;

            df.nfo_FontSize = (int)trbFontSize.Value;

            defaults.WriteDefaults(df);
        }
    }
}

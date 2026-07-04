/******************************************************
 *     ROMVault3 is written by Gordon J.              *
 *     Contact gordon@romvault.com                    *
 *     Copyright 2025                                 *
 ******************************************************/

using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using RomVaultCore;
using RomVaultCore.RvDB;

namespace ROMVault
{
    public partial class MainWindow
    {
        private TextBlock _labelGameName;
        private TextBox _textGameName;

        private TextBlock _labelGameDescription;
        private TextBox _textGameDescription;

        private TextBlock _labelGameManufacturer;
        private TextBox _textGameManufacturer;

        private TextBlock _labelGameCloneOf;
        private TextBox _textGameCloneOf;

        private TextBlock _labelGameRomOf;
        private TextBox _textGameRomOf;

        private TextBlock _labelGameYear;
        private TextBox _textGameYear;

        private TextBlock _labelGameCategory;
        private TextBox _textGameCategory;

        // Trurip Extra Data
        private TextBlock _labelTruripPublisher;
        private TextBox _textTruripPublisher;

        private TextBlock _labelTruripDeveloper;
        private TextBox _textTruripDeveloper;

        private TextBlock _labelTruripTitleId;
        private TextBox _textTruripTitleId;

        private TextBlock _labelTruripSource;
        private TextBox _textTruripSource;

        private TextBlock _labelTruripCloneOf;
        private TextBox _textTruripCloneOf;

        private TextBlock _labelTruripRelatedTo;
        private TextBox _textTruripRelatedTo;

        private TextBlock _labelTruripYear;
        private TextBox _textTruripYear;

        private TextBlock _labelTruripPlayers;
        private TextBox _textTruripPlayers;

        private TextBlock _labelTruripGenre;
        private TextBox _textTruripGenre;

        private TextBlock _labelTruripSubGenre;
        private TextBox _textTruripSubGenre;

        private TextBlock _labelTruripRatings;
        private TextBox _textTruripRatings;

        private TextBlock _labelTruripScore;
        private TextBox _textTruripScore;

        // We use a Canvas-like approach with absolute positioning inside gbSetInfo
        // to match the original WinForms layout with pixel-based positioning.
        // The gbSetInfo is the Grid named in AXAML.

        private void AddTextBox(int line, string name, int x, int x1, out TextBlock lBox, out TextBox tBox)
        {
            int y = 14 + line * 16;

            lBox = new TextBlock
            {
                Margin = new Thickness(x, y + 1, 0, 0),
                Width = x1 - x - 2,
                Text = name + " :",
                TextAlignment = Avalonia.Media.TextAlignment.Right,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            tBox = new TextBox
            {
                Margin = new Thickness(x1, y, 0, 0),
                Width = 20, // Initial width, updated in resize
                Height = 17,
                IsReadOnly = true,
                IsTabStop = false,
                FontSize = 11,
                Padding = new Thickness(2, 1),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                BorderThickness = new Thickness(1)
            };

            gbSetInfo.Children.Add(lBox);
            gbSetInfo.Children.Add(tBox);
        }

        private void AddGameMetaData()
        {
            AddTextBox(0, "Name", 6, 84, out _labelGameName, out _textGameName);
            AddTextBox(1, "Description", 6, 84, out _labelGameDescription, out _textGameDescription);
            AddTextBox(2, "Manufacturer", 6, 84, out _labelGameManufacturer, out _textGameManufacturer);

            AddTextBox(3, "Clone of", 6, 84, out _labelGameCloneOf, out _textGameCloneOf);
            AddTextBox(3, "Year", 206, 284, out _labelGameYear, out _textGameYear);

            AddTextBox(4, "Rom of", 6, 84, out _labelGameRomOf, out _textGameRomOf);
            AddTextBox(4, "Category", 206, 284, out _labelGameCategory, out _textGameCategory);

            // Trurip
            AddTextBox(2, "Publisher", 6, 84, out _labelTruripPublisher, out _textTruripPublisher);
            AddTextBox(2, "Title Id", 406, 484, out _labelTruripTitleId, out _textTruripTitleId);

            AddTextBox(3, "Developer", 6, 84, out _labelTruripDeveloper, out _textTruripDeveloper);
            AddTextBox(3, "Source", 406, 484, out _labelTruripSource, out _textTruripSource);

            AddTextBox(4, "Clone of", 6, 84, out _labelTruripCloneOf, out _textTruripCloneOf);
            AddTextBox(5, "Related to", 6, 84, out _labelTruripRelatedTo, out _textTruripRelatedTo);

            AddTextBox(6, "Year", 6, 84, out _labelTruripYear, out _textTruripYear);
            AddTextBox(6, "Genre", 206, 284, out _labelTruripGenre, out _textTruripGenre);
            AddTextBox(6, "Ratings", 406, 484, out _labelTruripRatings, out _textTruripRatings);

            AddTextBox(7, "Players", 6, 84, out _labelTruripPlayers, out _textTruripPlayers);
            AddTextBox(7, "SubGenre", 206, 284, out _labelTruripSubGenre, out _textTruripSubGenre);
            AddTextBox(7, "Score", 406, 484, out _labelTruripScore, out _textTruripScore);

            // Wire up SizeChanged so TextBoxes resize after layout and on window resize
            // (In the constructor, gbSetInfo.Bounds.Width is 0 — layout hasn't happened yet)
            gbSetInfo.SizeChanged += (s, e) => gbSetInfo_Resize(s, e);

            UpdateGameMetaData(new RvFile(FileType.Dir));
        }

        private void UpdateGameMetaData(RvFile tGame)
        {
            _labelGameName.IsVisible = true;
            _textGameName.Text = tGame.Name;
            string gameId = tGame.Game?.GetData(RvGame.GameData.Id);
            if (!string.IsNullOrWhiteSpace(gameId))
                _textGameName.Text += $" (ID:{gameId})";

            if (tGame.Game == null)
            {
                _labelGameDescription.IsVisible = false;
                _textGameDescription.IsVisible = false;
            }

            if (tGame.Game == null || tGame.Game.GetData(RvGame.GameData.EmuArc) != "yes")
            {
                _labelTruripPublisher.IsVisible = false;
                _textTruripPublisher.IsVisible = false;

                _labelTruripDeveloper.IsVisible = false;
                _textTruripDeveloper.IsVisible = false;

                _labelTruripTitleId.IsVisible = false;
                _textTruripTitleId.IsVisible = false;

                _labelTruripSource.IsVisible = false;
                _textTruripSource.IsVisible = false;

                _labelTruripCloneOf.IsVisible = false;
                _textTruripCloneOf.IsVisible = false;

                _labelTruripRelatedTo.IsVisible = false;
                _textTruripRelatedTo.IsVisible = false;

                _labelTruripYear.IsVisible = false;
                _textTruripYear.IsVisible = false;

                _labelTruripPlayers.IsVisible = false;
                _textTruripPlayers.IsVisible = false;

                _labelTruripGenre.IsVisible = false;
                _textTruripGenre.IsVisible = false;

                _labelTruripSubGenre.IsVisible = false;
                _textTruripSubGenre.IsVisible = false;

                _labelTruripRatings.IsVisible = false;
                _textTruripRatings.IsVisible = false;

                _labelTruripScore.IsVisible = false;
                _textTruripScore.IsVisible = false;
            }

            if (tGame.Game == null || tGame.Game.GetData(RvGame.GameData.EmuArc) == "yes")
            {
                _labelGameManufacturer.IsVisible = false;
                _textGameManufacturer.IsVisible = false;

                _labelGameCloneOf.IsVisible = false;
                _textGameCloneOf.IsVisible = false;

                _labelGameRomOf.IsVisible = false;
                _textGameRomOf.IsVisible = false;

                _labelGameYear.IsVisible = false;
                _textGameYear.IsVisible = false;

                _labelGameCategory.IsVisible = false;
                _textGameCategory.IsVisible = false;
            }

            if (tGame.Game != null)
            {
                if (tGame.Game.GetData(RvGame.GameData.EmuArc) == "yes")
                {
                    _labelGameDescription.IsVisible = true;
                    _textGameDescription.IsVisible = true;
                    string desc = tGame.Game.GetData(RvGame.GameData.Description);
                    if (desc == "\u00A4") desc = Path.GetFileNameWithoutExtension(tGame.Name);
                    _textGameDescription.Text = desc;

                    _labelTruripPublisher.IsVisible = true;
                    _textTruripPublisher.IsVisible = true;
                    _textTruripPublisher.Text = tGame.Game.GetData(RvGame.GameData.Publisher);

                    _labelTruripDeveloper.IsVisible = true;
                    _textTruripDeveloper.IsVisible = true;
                    _textTruripDeveloper.Text = tGame.Game.GetData(RvGame.GameData.Developer);

                    _labelTruripTitleId.IsVisible = true;
                    _textTruripTitleId.IsVisible = true;
                    _textTruripTitleId.Text = tGame.Game.GetData(RvGame.GameData.Id);

                    _labelTruripSource.IsVisible = true;
                    _textTruripSource.IsVisible = true;
                    _textTruripSource.Text = tGame.Game.GetData(RvGame.GameData.Source);

                    _labelTruripCloneOf.IsVisible = true;
                    _textTruripCloneOf.IsVisible = true;
                    _textTruripCloneOf.Text = tGame.Game.GetData(RvGame.GameData.CloneOf);

                    _labelTruripRelatedTo.IsVisible = true;
                    _textTruripRelatedTo.IsVisible = true;
                    _textTruripRelatedTo.Text = tGame.Game.GetData(RvGame.GameData.RelatedTo);

                    _labelTruripYear.IsVisible = true;
                    _textTruripYear.IsVisible = true;
                    _textTruripYear.Text = tGame.Game.GetData(RvGame.GameData.Year);

                    _labelTruripPlayers.IsVisible = true;
                    _textTruripPlayers.IsVisible = true;
                    _textTruripPlayers.Text = tGame.Game.GetData(RvGame.GameData.Players);

                    _labelTruripGenre.IsVisible = true;
                    _textTruripGenre.IsVisible = true;
                    _textTruripGenre.Text = tGame.Game.GetData(RvGame.GameData.Genre);

                    _labelTruripSubGenre.IsVisible = true;
                    _textTruripSubGenre.IsVisible = true;
                    _textTruripSubGenre.Text = tGame.Game.GetData(RvGame.GameData.SubGenre);

                    _labelTruripRatings.IsVisible = true;
                    _textTruripRatings.IsVisible = true;
                    _textTruripRatings.Text = tGame.Game.GetData(RvGame.GameData.Ratings);

                    _labelTruripScore.IsVisible = true;
                    _textTruripScore.IsVisible = true;
                    _textTruripScore.Text = tGame.Game.GetData(RvGame.GameData.Score);

                    LoadTruRipPannel(tGame);
                }
                else
                {
                    bool found = false;
                    string path = tGame.Parent.DatTreeFullName;
                    foreach (EmulatorInfo ei in Settings.rvSettings.EInfo)
                    {
                        if (path.Length <= 8)
                            continue;

                        if (!string.Equals(path.Substring(8), ei.TreeDir, StringComparison.CurrentCultureIgnoreCase))
                            continue;

                        if (string.IsNullOrWhiteSpace(ei.ExtraPath))
                            continue;

                        if (ei.ExtraPath != null)
                        {
                            found = true;
                            if (ei.ExtraPath.Substring(0, 1) == "%")
                                LoadMameSLPannels(tGame, ei.ExtraPath.Substring(1));
                            else
                                LoadMamePannels(tGame, ei.ExtraPath);

                            break;
                        }
                    }

                    if (!found)
                        found = LoadNFOPannel(tGame);

                    if (!found)
                        found = LoadC64Pannel(tGame);

                    if (!found)
                        HidePannel();

                    _labelGameDescription.IsVisible = true;
                    _textGameDescription.IsVisible = true;
                    string desc = tGame.Game.GetData(RvGame.GameData.Description);
                    if (desc == "\u00A4") desc = Path.GetFileNameWithoutExtension(tGame.Name);
                    _textGameDescription.Text = desc;

                    _labelGameManufacturer.IsVisible = true;
                    _textGameManufacturer.IsVisible = true;
                    _textGameManufacturer.Text = tGame.Game.GetData(RvGame.GameData.Manufacturer);

                    _labelGameCloneOf.IsVisible = true;
                    _textGameCloneOf.IsVisible = true;
                    _textGameCloneOf.Text = tGame.Game.GetData(RvGame.GameData.CloneOf);

                    _labelGameRomOf.IsVisible = true;
                    _textGameRomOf.IsVisible = true;
                    _textGameRomOf.Text = tGame.Game.GetData(RvGame.GameData.RomOf);

                    _labelGameYear.IsVisible = true;
                    _textGameYear.IsVisible = true;
                    _textGameYear.Text = tGame.Game.GetData(RvGame.GameData.Year);

                    _labelGameCategory.IsVisible = true;
                    _textGameCategory.IsVisible = true;
                    _textGameCategory.Text = tGame.Game.GetData(RvGame.GameData.Category);
                }
            }
            else
            {
                HidePannel();
            }

            // In Avalonia, focus is handled differently; set focus to GameGrid
            GameGrid.Focus();
        }

        private void gbSetInfo_Resize(object sender, EventArgs e)
        {
            const int leftPos = 84;
            int rightPos = (int)gbSetInfo.Bounds.Width - 15;
            if (rightPos < leftPos + 10)
                rightPos = leftPos + 10;
            if (rightPos > 750)
                rightPos = 750;

            int width = rightPos - leftPos;

            if (_textGameName == null)
                return;

            // Main Meta Data
            int textWidth = (int)((double)width * 120 / 340);
            int text2Left = leftPos + width - textWidth;
            int label2Left = text2Left - 78;

            _textGameName.Width = width;
            _textGameDescription.Width = width;
            _textGameManufacturer.Width = width;

            _textGameCloneOf.Width = textWidth;

            _labelGameYear.Margin = new Thickness(label2Left, _labelGameYear.Margin.Top, 0, 0);
            _textGameYear.Margin = new Thickness(text2Left, _textGameYear.Margin.Top, 0, 0);
            _textGameYear.Width = textWidth;

            _textGameRomOf.Width = textWidth;

            _labelGameCategory.Margin = new Thickness(label2Left, _labelGameCategory.Margin.Top, 0, 0);
            _textGameCategory.Margin = new Thickness(text2Left, _textGameCategory.Margin.Top, 0, 0);
            _textGameCategory.Width = textWidth;

            // TruRip Meta Data
            textWidth = (int)(width * 0.20);
            text2Left = (int)(width * 0.4 + leftPos);
            label2Left = text2Left - 78;
            int text3Left = leftPos + width - textWidth;
            int label3Left = text3Left - 78;

            _textTruripPublisher.Width = (int)(width * 0.6);
            _textTruripDeveloper.Width = (int)(width * 0.6);
            _textTruripCloneOf.Width = width;
            _textTruripRelatedTo.Width = width;

            _textTruripYear.Width = textWidth;
            _textTruripPlayers.Width = textWidth;

            _labelTruripGenre.Margin = new Thickness(label2Left, _labelTruripGenre.Margin.Top, 0, 0);
            _textTruripGenre.Margin = new Thickness(text2Left, _textTruripGenre.Margin.Top, 0, 0);
            _textTruripGenre.Width = textWidth;

            _labelTruripSubGenre.Margin = new Thickness(label2Left, _labelTruripSubGenre.Margin.Top, 0, 0);
            _textTruripSubGenre.Margin = new Thickness(text2Left, _textTruripSubGenre.Margin.Top, 0, 0);
            _textTruripSubGenre.Width = textWidth;

            _labelTruripTitleId.Margin = new Thickness(label3Left, _labelTruripTitleId.Margin.Top, 0, 0);
            _textTruripTitleId.Margin = new Thickness(text3Left, _textTruripTitleId.Margin.Top, 0, 0);
            _textTruripTitleId.Width = textWidth;

            _labelTruripSource.Margin = new Thickness(label3Left, _labelTruripSource.Margin.Top, 0, 0);
            _textTruripSource.Margin = new Thickness(text3Left, _textTruripSource.Margin.Top, 0, 0);
            _textTruripSource.Width = textWidth;

            _labelTruripRatings.Margin = new Thickness(label3Left, _labelTruripRatings.Margin.Top, 0, 0);
            _textTruripRatings.Margin = new Thickness(text3Left, _textTruripRatings.Margin.Top, 0, 0);
            _textTruripRatings.Width = textWidth;

            _labelTruripScore.Margin = new Thickness(label3Left, _labelTruripScore.Margin.Top, 0, 0);
            _textTruripScore.Margin = new Thickness(text3Left, _textTruripScore.Margin.Top, 0, 0);
            _textTruripScore.Width = textWidth;
        }
    }
}

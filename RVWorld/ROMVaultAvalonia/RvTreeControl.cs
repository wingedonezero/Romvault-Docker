/******************************************************
 *     ROMVault3 is written by Gordon J.              *
 *     Contact gordon@romvault.com                    *
 *     Copyright 2025                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.VisualTree;
using DarkAvalonia;
using RomVaultCore;
using RomVaultCore.RvDB;

namespace ROMVault
{
    public class RvTreeControl : Control
    {
        public bool Working;

        private class UiTree
        {
            public string TreeBranches;
            public Rect RTree;
            public Rect RExpand;
            public Rect RChecked;
            public Rect RIcon;
            public Rect RText;
        }

        // Events using Avalonia event system
        public event EventHandler<RvTreeEventArgs> RvSelected;
        public event EventHandler<RvTreeEventArgs> RvChecked;

        private RvFile _lTree;

        private readonly Typeface _tTypeface = new Typeface("Microsoft Sans Serif");
        private readonly double _tFontSize = 11; // ~8pt
        private readonly double _tFontSize1 = 9.5; // ~7pt

        private double _contentWidth = 500;
        private double _contentHeight;

        // Scroll offsets set by the parent ScrollViewer
        private double _hScroll;
        private double _vScroll;

        public RvTreeControl()
        {
            ClipToBounds = true;
            Focusable = true;
        }

        public RvFile Selected { get; private set; }

        #region "Setup"

        private int _yPos;

        public void Setup(ref RvFile dirTree, bool keepSelected = false)
        {
            if (!keepSelected)
                Selected = null;
            _lTree = dirTree;
            SetupInt();
        }

        private void SetupInt()
        {
            _yPos = 0;
            _contentWidth = 0;

            if (_lTree == null)
            {
                _contentHeight = 0;
                InvalidateMeasure();
                InvalidateVisual();
                return;
            }

            int treeCount = _lTree.ChildCount;

            if (treeCount >= 1)
            {
                for (int i = 0; i < treeCount - 1; i++)
                {
                    SetupTree(_lTree.Child(i), "\u251C"); // "├"
                }

                SetupTree(_lTree.Child(treeCount - 1), "\u2514"); // "└"
            }

            _contentWidth = Math.Max(_contentWidth, 200);
            _contentHeight = _yPos;
            InvalidateMeasure();
            InvalidateVisual();
        }

        private void SetupTree(RvFile pTree, string pTreeBranches)
        {
            int nodeDepth = pTreeBranches.Length - 1;

            int nodeHeight = 16;
            if (pTree.Tree.TreeExpanded && pTree.DirDatCount > 1)
            {
                for (int i = 0; i < pTree.DirDatCount; i++)
                {
                    if (!pTree.DirDat(i).Flag(DatFlags.AutoAddedDirectory))
                        nodeHeight += 12;
                }
            }

            UiTree uTree = new UiTree();
            pTree.Tree.UiObject = uTree;

            uTree.TreeBranches = pTreeBranches;

            uTree.RTree = new Rect(0, _yPos, 1 + nodeDepth * 18, nodeHeight);
            uTree.RExpand = new Rect(5 + nodeDepth * 18, _yPos + 4, 9, 9);
            uTree.RChecked = new Rect(20 + nodeDepth * 18, _yPos + 2, 13, 13);
            uTree.RIcon = new Rect(35 + nodeDepth * 18, _yPos, 16, 16);
            uTree.RText = new Rect(51 + nodeDepth * 18, _yPos, 10000, nodeHeight);

            // Estimate text width for horizontal scrolling
            try
            {
                string nodeText = pTree.Name;
                if (pTree.Dat == null && pTree.DirDatCount == 1)
                    nodeText += ": " + (pTree.DirDat(0).GetData(RvDat.DatData.Description) ?? "");
                int intMIA = pTree.DirStatus.CountMIA();
                nodeText += $" ( Have: {pTree.DirStatus.CountCorrect()} \\ Missing: {pTree.DirStatus.CountMissing()}" +
                    (intMIA > 0 ? $" \\ MIA: {intMIA}" : "") + " )";
                var ft = new FormattedText(nodeText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _tTypeface, _tFontSize, Brushes.Black);
                double nodeWidth = uTree.RText.X + ft.Width + 20;
                if (nodeWidth > _contentWidth)
                    _contentWidth = nodeWidth;
            }
            catch { }

            pTreeBranches = pTreeBranches.Replace("\u251C", "\u2502"); // "├" → "│"
            pTreeBranches = pTreeBranches.Replace("\u2514", " ");     // "└" → " "

            _yPos = _yPos + nodeHeight;

            bool found = false;
            int last = 0;
            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvFile dir = pTree.Child(i);
                if (!dir.IsDirectory)
                    continue;

                if (dir.Tree == null)
                    continue;

                found = true;
                if (pTree.Tree.TreeExpanded)
                    last = i;
            }

            if (!found && pTree.DirDatCount <= 1)
            {
                uTree.RExpand = new Rect(0, 0, 0, 0);
            }

            if (pTree.Tree.TreeExpanded && found)
            {
                uTree.TreeBranches += "\u2510"; // "┐"
            }

            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvFile dir = pTree.Child(i);
                if (!dir.IsDirectory)
                    continue;

                if (dir.Tree == null)
                    continue;

                if (!pTree.Tree.TreeExpanded)
                    continue;

                if (i != last)
                    SetupTree(pTree.Child(i), pTreeBranches + "\u251C"); // "├"
                else
                    SetupTree(pTree.Child(i), pTreeBranches + "\u2514"); // "└"
            }
        }

        #endregion

        #region "Measure / Arrange"

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(_contentWidth, _contentHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return new Size(Math.Max(finalSize.Width, _contentWidth), _contentHeight);
        }

        #endregion

        #region "Paint"

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            // Get scroll offsets from parent ScrollViewer
            var scrollViewer = this.FindAncestorOfType<ScrollViewer>();
            _hScroll = scrollViewer?.Offset.X ?? 0;
            _vScroll = scrollViewer?.Offset.Y ?? 0;

            Rect clipRect = new Rect(_hScroll, _vScroll, Bounds.Width, Bounds.Height);

            // Fill background - use Fluent theme resource
            IBrush bgBrush;
            if (this.TryFindResource("SystemControlBackgroundChromeMediumLowBrush",
                    this.ActualThemeVariant, out var bgRes) && bgRes is IBrush foundBrush)
            {
                bgBrush = foundBrush;
            }
            else
            {
                bgBrush = new SolidColorBrush(
                    Application.Current?.ActualThemeVariant == ThemeVariant.Dark
                        ? Color.Parse("#2B2B2B")
                        : Colors.White);
            }
            context.DrawRectangle(
                bgBrush,
                null,
                new Rect(0, 0, Bounds.Width + _hScroll, Bounds.Height + _vScroll));

            if (_lTree == null)
                return;

            for (int i = 0; i < _lTree.ChildCount; i++)
            {
                RvFile tDir = _lTree.Child(i);
                if (!tDir.IsDirectory)
                    continue;

                if (tDir.Tree?.UiObject != null)
                {
                    PaintTree(tDir, context, clipRect);
                }
            }
        }

        private void PaintTree(RvFile pTree, DrawingContext context, Rect clipRect)
        {
            UiTree uTree = (UiTree)pTree.Tree.UiObject;

            double y = uTree.RTree.Top;

            if (uTree.RTree.Intersects(clipRect))
            {
                ImmutableSolidColorBrush lineBrush;
                if (this.TryFindResource("SystemChromeHighColor",
                        this.ActualThemeVariant, out var lineColorRes) && lineColorRes is Color lineColor)
                {
                    lineBrush = new ImmutableSolidColorBrush(lineColor);
                }
                else
                {
                    lineBrush = Application.Current?.ActualThemeVariant == ThemeVariant.Dark
                        ? new ImmutableSolidColorBrush(Color.Parse("#767676"))
                        : new ImmutableSolidColorBrush(Colors.Gray);
                }
                var pen = new Pen(lineBrush, 1, new DashStyle(new double[] { 1, 1 }, 0));

                string lTree = uTree.TreeBranches;
                for (int j = 0; j < lTree.Length; j++)
                {
                    double x = j * 18;
                    string cTree = lTree.Substring(j, 1);
                    switch (cTree)
                    {
                        case "\u2502": // "│"
                            context.DrawLine(pen, new Point(x + 9, y), new Point(x + 9, y + uTree.RTree.Height));
                            break;

                        case "\u251C": // "├"
                            context.DrawLine(pen, new Point(x + 9, y), new Point(x + 9, y + uTree.RTree.Height));
                            context.DrawLine(pen, new Point(x + 9, y + 8), new Point(x + 27, y + 8));
                            break;
                        case "\u2514": // "└"
                            context.DrawLine(pen, new Point(x + 9, y), new Point(x + 9, y + 8));
                            context.DrawLine(pen, new Point(x + 9, y + 8), new Point(x + 27, y + 8));
                            break;
                        case "\u2510": // "┐"
                            context.DrawLine(pen, new Point(x + 9, y + 8), new Point(x + 9, y + uTree.RTree.Height));
                            break;
                    }
                }
            }

            if (!uTree.RExpand.IsEmpty())
            {
                if (uTree.RExpand.Intersects(clipRect))
                {
                    Bitmap bm = pTree.Tree.TreeExpanded
                        ? rvImages.GetBitmap("ExpandBoxMinus", false)
                        : rvImages.GetBitmap("ExpandBoxPlus", false);
                    if (bm != null)
                        context.DrawImage(bm, uTree.RExpand);
                }
            }

            if (uTree.RChecked.Intersects(clipRect))
            {
                Bitmap bm = null;
                switch (pTree.Tree.Checked)
                {
                    case RvTreeRow.TreeSelect.Locked:
                        bm = rvImages.GetBitmap("TickBoxLocked", false);
                        break;
                    case RvTreeRow.TreeSelect.UnSelected:
                        bm = rvImages.GetBitmap("TickBoxUnTicked", false);
                        break;
                    case RvTreeRow.TreeSelect.Selected:
                        bm = rvImages.GetBitmap("TickBoxTicked", false);
                        break;
                }
                if (bm != null)
                    context.DrawImage(bm, uTree.RChecked);
            }

            if (uTree.RIcon.Intersects(clipRect))
            {
                int icon = 2;
                if (pTree.DirStatus.HasInToSort())
                {
                    icon = 4;
                }
                else if (!pTree.DirStatus.HasCorrect() && pTree.DirStatus.HasMissing())
                {
                    icon = 1;
                }
                else if (!pTree.DirStatus.HasMissing() && pTree.DirStatus.HasMIA())
                {
                    icon = 5;
                }
                else if (!pTree.DirStatus.HasMissing())
                {
                    icon = 3;
                }

                Bitmap bm;
                if (pTree.Dat == null && pTree.DirDatCount == 0) // Directory above DAT's in Tree
                {
                    bm = rvImages.GetBitmap("DirectoryTree" + icon, false);
                }
                else if (pTree.Dat == null && pTree.DirDatCount >= 1) // Directory that contains DAT's
                {
                    bm = rvImages.GetBitmap("Tree" + icon, false);
                }
                else if (pTree.Dat != null && pTree.DirDatCount == 0) // Directories made by a DAT
                {
                    bm = rvImages.GetBitmap("Tree" + icon, false);
                }
                else if (pTree.Dat != null && pTree.DirDatCount >= 1) // Directories made by a DAT
                {
                    bm = rvImages.GetBitmap("Tree" + icon, false);
                }
                else
                {
                    ReportError.SendAndShow("Unknown Tree settings in DisplayTree.");
                    bm = null;
                }

                if (bm != null)
                {
                    context.DrawImage(bm, uTree.RIcon);
                }
            }

            Rect recBackGround = new Rect(uTree.RText.X, uTree.RText.Y, Bounds.Width - uTree.RText.X + _hScroll, uTree.RText.Height);

            if (recBackGround.Intersects(clipRect))
            {
                string thistxt;
                List<string> datList = null;

                int intMIA = pTree.DirStatus.CountMIA();
                int intFoundMIA = pTree.DirStatus.CountFoundMIA();

                string strMIA = intMIA > 0 ? $" \\ MIA: {intMIA}" : "";
                string strFoundMIA = intFoundMIA > 0 ? $" \\ Found MIA: {intFoundMIA}" : "";
                string subtxt = $"( Have: {pTree.DirStatus.CountCorrect()}{strFoundMIA} \\ Missing: {pTree.DirStatus.CountMissing()}{strMIA} )";

                if (pTree.Dat == null && pTree.DirDatCount == 0) // Directory above DAT's in Tree
                {
                    thistxt = pTree.Name;
                }
                else if (pTree.Dat == null && pTree.DirDatCount == 1) // Directory that contains DAT's
                {
                    thistxt = pTree.Name + ": " + pTree.DirDat(0).GetData(RvDat.DatData.Description);
                }
                else if (pTree.Dat == null && pTree.DirDatCount > 1) // Directory above DAT's in Tree
                {
                    thistxt = pTree.Name;
                    if (pTree.Tree.TreeExpanded)
                    {
                        datList = new List<string>();
                        for (int i = 0; i < pTree.DirDatCount; i++)
                        {
                            if (!pTree.DirDat(i).Flag(DatFlags.AutoAddedDirectory))
                            {
                                string title = pTree.DirDat(i).GetData(RvDat.DatData.Description);
                                if (string.IsNullOrWhiteSpace(title))
                                    title = pTree.DirDat(i).GetData(RvDat.DatData.DatName);
                                datList.Add(title);
                            }
                        }
                    }
                }
                // pTree.Parent.DirDatCount>1: This should probably be a test like parent contains Dat
                else if (pTree.Dat != null && pTree.Dat.Flag(DatFlags.AutoAddedDirectory) && pTree.Parent.DirDatCount > 1)
                {
                    thistxt = pTree.Name + ": ";
                }
                else if (pTree.Dat != null && pTree.DirDatCount == 0) // Directories made by a DAT
                {
                    thistxt = pTree.Name;
                }
                else
                {
                    ReportError.SendAndShow("Unknown Tree settings in DisplayTree.");
                    thistxt = "";
                }

                if (pTree.IsInToSort)
                {
                    subtxt = "";
                }
                if (pTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortPrimary | RvFile.ToSortDirType.ToSortCache))
                {
                    thistxt += " (Primary)";
                }
                else if (pTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortPrimary))
                {
                    thistxt += " (Primary)";
                }
                else if (pTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortCache))
                {
                    thistxt += " (Cache)";
                }
                else if (pTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortFileOnly))
                {
                    thistxt += " (File Only)";
                }

                IBrush textBrush;
                if (Selected != null && pTree.TreeFullName == Selected.TreeFullName)
                {
                    context.DrawRectangle(
                        new ImmutableSolidColorBrush(Color.FromArgb(255, 51, 153, 255)),
                        null,
                        recBackGround);
                    textBrush = new ImmutableSolidColorBrush(Colors.White);
                }
                else
                {
                    if (this.TryFindResource("SystemBaseHighColor",
                            this.ActualThemeVariant, out var fgRes) && fgRes is Color fgColor)
                    {
                        textBrush = new ImmutableSolidColorBrush(fgColor);
                    }
                    else
                    {
                        textBrush = Application.Current?.ActualThemeVariant == ThemeVariant.Dark
                            ? new ImmutableSolidColorBrush(Color.Parse("#FFFFFF"))
                            : Brushes.Black;
                    }
                }

                thistxt += " " + subtxt;

                var ft = new FormattedText(
                    thistxt,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    _tTypeface,
                    _tFontSize,
                    textBrush);
                context.DrawText(ft, new Point(uTree.RText.Left, uTree.RText.Top + 1));

                if (datList != null)
                {
                    for (int i = 0; i < datList.Count; i++)
                    {
                        var ft1 = new FormattedText(
                            datList[i],
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            _tTypeface,
                            _tFontSize1,
                            textBrush);
                        context.DrawText(ft1,
                            new Point(
                                ((UiTree)pTree.Tree.UiObject).RText.Left + 20,
                                ((UiTree)pTree.Tree.UiObject).RText.Top + 14 + i * 12));
                    }
                }
            }

            if (!pTree.Tree.TreeExpanded)
                return;

            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvFile tDir = pTree.Child(i);
                if (tDir.IsDirectory && tDir.Tree?.UiObject != null)
                {
                    PaintTree(tDir, context, clipRect);
                }
            }
        }

        #endregion

        #region "Mouse Events"

        private bool _mousehit;

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            Point pos = e.GetPosition(this);
            double x = pos.X;
            double y = pos.Y;

            _mousehit = false;

            if (_lTree != null)
            {
                for (int i = 0; i < _lTree.ChildCount; i++)
                {
                    RvFile tDir = _lTree.Child(i);
                    if (tDir.Tree == null)
                        continue;
                    if (CheckMouseUp(tDir, x, y, e))
                        break;
                }
            }

            if (!_mousehit)
            {
                return;
            }

            SetupInt();
        }

        public void SetSelected(RvFile selected)
        {
            bool found = false;

            RvFile t = selected;
            while (t != null)
            {
                if (t.Tree != null)
                {
                    if (!found)
                    {
                        Selected = t;
                        found = true;
                    }
                    else
                    {
                        t.Tree.SetTreeExpanded(true, Working);
                    }
                }
                t = t.Parent;
            }
            SetupInt();
        }

        private bool CheckMouseUp(RvFile pTree, double x, double y, PointerReleasedEventArgs e)
        {
            RvTreeRow treeRow = pTree?.Tree;
            if (treeRow == null)
                return false;

            UiTree uTree = treeRow.UiObject as UiTree;
            if (uTree == null)
            {
                // If the UI tree mapping is stale, rebuild once and retry.
                SetupInt();
                treeRow = pTree.Tree;
                uTree = treeRow?.UiObject as UiTree;
                if (uTree == null)
                    return false;
            }

            Point pt = new Point(x, y);

            if (!Working && uTree.RChecked.Contains(pt))
            {
                RvChecked?.Invoke(pTree, new RvTreeEventArgs(pTree, e));

                bool shiftPressed = (e.KeyModifiers & KeyModifiers.Shift) != 0;

                if (e.InitialPressMouseButton == MouseButton.Right)
                {
                    _mousehit = true;
                    if (pTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortPrimary) || pTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortCache))
                        return true;

                    SetChecked(pTree, RvTreeRow.TreeSelect.Locked, Working, shiftPressed);
                    return true;
                }

                _mousehit = true;
                SetChecked(pTree, treeRow.Checked == RvTreeRow.TreeSelect.Selected ? RvTreeRow.TreeSelect.UnSelected : RvTreeRow.TreeSelect.Selected, Working, shiftPressed);
                return true;
            }

            if (uTree.RExpand.Contains(pt))
            {
                _mousehit = true;
                SetExpanded(pTree, e.InitialPressMouseButton == MouseButton.Right, Working);
                return true;
            }

            if (uTree.RText.Contains(pt))
            {
                _mousehit = true;

                RvSelected?.Invoke(pTree, new RvTreeEventArgs(pTree, e));

                Selected = pTree;
                return true;
            }

            if (!treeRow.TreeExpanded)
                return false;

            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvFile rDir = pTree.Child(i);
                if (!rDir.IsDirectory || rDir.Tree == null)
                    continue;

                if (CheckMouseUp(rDir, x, y, e))
                {
                    return true;
                }
            }

            return false;
        }

        private static void SetChecked(RvFile pTree, RvTreeRow.TreeSelect nSelection, bool isWorking, bool shiftPressed)
        {
            if (!isWorking) RvTreeRow.OpenStream();
            SetCheckedRecurse(pTree, nSelection, isWorking, shiftPressed);
            if (!isWorking) RvTreeRow.CloseStream();
        }

        private static void SetCheckedRecurse(RvFile pTree, RvTreeRow.TreeSelect nSelection, bool isworking, bool shiftPressed)
        {
            pTree.Tree.SetChecked(nSelection, isworking);
            if (shiftPressed)
                return;
            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvFile d = pTree.Child(i);
                if (d.IsDirectory && d.Tree != null)
                {
                    SetCheckedRecurse(d, nSelection, isworking, false);
                }
            }
        }

        private static void SetExpanded(RvFile pTree, bool rightClick, bool isWorking)
        {
            if (!rightClick)
            {
                pTree.Tree.SetTreeExpanded(!pTree.Tree.TreeExpanded, isWorking);
                return;
            }
            if (!isWorking) RvTreeRow.OpenStream();
            // Find the value of the first child node.
            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvFile d = pTree.Child(i);
                if (!d.IsDirectory || d.Tree == null)
                    continue;

                // Recursively Set All Child Nodes to this value
                SetExpandedRecurse(pTree, !d.Tree.TreeExpanded, isWorking);
                break;
            }
            if (!isWorking) RvTreeRow.CloseStream();
        }

        private static void SetExpandedRecurse(RvFile pTree, bool expanded, bool isWorking)
        {
            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvFile d = pTree.Child(i);
                if (!d.IsDirectory || d.Tree == null)
                    continue;

                d.Tree.SetTreeExpanded(expanded, isWorking);
                SetExpandedRecurse(d, expanded, isWorking);
            }
        }

        #endregion
    }

    /// <summary>
    /// Event args for RvTree selection and check events, carrying the RvFile node and pointer info.
    /// </summary>
    public class RvTreeEventArgs : EventArgs
    {
        public RvFile RvFile { get; }
        public PointerReleasedEventArgs PointerArgs { get; }

        public RvTreeEventArgs(RvFile rvFile, PointerReleasedEventArgs pointerArgs)
        {
            RvFile = rvFile;
            PointerArgs = pointerArgs;
        }
    }

    /// <summary>
    /// Extension to check if Rect is effectively empty (zero size).
    /// </summary>
    internal static class RectExtensions
    {
        public static bool IsEmpty(this Rect r)
        {
            return r.Width == 0 && r.Height == 0;
        }
    }
}

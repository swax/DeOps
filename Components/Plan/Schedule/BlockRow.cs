using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Components.Link;
using DeOps.Components.Plan;
using DeOps.Interface.TLVex;


namespace DeOps.Components.Plan
{
    internal partial class BlockRow : UserControl
    {
        PlanNode Node;
        ScheduleView View;

        ulong DhtID;

        Bitmap DisplayBuffer;
        bool Redraw;

        List<BlockArea> BlockAreas = new List<BlockArea>();

        Pen RefPen = new Pen(Color.PowderBlue);
        Pen BigPen = new Pen(Color.FromArgb(224,224,224));
        Pen SmallPen = new Pen(Color.FromArgb(248, 248, 248));
        Pen SelectPen = new Pen(Color.Black, 2);
        SolidBrush Highlight = new SolidBrush(SystemColors.Highlight);
        Pen DashPen = new Pen(Color.Black);
        Font Tahoma = new Font("Tahoma", 8);

        SolidBrush HighMask = new SolidBrush(Color.FromArgb(25, Color.Red));
        SolidBrush LowMask = new SolidBrush(Color.FromArgb(25, Color.Blue));
        SolidBrush NeutralMask = new SolidBrush(Color.FromArgb(25, Color.Black));

        DateTime StartTime;
        DateTime EndTime;
        long TicksperPixel;

        List<ulong> Uplinks = new List<ulong>();


        internal BlockRow()
        {
            InitializeComponent();
            DashPen.DashStyle = DashStyle.Dot;
        }

        internal BlockRow(PlanNode node)
        {
            InitializeComponent();
            
            Node = node;
            View = node.View;
            DhtID = node.Link.DhtID;

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            DashPen.DashStyle = DashStyle.Dot;
        }

        Font debugFont = new Font("Tahoma", 8);
        SolidBrush blackBrush = new SolidBrush(Color.Black);
       
        private void BlockRow_Paint(object sender, PaintEventArgs e)
        {
            if(DisplayBuffer == null )
                DisplayBuffer = new Bitmap(Width, Height);

            if (!Redraw)
            {
                e.Graphics.DrawImage(DisplayBuffer, 0, 0);
                return;
            }
            Redraw = false;

            // background
            Graphics buffer = Graphics.FromImage(DisplayBuffer);

            buffer.Clear(Color.White);
            //buffer.SmoothingMode = SmoothingMode.AntiAlias;


            // draw tick lines
            foreach (int mark in View.ScheduleSlider.SmallMarks)
               buffer.DrawLine(SmallPen, mark, 0, mark, Height);

            foreach (int mark in View.ScheduleSlider.BigMarks)
               buffer.DrawLine(BigPen, mark, 0, mark, Height);

           //buffer.DrawLine(RefPen, View.ScheduleSlider.RefMark, 0, View.ScheduleSlider.RefMark, Height);

            // setup vars
            Rectangle tempRect = new Rectangle();
            tempRect.Y = 0;
            tempRect.Height = Height;

            StartTime = View.StartTime.ToUniversalTime();
            EndTime   = View.EndTime.ToUniversalTime();
            TicksperPixel = View.ScheduleSlider.TicksperPixel;


            // draw higher plans    
            BlockAreas.Clear();
            
            List<PlanNode> upnodes = new List<PlanNode>();
            PlanNode nextNode = Node;

            while (nextNode.Parent.GetType() == typeof(PlanNode))
            {
                nextNode = (PlanNode) nextNode.Parent;
                Uplinks.Add(nextNode.Link.DhtID);
                upnodes.Add(nextNode);
            }

            upnodes.Reverse();
            int level = 0;
            
            foreach (PlanNode upnode in upnodes)
            {
                foreach (PlanBlock block in GetBlocks(upnode.Link.DhtID))
                    if (BlockinRange(block, ref tempRect))
                    {
                        buffer.FillRectangle(GetMask(upnode.Link.DhtID), tempRect);
                        BlockAreas.Add(new BlockArea(tempRect, block, level, false));
                    }

                    level++;
            }

            // draw local plans
            List<List<DrawBlock>> layers = new List<List<DrawBlock>>();

            // arrange visible blocks
            foreach (PlanBlock block in GetBlocks(DhtID))
                if(BlockinRange(block, ref tempRect))
                    AddDrawBlock(new DrawBlock(block, tempRect), layers);

            // draw blocks
            if (layers.Count > 0)
            {
                int y = 2;
                int yStep = (Height-2) / layers.Count;

                foreach (List<DrawBlock> list in layers)
                {
                    foreach (DrawBlock item in list)
                        if(item.Above == null)
                        {
                            item.Rect.Y = y;
                            item.Rect.Height = yStep - 1;

                            DrawBlock down = item.Below;
                            while (down != null)
                            {
                                item.Rect.Height += yStep;
                                down = down.Below;
                            }

                            BlockAreas.Add(new BlockArea(item.Rect, item.Block, level, true));

                            Rectangle fill = item.Rect;
                            fill.Height = Height - y;
                            buffer.FillRectangle(GetMask(DhtID), fill);

                            if (item.Block == View.SelectedBlock)
                                buffer.DrawRectangle(SelectPen, item.Rect);

                            SizeF size = buffer.MeasureString(item.Block.Title, Tahoma);

                            if (size.Width < item.Rect.Width - 2 && size.Height < item.Rect.Height - 2)
                                buffer.DrawString(item.Block.Title, Tahoma, blackBrush,
                                    item.Rect.X + (item.Rect.Width - size.Width) / 2,
                                    item.Rect.Y + (item.Rect.Height - size.Height) / 2);

                        }

                    y += yStep;
                }
            }


            // draw selection
            if (Node.Selected)
            {
                if (View.PlanStructure.Focused)
                {
                    buffer.FillRectangle(Highlight, 0, 0, Width, 2);
                    buffer.FillRectangle(Highlight, 0, Height - 2, Width, 2);
                }

                else
                {
                    buffer.DrawLine(DashPen, 1, 0, Width-1, 0);
                    buffer.DrawLine(DashPen, 0, Height-1, Width, Height-1);
                }
            }

            // Copy buffer to display
            e.Graphics.DrawImage(DisplayBuffer, 0, 0);

        }

        private SolidBrush GetMask(ulong key)
        {

            // if key above target - red
            if (View.Uplinks.Contains(key))
                return HighMask;

            // if target is equal to or above key - blue
            if(key == View.DhtID || Uplinks.Contains(View.DhtID))
                return LowMask;

            // else black
            return NeutralMask;
        }

        private List<PlanBlock> GetBlocks(ulong key)
        {
            if (View.Plans.PlanMap.ContainsKey(key) &&
                View.Plans.PlanMap[key].Loaded &&
                View.Plans.PlanMap[key].Blocks != null && 
                View.Plans.PlanMap[key].Blocks.ContainsKey(View.ProjectID))

                return View.Plans.PlanMap[key].Blocks[View.ProjectID];


            return new List<PlanBlock>();
        }

        private bool BlockinRange(PlanBlock block, ref Rectangle rect)
        {
            bool add = false;

            // start is in span
            if (View.StartTime < block.StartTime && block.StartTime < EndTime)
            {
                rect.X = (int)((block.StartTime.Ticks - StartTime.Ticks) / TicksperPixel);
                rect.Width = Width - rect.X;
                
                add = true;
            }

            // end is in span
            if (StartTime < block.EndTime && block.EndTime < EndTime)
            {
                long startTicks = StartTime.Ticks;

                if (!add)
                    rect.X = 0; // if rect set above, dont reset left
                else
                    startTicks = block.StartTime.Ticks;

                rect.Width = (int)((block.EndTime.Ticks - startTicks) / TicksperPixel);

                add = true;
            }

            // if block spans entire row
            if (block.StartTime < StartTime && EndTime < block.EndTime)
            {
                rect.X = 0;
                rect.Width = Width;
                
                add = true;
            }

            return add;
        }

        private void AddDrawBlock(DrawBlock draw, List<List<DrawBlock>> layers)
        {
            bool conflict = false;

            List<DrawBlock> delete = new List<DrawBlock>();

            foreach (List<DrawBlock> layer in layers)
            {
                conflict = false;
                delete.Clear();

                foreach (DrawBlock item in layer)
                    if (item.Rect.IntersectsWith(draw.Rect))
                    {
                        if (draw.Above != null) // signals block was already drawn, but this layer is full
                            return;

                        if (item.Above == null) // this is the main block for an item, cant draw here, create new layer
                        {
                            conflict = true;
                            break;
                        }
                        else // this item isnt the main so it can be deleted if there is a conflict
                            delete.Add(item);
                        
                    }

                if (!conflict)
                {
                    foreach (DrawBlock item in delete)
                        item.Remove();

                    if (draw.Above != null)
                        draw.Above.Below = draw;

                    draw.Layer = layer;
                    layer.Add(draw);

                    // draw same rect another layer down if there is room
                    DrawBlock next = new DrawBlock(draw.Block, draw.Rect);
                    next.Above = draw;
                    draw = next;
                }
            }

            if (draw.Above != null) // signals block already is drawn in a layer
                return;

            // new layer
            List<DrawBlock> newLayer = new List<DrawBlock>();
            draw.Layer = newLayer;
            newLayer.Add(draw);

            // duplicate previous layer's blocks to this list
            if (layers.Count > 0)
            {
                foreach (DrawBlock topBlock in layers[layers.Count - 1])
                    if (!topBlock.Rect.IntersectsWith(draw.Rect))
                        newLayer.Add(new DrawBlock(newLayer, topBlock));
            }

            layers.Add(newLayer);
        }

        private void BlockRow_Resize(object sender, EventArgs e)
        {
            if (Width > 0 && Height > 0)
            {
                DisplayBuffer = new Bitmap(Width, Height);

                UpdateRow(false);
            }
        }

        internal void UpdateRow(bool immediate)
        {
            if (View.Plans.PlanMap.ContainsKey(DhtID) && !View.Plans.PlanMap[DhtID].Loaded)
                View.Plans.LoadPlan(DhtID);

            Redraw = true;
            Invalidate();

            if (immediate)
                Update();
        }

        private void BlockRow_VisibleChanged(object sender, EventArgs e)
        {
            UpdateRow(false);
        }

        private void BlockRow_MouseClick(object sender, MouseEventArgs e)
        {
            foreach (BlockArea area in BlockAreas)
                if (area.Local && area.Rect.Contains(e.Location))
                {
                    if (View.SelectedBlock != area.Block)
                    {
                        View.SelectedBlock = area.Block;
                        View.RefreshRows();
                    }

                    break;
                }


            if (e.Button != MouseButtons.Right)
                return;

            // create details menu

            // if local create edit/delete menu


            ContextMenu menu = new ContextMenu();
            MenuItem details = new MenuItem("Details");

            string indent = "";
            int lastLevel = -1;

            foreach (BlockArea area in BlockAreas)
                if (area.Rect.Contains(e.Location))
                {
                    if (lastLevel == -1)
                        lastLevel = area.Level;

                    if (area.Level > lastLevel)
                    {
                        lastLevel = area.Level;
                        indent += "    ";
                    }

                    details.MenuItems.Add(new BlockMenuItem(indent + area.Block.Title, area.Block, new EventHandler(RClickView)));

                    if (area.Local)
                    {
                        if (DhtID != View.Core.LocalDhtID)
                            menu.MenuItems.Add(new BlockMenuItem("View", area.Block, new EventHandler(RClickView)));
                        else
                        {
                            menu.MenuItems.Add(new BlockMenuItem("Edit", area.Block, new EventHandler(RClickEdit)));
                            menu.MenuItems.Add(new BlockMenuItem("Delete", area.Block, new EventHandler(RClickDelete)));
                        }
                    }
                }

            if (details.MenuItems.Count > 0)
                menu.MenuItems.Add(details);

            if(menu.MenuItems.Count > 0)
                menu.Show(this, e.Location);
        }

        private void RClickView(object sender, EventArgs e)
        {
            BlockMenuItem menu = sender as BlockMenuItem;

            if (menu == null)
                return;

            EditBlock form = new EditBlock(BlockViewMode.Show, View, menu.Block);
            form.Show(View);
        }

        private void RClickEdit(object sender, EventArgs e)
        {
            BlockMenuItem menu = sender as BlockMenuItem;

            if (menu == null)
                return;

            EditBlock form = new EditBlock(BlockViewMode.Edit, View, menu.Block);
            form.Show(View);
        }

        private void RClickDelete(object sender, EventArgs e)
        {
            BlockMenuItem menu = sender as BlockMenuItem;

            if (menu == null)
                return;

            if (View.Plans.LocalPlan.Blocks.ContainsKey(View.ProjectID))
                View.Plans.LocalPlan.Blocks[View.ProjectID].Remove(menu.Block);

            View.ChangesMade();
        }

        private void BlockRow_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            foreach (BlockArea area in BlockAreas)
                if (area.Local && area.Rect.Contains(e.Location))
                {
                    BlockViewMode mode = (DhtID == View.Core.LocalDhtID) ? BlockViewMode.Edit : BlockViewMode.Show;

                    EditBlock form = new EditBlock(mode, View, area.Block);
                    form.Show(View);

                    break;
                }
        }

        private void BlockRow_MouseMove(object sender, MouseEventArgs e)
        {
            View.CursorUpdate(this);
        }

        internal string GetHoverText()
        {
            Point pos = PointToClient(Cursor.Position);

            bool good = false;
            StringBuilder text = new StringBuilder(100);

            text.Append(Node.Link.Name);
            text.Append(" - ");

            DateTime start = View.StartTime;
            DateTime time = new DateTime(start.Ticks + View.ScheduleSlider.TicksperPixel * pos.X);
            text.Append(time.ToString("D"));

            text.Append("\n\n");

            string indent = "";
            int lastLevel = -1;

            foreach (BlockArea area in BlockAreas)
                if (area.Rect.X <= pos.X && pos.X <= area.Rect.X + area.Rect.Width)
                {
                    good = true;

                    if(lastLevel == -1)
                        lastLevel = area.Level;

                    if (area.Level > lastLevel)
                    {
                        lastLevel = area.Level;
                        indent += "    ";
                    }

                    text.Append(indent);
                    text.Append(area.Block.Title);

                    RichTextBox rich = new RichTextBox();
                    rich.Rtf = area.Block.Description;

                    if (rich.Text != "")
                    {
                        text.Append(" - ");

                        rich.Text = rich.Text.Replace('\n', ' ');

                        if (rich.Text.Length > 25)
                            text.Append(rich.Text.Substring(0, 25) + "...");
                        else
                            text.Append(rich.Text);
                    }

                    text.Append("\n");
                }

            

            return good ? text.ToString() : "";
        }
    }

    internal class BlockMenuItem : MenuItem
    {
        internal PlanBlock Block;

        internal BlockMenuItem(string text, PlanBlock block, EventHandler onClick)
            :
            base(text, onClick)
        {
            Block = block;
        }
    }


    internal class DrawBlock
    {
        internal List<DrawBlock> Layer;
        internal PlanBlock Block;
        internal Rectangle Rect;

        internal DrawBlock Above;
        internal DrawBlock Below;

        internal DrawBlock(PlanBlock block, Rectangle rect)
        {
            Block = block;
            Rect = rect;
        }

        internal DrawBlock(List<DrawBlock> layer, DrawBlock top)
        {
            Layer = layer;
            Block = top.Block;
            Rect = top.Rect;

            Above = top;
            top.Below = this;
        }

        internal void Remove()
        {
            Layer.Remove(this);

            if (Below != null)
                Below.Remove();
            
            Above.Below = null;
            Above = null;
        }
    }

    internal class BlockArea
    {
        internal Rectangle Rect;
        internal PlanBlock Block;
        internal int       Level;
        internal bool      Local;


        internal BlockArea(Rectangle rect, PlanBlock block, int level, bool local)
        {
            Rect  = rect;
            Block = block;
            Level = level;
            Local = local;
        }
    }
}

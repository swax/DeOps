using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Microsoft.Win32;
using System.Collections;
using DeOps.Interface.Views;
using DeOps.Interface.TLVex;
using System.IO;
using DeOps.Interface;
using DeOps.Implementation;
using System.Drawing;


namespace DeOps
{
    public static class GuiUtils
    {
        public static bool IsRunningOnMono()
        {
            return (Type.GetType("Mono.Runtime") != null);//&& Properties.Settings.Default.MonoHelp);
        }

        public static void SetupToolstrip(ToolStrip strip, OpusColorTable colorTable)
        {
            strip.Renderer = new ToolStripProfessionalRenderer(colorTable);

            if (IsRunningOnMono() && strip.Items != null)
                foreach (ToolStripItem item in strip.Items)
                {
                    //item.Image = null;
                    //item.DisplayStyle = ToolStripItemDisplayStyle.Text;
                }
        }

        public static void FixMonoDropDownOpening(ToolStripDropDownButton button, EventHandler action)
        {
            if (IsRunningOnMono())
                button.MouseEnter += (s, e) => action.Invoke(s, null);
        }

        public static void SortedAdd(this ToolStripItemCollection strip, ToolStripMenuItem item)
        {
            int i = 0;

            for (i = 0; i < strip.Count; i++)
                if (string.Compare(strip[i].Text, item.Text) > 0)
                {
                    strip.Insert(i, item);
                    return;
                }

            strip.Insert(i, item);
        }

        public static void InsertSubNode(TreeListNode parent, TreeListNode node)
        {
            int index = 0;

            foreach (TreeListNode entry in parent.Nodes)
                if (string.Compare(node.Text, entry.Text, true) < 0)
                {
                    parent.Nodes.Insert(index, node);
                    return;
                }
                else
                    index++;

            parent.Nodes.Insert(index, node);
        }

        public static string GetQuip(string body, TextFormat format)
        {
            string quip = body;

            // rtf to short text quip
            if (format == TextFormat.RTF)
            {
                RichTextBox box = new RichTextBox();
                box.Rtf = body;
                quip = box.Text;
            }

            quip = quip.Replace('\r', ' ');
            quip = quip.Replace('\n', ' ');

            if (quip.Length > 50)
                quip = quip.Substring(0, 50) + "...";

            return quip;
        }

        public static bool VerifyPassphrase(OpCore core, ThreatLevel threat)
        {
            //crit revise
            if (threat != ThreatLevel.High)
                return true;

            bool trying = true;

            while (trying)
            {
                GetTextDialog form = new GetTextDialog(core, core.User.GetTitle(), "Enter Passphrase", "");

                form.StartPosition = FormStartPosition.CenterScreen;
                form.ResultBox.UseSystemPasswordChar = true;

                if (form.ShowDialog() != DialogResult.OK)
                    return false;

                byte[] key = Utilities.GetPasswordKey(form.ResultBox.Text, core.User.PasswordSalt);

                if (Utilities.MemCompare(core.User.PasswordKey, key))
                    return true;

                MessageBox.Show("Wrong passphrase", "DeOps");
            }

            return false;
        }

        public static int GetDistance(Point start, Point end)
        {
            int x = end.X - start.X;
            int y = end.Y - start.Y;

            return (int)Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
        }

        public static string RtftoColor(string rtf, Color color)
        {
            rtf = rtf.Replace("red0", "red" + color.R);
            rtf = rtf.Replace("blue0", "blue" + color.B);
            rtf = rtf.Replace("green0", "green" + color.G);

            return rtf;
        }

        static public SizeF MeasureDisplayString(Graphics graphics, string text, System.Drawing.Font font)
        {
            const int width = 32;

            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width, 1, graphics);
            System.Drawing.SizeF size = graphics.MeasureString(text, font);
            System.Drawing.Graphics anagra = System.Drawing.Graphics.FromImage(bitmap);

            int measured_width = (int)size.Width;

            if (anagra != null)
            {
                anagra.Clear(System.Drawing.Color.White);
                anagra.DrawString(text + "|", font, System.Drawing.Brushes.Black, width - measured_width, -font.Height / 2);

                for (int i = width - 1; i >= 0; i--)
                {
                    measured_width--;
                    if (bitmap.GetPixel(i, 0).R == 0)
                    {
                        break;
                    }
                }
            }

            return new System.Drawing.SizeF(measured_width, size.Height);
        }

        static public int MeasureDisplayStringWidth(Graphics graphics, string text, System.Drawing.Font font)
        {
            return (int)MeasureDisplayString(graphics, text, font).Width;
        }

    }

    public class ListViewColumnSorter : IComparer
    {
        public int ColumnToSort;
        public SortOrder OrderOfSort;
        public CaseInsensitiveComparer ObjectCompare;

        public ListViewColumnSorter()
        {
            ColumnToSort = 0;
            OrderOfSort = SortOrder.None;
            ObjectCompare = new CaseInsensitiveComparer();
        }

        public int Compare(object x, object y)
        {
            int compareResult;
            ListViewItem listviewX, listviewY;

            // Cast the objects to be compared to ListViewItem objects
            listviewX = (ListViewItem)x;
            listviewY = (ListViewItem)y;

            // Compare the two items
            compareResult = ObjectCompare.Compare(listviewX.SubItems[ColumnToSort].Text, listviewY.SubItems[ColumnToSort].Text);

            // Calculate correct return value based on object comparison
            if (OrderOfSort == SortOrder.Ascending)
                return compareResult;
            else if (OrderOfSort == SortOrder.Descending)
                return (-compareResult);
            else
                return 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using RiseOp.Interface.Views;


namespace RiseOp.Interface
{
    public partial class TextInput : UserControl
    {
        // show font strip
        private bool ShowStrip = true;

        public bool ShowFontStrip
        {
            get
            {
                return ShowStrip;
            }
            set
            {
                ShowStrip = value;

                if (ShowStrip)
                    FontToolStrip.Show();
                else
                    FontToolStrip.Hide();
            }
        }

        // read only
        public bool ReadOnly
        {
            get
            {
                return InputBox.ReadOnly;
            }
            set
            {
                InputBox.ReadOnly = value;

                if (value) // dont auto turn on when input is not read-only
                    ShowFontStrip = false;
            }
        }

        // enter clears
        private bool _EnterClears = true;

        public bool EnterClears
        {
            get
            {
                return _EnterClears;
            }
            set
            {
                _EnterClears = value;
            }
        }

        // accept tabs
        public bool AcceptTabs
        {
            get
            {
                return InputBox.AcceptsTab;
            }
            set
            {
                InputBox.AcceptsTab = value;
            }
        }

        internal delegate void SendMessageHandler(string message);
        internal SendMessageHandler SendMessage;

        public TextInput()
        {
            InitializeComponent();

            FontToolStrip.Renderer = new ToolStripProfessionalRenderer(new OpusColorTable());
        }


        private void SmallerButton_Click(object sender, EventArgs e)
        {
            Font current = InputBox.SelectionFont;

            InputBox.SelectionFont = new Font(current.FontFamily, current.Size - 2, current.Style);
        }

        private void NormalButton_Click(object sender, EventArgs e)
        {
            Font current = InputBox.SelectionFont;

            InputBox.SelectionFont = new Font(current.FontFamily, 10, current.Style);
        }

        private void LargerButton_Click(object sender, EventArgs e)
        {
            Font current = InputBox.SelectionFont;

            InputBox.SelectionFont = new Font(current.FontFamily, current.Size + 2, current.Style); 
        }

        private void BoldButton_Click(object sender, EventArgs e)
        {
            ApplyStyle(FontStyle.Bold, BoldButton.Checked);
        }

        private void ItalicsButton_Click(object sender, EventArgs e)
        {
            ApplyStyle(FontStyle.Italic, ItalicsButton.Checked);
        }

        private void UnderlineButton_Click(object sender, EventArgs e)
        {
            ApplyStyle(FontStyle.Underline, UnderlineButton.Checked);
        }

        void ApplyStyle(FontStyle style, bool enabled)
        {
            FontStyle newStyle = InputBox.SelectionFont.Style;

            if (enabled)
                newStyle |= style;
            else
                newStyle &= ~style;

            InputBox.SelectionFont = new Font(InputBox.SelectionFont, newStyle);
        }

        private void ColorsButton_Click(object sender, EventArgs e)
        {
            ColorDialog dialog = new ColorDialog();

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            InputBox.SelectionColor = dialog.Color;

            InputBox.Focus();

            /* RClick font selection
             FontDialog dialog = new FontDialog();

			dialog.Font = TextBox.SelectionFont;

			if(dialog.ShowDialog(this) != DialogResult.OK)
				return;

			TextBox.SelectionFont = dialog.Font;

			TextBox.Focus();
             */
        }


        private void InputBox_SelectionChanged(object sender, EventArgs e)
        {
            if (InputBox.SelectionFont == null)
                return;

            BoldButton.Checked = InputBox.SelectionFont.Bold;
            ItalicsButton.Checked = InputBox.SelectionFont.Italic;
            UnderlineButton.Checked = InputBox.SelectionFont.Underline;
        }

        
        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return && _EnterClears && !e.Shift)
            {
                string message = InputBox.Rtf;

                Regex rex = new Regex("\\\\par\\s+", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
                Match m = rex.Match(message);

                while (m.Success)
                {
                    message = rex.Replace(message, "");
                    m = m.NextMatch();
                }

                BeginInvoke(SendMessage, message);
                //Panel.SendMessage();

                Font savedFont = InputBox.SelectionFont;
                Color savedColor = InputBox.SelectionColor;

                InputBox.Clear();

                InputBox.SelectionFont = savedFont;
                InputBox.SelectionColor = savedColor;

                e.Handled = true;
            }
        }

        private void InputBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using DeOps.Interface.Views;


namespace DeOps.Interface
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

        // Default Plain Text
        bool PlainText;

        public bool PlainTextMode
        {
            get
            {
                return PlainText;
            }
            set
            {
                PlainText = value;

                FontSeparator.Visible = !value;

                BoldButton.Visible = !value;
                ItalicsButton.Visible = !value;
                UnderlineButton.Visible = !value;

                FontButton.Visible = !value;
                ColorsButton.Visible = !value;

                TextFormat = value ? TextFormat.Plain : TextFormat.RTF;
            }
        }

        // IM Buttons
        bool _IMButtons;

        public bool IMButtons
        {
            get
            {
                return _IMButtons;
            }
            set
            {
                _IMButtons = value;
                SendFileButton.Visible = value;
                BlockButton.Visible = value;
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

        internal TextFormat TextFormat = TextFormat.RTF;

        internal delegate void SendMessageHandler(string message, TextFormat format);
        internal SendMessageHandler SendMessage;

        internal MethodInvoker SendFile;
        internal MethodInvoker IgnoreUser;
        internal MethodInvoker AddBuddy;


        public TextInput()
        {
            InitializeComponent();

            GuiUtils.SetupToolstrip(FontToolStrip, new OpusColorTable());


            // need to init, so when we get the rtf there is color encoding info in it we can re-assign if need be
            InputBox.SelectionColor = Color.Black;
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

            InputBox.Select();
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
                if (InputBox.Text.Replace("\n", "") != "")
                {
                    string message = InputBox.Text;

                    if (TextFormat == TextFormat.RTF)
                    {
                        message = InputBox.Rtf;

                        Regex rex = new Regex("\\\\par\\s+", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
                        Match m = rex.Match(message);

                        while (m.Success)
                        {
                            message = rex.Replace(message, "");
                            m = m.NextMatch();
                        }
                    }

                    BeginInvoke(SendMessage, message, TextFormat);
                    //Panel.SendMessage();
                }

                Font savedFont = InputBox.SelectionFont;
                Color savedColor = InputBox.SelectionColor;

                InputBox.Clear();

                InputBox.SelectionFont = savedFont;
                InputBox.SelectionColor = savedColor;

                e.Handled = true;
            }
        }

        private void FontButton_Click(object sender, EventArgs e)
        {
            FontDialog dialog = new FontDialog();

            dialog.Font = InputBox.SelectionFont;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            InputBox.SelectionFont = dialog.Font;

            InputBox.Select();
        }

        private void BlockButton_Click(object sender, EventArgs e)
        {
            BeginInvoke(IgnoreUser);
        }

        private void SendFileButton_Click(object sender, EventArgs e)
        {
            BeginInvoke(SendFile);
        }
        
        private void PlainTextButton_Click(object sender, EventArgs e)
        {
            // clear formatting
            string text = InputBox.Text;
            InputBox.Clear();
            InputBox.Text = text;

            PlainTextMode = true;
        }

        private void RichTextButton_Click_1(object sender, EventArgs e)
        {
            PlainTextMode = false;

        }

        private void AddBuddyButton_Click(object sender, EventArgs e)
        {
            BeginInvoke(AddBuddy);
        }
    }
}

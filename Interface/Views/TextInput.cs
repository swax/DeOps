using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

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


        internal delegate void SendMessageHandler(string message);
        internal SendMessageHandler SendMessage;

        public TextInput()
        {
            InitializeComponent();
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
    }
}

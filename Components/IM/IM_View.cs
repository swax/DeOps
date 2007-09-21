using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Interface;
using DeOps.Components.Link;
using DeOps.Components.Location;


namespace DeOps.Components.IM
{

    internal partial class IM_View : ViewShell
    {        
        IMControl       IM;
        LinkControl     Links;
        LocationControl Locations;
        internal ulong  DhtID;
        string          NodeName = "";

        //bool ShowTimestamps;

        Font BoldFont    = new Font("Tahoma", 10, FontStyle.Bold);
        Font RegularFont = new Font("Tahoma", 10, FontStyle.Regular);
        Font TimeFont    = new Font("Tahoma", 8, FontStyle.Bold);


        internal IM_View(IMControl im, ulong key)
        {
            InitializeComponent();

            IM    = im;
            Links = IM.Core.Links;
            Locations = IM.Core.Locations;
            DhtID = key;

            UpdateName();
            
            // do here so window can be found and multiples not created for the same user
            IM.IM_Update += new IM_UpdateHandler(OnMessageUpdate);           
        }

        private void UpdateName()
        {
            if (Links.LinkMap.ContainsKey(DhtID))
                if (Links.LinkMap[DhtID].Loaded)
                    NodeName = Links.LinkMap[DhtID].Name;
        }

        internal override void Init()
        {
            InputControl.SendMessage += new TextInput.SendMessageHandler(OnSendMessage);

            InputControl.Focus();
            InputControl.InputBox.Focus();

            CheckBackColor();

            DisplayLog();

            Links.LinkUpdate += new LinkUpdateHandler(Links_LinkUpdate);
        }

        internal override bool Fin()
        {
            InputControl.SendMessage -= new TextInput.SendMessageHandler(OnSendMessage);

            IM.IM_Update -= new IM_UpdateHandler(OnMessageUpdate);

            Links.LinkUpdate -= new LinkUpdateHandler(Links_LinkUpdate);

            return true;
        }

        internal override string GetTitle(bool small)
        {
            if (small)
                return "IM";

            return "IM " + NodeName;
        }

        internal override Size GetDefaultSize()
        {
            return new Size(280, 350);
        }

        internal override Icon GetIcon()
        {
            return IMRes.Icon;
        }

        void Links_LinkUpdate(OpLink link)
        {
            UpdateName();
        }

        private void DisplayLog()
        {
            if(IM.MessageLog.ContainsKey(DhtID))
                foreach (InstantMessage message in IM.MessageLog[DhtID])
                    OnMessageUpdate(DhtID, message);
        }

        private void CheckBackColor()
        {
            // check if higher
            OpLink parent = null;

            if (Links.LocalLink.Uplink.ContainsKey(0))
                parent = Links.LocalLink.Uplink[0];

            while (parent != null)
            {
                if (parent.DhtID == DhtID)
                {
                    MessageTextBox.BackColor = Color.FromArgb(255, 250, 250);
                    return;
                }

                parent = parent.Uplink.ContainsKey(0) ? parent.Uplink[0] : null;
            }

            // check if lower
            if(Links.LinkMap.ContainsKey(DhtID))
                if (Links.LocalLink.SearchBranch(0, Links.LinkMap[DhtID]))
                {
                    MessageTextBox.BackColor = Color.FromArgb(250, 250, 255);
                    return;
                }

            // neither, just set white
            MessageTextBox.BackColor = Color.White;
        }

        internal void OnSendMessage(string message)
        {
            IM.SendMessage(DhtID, message);
        }

        internal void OnMessageUpdate(ulong dhtid, InstantMessage message)
        {
            if (dhtid != DhtID)
                return;

            if (message == null)
            {
                CheckBackColor();
                UpdateName();
                return;
            }

            int oldStart  = MessageTextBox.SelectionStart;
            int oldLength = MessageTextBox.SelectionLength;

            MessageTextBox.SelectionStart = MessageTextBox.Text.Length;
            MessageTextBox.SelectionLength = 0;

            // name, in bold, blue for incoming, red for outgoing
            if(message.System)
                MessageTextBox.SelectionColor = Color.Black;
            else if(message.Source == IM.Core.LocalDhtID)
                MessageTextBox.SelectionColor = Color.Red;
            else
                MessageTextBox.SelectionColor = Color.Blue;

            MessageTextBox.SelectionFont = BoldFont;

            string prefix = " ";
            if (!message.System)
            {
                if (message.Source == IM.Core.LocalDhtID)
                    prefix += IM.Core.User.Settings.ScreenName;
                else
                    prefix += NodeName;
            }

            if (MessageTextBox.Text.Length != 0)
                prefix = "\n" + prefix;

            // add timestamp
            /*if (ShowTimestamps)
            {
                MessageTextBox.SelectionFont = TimeFont;
                seperator = " (" + message.TimeStamp.ToString("T") + ")" + seperator;
            }*/

            string location = "";
            if(Locations.ClientCount(DhtID) > 1)
                location  = " @" + Locations.GetLocationName(DhtID, message.ClientID);

            if (!message.System)
                prefix += location + ": ";
            else 
                prefix += "> ";

            MessageTextBox.AppendText(prefix);

            // message, grey for not acked
            MessageTextBox.SelectionColor = Color.Black;
            MessageTextBox.SelectionFont = RegularFont;

            if (!message.System)
                MessageTextBox.SelectedRtf = message.Text;
            else
            {
                MessageTextBox.SelectionFont = BoldFont;
                MessageTextBox.AppendText(message.Text + location);
            }

            MessageTextBox.SelectionStart = oldStart;
            MessageTextBox.SelectionLength = oldLength;


            if (InputControl.Focused)
            {
                MessageTextBox.Focus();
                MessageTextBox.SelectionStart = MessageTextBox.Text.Length;
                MessageTextBox.ScrollToCaret();

                InputControl.Focus();
                InputControl.InputBox.Focus();
            }
        }

        
    }
}

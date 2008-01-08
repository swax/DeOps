using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Implementation.Transport;
using DeOps.Interface;
using DeOps.Services.Link;
using DeOps.Services.Location;


namespace DeOps.Services.IM
{

    internal partial class IM_View : ViewShell
    {        
        IMControl       IM;
        LinkControl     Links;
        OpCore          Core;
        LocationControl Locations;
        internal ulong  DhtID;

        MenuItem TimestampMenu;

        bool WindowActivated;
        bool FlashMe;

        Font BoldFont    = new Font("Tahoma", 10, FontStyle.Bold);
        Font RegularFont = new Font("Tahoma", 10, FontStyle.Regular);
        Font TimeFont = new Font("Tahoma", 8, FontStyle.Regular);
        Font SystemFont = new Font("Tahoma", 8, FontStyle.Bold);

        internal IM_View(IMControl im, ulong key)
        {
            InitializeComponent();

            IM    = im;
            Links = IM.Core.Links;
            Core  = IM.Core;
            Locations = IM.Core.Locations;
            DhtID = key;

            UpdateName();
            
            // do here so window can be found and multiples not created for the same user
            IM.MessageUpdate += new IM_MessageHandler(IM_MessageUpdate);
            IM.StatusUpdate += new IM_StatusHandler(IM_StatusUpdate);

            ContextMenu menu = new ContextMenu();
            TimestampMenu = new MenuItem("Timestamps", new EventHandler(Menu_Timestamps));
            menu.MenuItems.Add(TimestampMenu);
            MessageTextBox.ContextMenu = menu;
        }

        private void UpdateName()
        {
            if(External != null)
                External.Text = "IM " + Links.GetName(DhtID);
        }

        internal override void Init()
        {
            InputControl.SendMessage += new TextInput.SendMessageHandler(InputControl_SendMessage);

            InputControl.Focus();
            InputControl.InputBox.Focus();

            IM_StatusUpdate(DhtID);
            DisplayLog();

            if (External != null)
            {
                External.Activated += new EventHandler(External_Activated);
                External.Deactivate += new EventHandler(External_Deactivate);
            }
        }

        internal override bool Fin()
        {
            InputControl.SendMessage -= new TextInput.SendMessageHandler(InputControl_SendMessage);

            IM.MessageUpdate -= new IM_MessageHandler(IM_MessageUpdate);
            IM.StatusUpdate -= new IM_StatusHandler(IM_StatusUpdate);

            if (External != null)
            {
                External.Activated -= new EventHandler(External_Activated);
                External.Deactivate -= new EventHandler(External_Deactivate);
            }

            return true;
        }

        internal override string GetTitle(bool small)
        {
            if (small)
                return "IM";

            return "IM " + Links.GetName(DhtID);
        }

        internal override Size GetDefaultSize()
        {
            return new Size(280, 350);
        }

        internal override Icon GetIcon()
        {
            return IMRes.Icon;
        }

        private void DisplayLog()
        {
            MessageTextBox.Clear();

            IMStatus status = null;
            if (!IM.IMMap.SafeTryGetValue(DhtID, out status))
                return;

            status.MessageLog.LockReading(delegate()
            {
                foreach (InstantMessage message in status.MessageLog)
                    IM_MessageUpdate(DhtID, message);
            });
        }

        private void CheckBackColor()
        {
            // higher
            if(Links.IsHigher(DhtID, 0))
                MessageTextBox.BackColor = Color.FromArgb(255, 250, 250);
            
            // lower
            else if(Links.IsHigher(DhtID, Core.LocalDhtID, 0))
                MessageTextBox.BackColor = Color.FromArgb(250, 250, 255);
            
            else
                MessageTextBox.BackColor = Color.White;
        }

        internal void InputControl_SendMessage(string message)
        {
            IM.SendMessage(DhtID, message);
        }

        void IM_StatusUpdate(ulong id)
        {
            if (id != DhtID)
                return;

            CheckBackColor();
            UpdateName();

            IMStatus status = null;
            if (!IM.IMMap.SafeTryGetValue(id, out status))
                return;

            // connected to jonn smith @home, @work
            // connecting to john smith
            // disconnected from john smith

            StatusLabel.Text = status.Text;

            if (status.Connected)
                StatusImage.Image = IMRes.greenled;
            else if (status.Away)
                StatusImage.Image = IMRes.yellowled;
            else if (status.Connecting)
                StatusImage.Image = IMRes.yellowled;
            else
                StatusImage.Image = IMRes.redled;

        }

        internal void IM_MessageUpdate(ulong dhtid, InstantMessage message)
        {
            if (dhtid != DhtID)
                return;

            int oldStart  = MessageTextBox.SelectionStart;
            int oldLength = MessageTextBox.SelectionLength;

            MessageTextBox.SelectionStart = MessageTextBox.Text.Length;
            MessageTextBox.SelectionLength = 0;

            // name, in bold, blue for incoming, red for outgoing
            if(message.System)
                MessageTextBox.SelectionColor = Color.Black;
            else if (message.Source == Core.LocalDhtID && message.ClientID == Core.ClientID)
                MessageTextBox.SelectionColor = Color.Red;
            else
                MessageTextBox.SelectionColor = Color.Blue;

            MessageTextBox.SelectionFont = BoldFont;

            string prefix = " ";
            if (!message.System)
                prefix += Links.GetName(message.Source);

            if (MessageTextBox.Text.Length != 0)
                prefix = "\n" + prefix;

            // add timestamp
            if (TimestampMenu.Checked)
            {
                MessageTextBox.AppendText(prefix);

                MessageTextBox.SelectionFont = TimeFont;
                MessageTextBox.AppendText(" (" + message.TimeStamp.ToString("T") + ")");

                MessageTextBox.SelectionFont = BoldFont;
                prefix = "";
            }

            string location = "";
            if (!message.System && Locations.ClientCount(message.Source) > 1)
                location = " @" + Locations.GetLocationName(message.Source, message.ClientID);


            if (!message.System)
                prefix += location + ": ";

            MessageTextBox.AppendText(prefix);

            // message, grey for not acked
            MessageTextBox.SelectionColor = Color.Black;

            if (message.System)
            {
                MessageTextBox.SelectionFont = SystemFont;
                MessageTextBox.AppendText(" *" + message.Text);
            }
            else
            {
                MessageTextBox.SelectionFont = RegularFont;
                MessageTextBox.SelectedRtf = message.Text;
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

            if (External != null && !WindowActivated)
                FlashMe = true;
        }

        void Menu_Timestamps(object sender, EventArgs e)
        {
            TimestampMenu.Checked = !TimestampMenu.Checked;

            DisplayLog();
        }

        private void FlashTimer_Tick(object sender, EventArgs e)
        {
            if (External != null && !WindowActivated && FlashMe)
                Win32.FlashWindow(External.Handle, true);
        }

        void External_Deactivate(object sender, EventArgs e)
        {
            WindowActivated = false;
        }

        void External_Activated(object sender, EventArgs e)
        {
            WindowActivated = true;
            FlashMe = false;
        }


    }
}

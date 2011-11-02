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
using DeOps.Interface.Views.Res;

using DeOps.Services.Location;
using DeOps.Services.Share;
using DeOps.Services.Voice;


namespace DeOps.Services.IM
{

    public partial class IM_View : ViewShell
    {
        CoreUI          UI;
        IMService       IM;
        OpCore          Core;
        LocationService Locations;
        public ulong  UserID;

        ToolStripMenuItem TimestampMenu;
        string RemoteName;

        bool WindowActivated;
        bool FlashMe;

        Font BoldFont    = new Font("Tahoma", 10, FontStyle.Bold);
        Font RegularFont = new Font("Tahoma", 10, FontStyle.Regular);
        Font TimeFont = new Font("Tahoma", 8, FontStyle.Regular);
        Font SystemFont = new Font("Tahoma", 8, FontStyle.Bold);

        VoiceToolstripButton VoiceButton;


        public IM_View(CoreUI ui, IMService im, ulong key)
        {
            InitializeComponent();

            UI = ui;
            Core = ui.Core;
            IM    = im;
            Locations = IM.Core.Locations;
            UserID = key;

            IM.ActiveUsers.Add(UserID);

            UpdateName();
            
            // do here so window can be found and multiples not created for the same user
            IM.MessageUpdate += new IM_MessageHandler(IM_MessageUpdate);
            IM.StatusUpdate += new IM_StatusHandler(IM_StatusUpdate);
            Core.KeepDataGui += new KeepDataHandler(Core_KeepData);

            MessageTextBox.Core = Core;
            MessageTextBox.ContextMenuStrip.Items.Insert(0, new ToolStripSeparator());

            TimestampMenu = new ToolStripMenuItem("Timestamps", ViewRes.timestamp, new EventHandler(Menu_Timestamps));
            MessageTextBox.ContextMenuStrip.Items.Insert(0, TimestampMenu);
        }

        private void UpdateName()
        {
            RemoteName = Core.GetName(UserID);

            if(External != null)
                External.Text = "IM " + RemoteName;
        }

        public override void Init()
        {
            InputControl.SendMessage += new TextInput.SendMessageHandler(InputControl_SendMessage);
            InputControl.SendFile += new MethodInvoker(InputControl_SendFile);
            InputControl.IgnoreUser += new MethodInvoker(InputControl_IgnoreUser);
            InputControl.AddBuddy += new MethodInvoker(InputControl_AddBuddy);

            IM.ReSearchUser(UserID);
            IM_StatusUpdate(UserID);
            DisplayLog();

            InputControl.InputBox.Select();

            if (External != null)
            {
                External.Activated += new EventHandler(External_Activated);
                External.Deactivate += new EventHandler(External_Deactivate);
            }

            if (!Core.Buddies.BuddyList.SafeContainsKey(UserID))
                InputControl.AddBuddyButton.Visible = true;


            VoiceService voices = Core.GetService(ServiceIDs.Voice) as VoiceService;
            if (voices != null)
            {
                VoiceButton = new VoiceToolstripButton(voices);
                InputControl.FontToolStrip.Items.Add(VoiceButton);
                VoiceButton.SetUsers(new List<ulong>() { Core.UserID, UserID }, AudioDirection.Both);
            }
        }

        public override bool Fin()
        {
            IM.ActiveUsers.Remove(UserID);

            InputControl.SendMessage -= new TextInput.SendMessageHandler(InputControl_SendMessage);

            IM.MessageUpdate -= new IM_MessageHandler(IM_MessageUpdate);
            IM.StatusUpdate -= new IM_StatusHandler(IM_StatusUpdate);
            Core.KeepDataGui -= new KeepDataHandler(Core_KeepData);


            if (External != null)
            {
                External.Activated -= new EventHandler(External_Activated);
                External.Deactivate -= new EventHandler(External_Deactivate);
            }

            return true;
        }

        void Core_KeepData()
        {
            Core.KeepData.SafeAdd(UserID, true);
        }

        public override string GetTitle(bool small)
        {
            if (small)
                return "IM";

            return "IM " + Core.GetName(UserID);
        }

        public override Size GetDefaultSize()
        {
            return new Size(280, 350);
        }

        public override Icon GetIcon()
        {
            return IMRes.Icon;
        }

        private void DisplayLog()
        {
            MessageTextBox.Clear();

            IMStatus status = null;
            if (!IM.IMMap.SafeTryGetValue(UserID, out status))
                return;

            status.MessageLog.LockReading(delegate()
            {
                foreach (InstantMessage message in status.MessageLog)
                    IM_MessageUpdate(UserID, message);
            });
        }

        private void CheckBackColor()
        {
            if (Core.Trust == null)
            {
                MessageTextBox.BackColor = Color.White; ;
                return;
            }

            // higher
            if (Core.Trust.IsHigher(UserID, 0))
                MessageTextBox.BackColor = Color.FromArgb(255, 250, 250);
            
            // lower
            else if (Core.Trust.IsHigher(UserID, Core.UserID, 0))
                MessageTextBox.BackColor = Color.FromArgb(250, 250, 255);
            
            else
                MessageTextBox.BackColor = Color.White;
        }

        public void InputControl_SendMessage(string message, TextFormat format)
        {
            IM.SendMessage(UserID, message, format);
        }

        public void InputControl_SendFile()
        {
            SendFileForm form = new SendFileForm(UI, UserID);

            form.FileProcessed = new Tuple<FileProcessedHandler, object>(new FileProcessedHandler(IM.Share_FileProcessed), (object)UserID);
            
            form.ShowDialog();
        }

        public void InputControl_IgnoreUser()
        {
            if (MessageBox.Show("Are you sure you want to ignore " + Core.GetName(UserID) + "?", "Ignore", MessageBoxButtons.YesNo) == DialogResult.Yes)
                Core.Buddies.Ignore(UserID, true);
        }

        public void InputControl_AddBuddy()
        {
            if(MessageBox.Show("Add " + Core.GetName(UserID) + " to you buddy list?", "Add Buddy", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Core.Buddies.AddBuddy(UserID);

                InputControl.AddBuddyButton.Visible = false;
            }
        }

        void IM_StatusUpdate(ulong id)
        {
            if (id != UserID)
                return;

            CheckBackColor();

            IMStatus status = null;
            if (!IM.IMMap.SafeTryGetValue(id, out status))
                return;

            // connected to jonn smith @home, @work
            // connecting to john smith
            // disconnected from john smith

            StatusLabel.Text = status.Text;

            if (status.Away)
                StatusImage.Image = IMRes.yellowled;
            if (status.Connected)
                StatusImage.Image = IMRes.greenled;
            else if (status.Connecting)
                StatusImage.Image = IMRes.yellowled;
            else
                StatusImage.Image = IMRes.redled;

            if(RemoteName != Core.GetName(id))
            {
                UpdateName();
                DisplayLog();
            }
        }

        public void IM_MessageUpdate(ulong id, InstantMessage message)
        {
            if (id != UserID)
                return;

            int oldStart  = MessageTextBox.SelectionStart;
            int oldLength = MessageTextBox.SelectionLength;

            MessageTextBox.SelectionStart = MessageTextBox.Text.Length;
            MessageTextBox.SelectionLength = 0;

            // name, in bold, blue for incoming, red for outgoing
            if(message.System)
                MessageTextBox.SelectionColor = Color.Black;
            else if (Core.Network.Local.Equals(message))
                MessageTextBox.SelectionColor = message.Sent ? Color.Red : Color.LightCoral;
            else
                MessageTextBox.SelectionColor = Color.Blue;

            MessageTextBox.SelectionFont = BoldFont;

            string prefix = " ";
            if (!message.System)
                prefix += Core.GetName(message.UserID);

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
            if (!message.System && Locations.ActiveClientCount(message.UserID) > 1)
                location = " @" + Locations.GetLocationName(message.UserID, message.ClientID);


            if (!message.System)
                prefix += location + ": ";

            MessageTextBox.AppendText(prefix);

            // message, grey for not acked
            MessageTextBox.SelectionColor = Color.Black;
            if (Core.Network.Local.Equals(message) && !message.Sent)
                MessageTextBox.SelectionColor = Color.LightGray;

            if (message.System)
            {
                MessageTextBox.SelectionFont = SystemFont;
                MessageTextBox.AppendText(" *" + message.Text);
            }
            else
            {
                MessageTextBox.SelectionFont = RegularFont;

                if (message.Format == TextFormat.RTF)
                    MessageTextBox.SelectedRtf = GuiUtils.RtftoColor(message.Text, MessageTextBox.SelectionColor);
                
                else if (message.Format == TextFormat.Plain)
                    MessageTextBox.AppendText(message.Text);
            }
            

            MessageTextBox.SelectionStart = oldStart;
            MessageTextBox.SelectionLength = oldLength;

            MessageTextBox.DetectLinksDefault();

            if (!MessageTextBox.Focused)
            {
                MessageTextBox.SelectionStart = MessageTextBox.Text.Length;
                MessageTextBox.ScrollToCaret();
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

        private void StatusImage_MouseClick(object sender, MouseEventArgs e)
        {
            IM.Connect(UserID);
        }
    }
}

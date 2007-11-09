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
using DeOps.Components.Link;
using DeOps.Components.Location;


namespace DeOps.Components.IM
{

    internal partial class IM_View : ViewShell
    {        
        IMControl       IM;
        LinkControl     Links;
        OpCore          Core;
        LocationControl Locations;
        internal ulong  DhtID;

        MenuItem TimestampMenu;

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
            IM.IM_Update += new IM_UpdateHandler(IM_MessageUpdate);
            Locations.GuiUpdate += new LocationGuiUpdateHandler(Location_Update);

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
            Links.LinkUpdate += new LinkUpdateHandler(Links_LinkUpdate);
            InputControl.SendMessage += new TextInput.SendMessageHandler(InputControl_SendMessage);

            Core.DCClientsUpdate += new DCClientsHandler(Core_DCClientsUpdate);
            Core.RefreshDCClients(DhtID);

            CheckBackColor();

           
            InputControl.Focus();
            InputControl.InputBox.Focus();

            DisplayLog();
            
        }

        internal override bool Fin()
        {
            InputControl.SendMessage -= new TextInput.SendMessageHandler(InputControl_SendMessage);


            IM.IM_Update -= new IM_UpdateHandler(IM_MessageUpdate);
            Core.DCClientsUpdate -= new DCClientsHandler(Core_DCClientsUpdate);

            Links.LinkUpdate -= new LinkUpdateHandler(Links_LinkUpdate);
            Locations.GuiUpdate -= new LocationGuiUpdateHandler(Location_Update);


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

        void Links_LinkUpdate(OpLink link)
        {
            CheckBackColor();
            UpdateName();
        }

        void Location_Update(ulong key)
        {
            // away msg etc.. may have been modified
            if(key == DhtID)
                Core.RefreshDCClients(key);
        }

        private void DisplayLog()
        {
            MessageTextBox.Clear();

            IM.MessageLog.LockReading(delegate()
            {
                if (IM.MessageLog.ContainsKey(DhtID))
                    foreach (InstantMessage message in IM.MessageLog[DhtID])
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

        internal void Core_DCClientsUpdate(ulong id, List<Tuple<ushort, SessionStatus>> clients)
        {
            if (id != DhtID)
                return;

            
            // connected to jonn smith @home, @work

            // connecting to john smith

            // Locations.ClientCount(DhtID) > 1

            // disconnected from john smith
            if (clients.Count == 0)
            {
                StatusImage.Image = IMRes.redled;
                StatusLabel.Text = "Disconnected from " + Core.Links.GetName(DhtID);
                return;
            }

            string places = "";
            

            bool connected = false;
            bool away = false;
            string awayMessage = "";

            foreach (Tuple<ushort, SessionStatus> client in clients)
                if (client.Second == SessionStatus.Active)
                {
                    LocInfo info = Locations.GetLocationInfo(id, client.First);

                    awayMessage = "";
                    if (info != null)
                        if (info.Location.Away)
                        {
                            away = true;
                            awayMessage = " " + info.Location.AwayMessage;
                        }
                        else
                            connected = true;

                    places += " @" + Locations.GetLocationName(DhtID, client.First) + awayMessage + ",";
                }

            if (connected)
            {
                StatusImage.Image = IMRes.greenled;
                StatusLabel.Text = "Connected to " + Core.Links.GetName(DhtID);

                if (Locations.ClientCount(DhtID) > 1)
                    StatusLabel.Text += places.TrimEnd(',');
            }

            else if (away)
            {
                StatusImage.Image = IMRes.yellowled;
                StatusLabel.Text = Core.Links.GetName(DhtID) + " is Away ";

                if (Locations.ClientCount(DhtID) > 1)
                    StatusLabel.Text += places.TrimEnd(',');
                else
                    StatusLabel.Text += awayMessage;
            }

            else
            {
                StatusImage.Image = IMRes.yellowled;
                StatusLabel.Text = "Connecting to " + Core.Links.GetName(DhtID);
            }
        }

        internal void IM_MessageUpdate(ulong dhtid, InstantMessage message)
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
            if (!message.System && Locations.ClientCount(DhtID) > 1)
                location = " @" + Locations.GetLocationName(DhtID, message.ClientID);


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
        }

        void Menu_Timestamps(object sender, EventArgs e)
        {
            TimestampMenu.Checked = !TimestampMenu.Checked;

            DisplayLog();
        }
    }
}

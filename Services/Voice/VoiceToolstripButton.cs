using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace RiseOp.Services.Voice 
{
    internal class VoiceToolstripButton : ToolStripSplitButton
    {
        VoiceService Voices;

        ToolStripMenuItem OffButton;
        ToolStripMenuItem VoiceActivatedButton;
        ToolStripMenuItem PushtoTalkButton;
        ToolStripMenuItem MuteButton;
        ToolStripMenuItem SettingsButton;

        ToolStripMenuItem SelectedButton;

        AudioDirection Direction;
        List<ulong> Users = new List<ulong>();

        int WindowID;

        int VolumeIn;
        int VolumeOut;

        SolidBrush VolumeBrush = new SolidBrush(Color.LimeGreen);


        internal VoiceToolstripButton(VoiceService voices)
        {
            Voices = voices;

            ToolTipText = "Voice Chat";

            Paint += new PaintEventHandler(VoiceToolstripButton_Paint);

            ButtonClick += new EventHandler(VoiceToolstripButton_ButtonClick);
            MouseDown += new MouseEventHandler(VoiceToolstripButton_MouseDown);
            MouseUp += new MouseEventHandler(VoiceToolstripButton_MouseUp);
            OffButton = new ToolStripMenuItem("Off", Res.VoiceRes.VoiceOff, OffButton_Clicked);
            VoiceActivatedButton = new ToolStripMenuItem("Voice Activated", Res.VoiceRes.VoiceVAD, VoiceActivatedButton_Clicked);
            PushtoTalkButton = new ToolStripMenuItem("Push to Talk", Res.VoiceRes.VoicePTT, PushtoTalkButton_Clicked);
            MuteButton = new ToolStripMenuItem("Mute", Res.VoiceRes.VoiceMute, MuteButton_Clicked);
            SettingsButton = new ToolStripMenuItem("Settings", Res.VoiceRes.VoiceSettings, SettingsButton_Clicked);

            DropDownItems.Add(OffButton);
            DropDownItems.Add(VoiceActivatedButton);
            DropDownItems.Add(PushtoTalkButton);
            DropDownItems.Add(MuteButton);
            DropDownItems.Add(SettingsButton);

            WindowID = Voices.Core.RndGen.Next();

            Voices.RegisterWindow(WindowID, new VolumeUpdateHandler(VoiceService_VolumeUpdate));

            OffButton.PerformClick();
        }

        internal void SetUsers(List<ulong> users, AudioDirection direction)
        {
            // ensure chat/im connects to self as well

            Direction = direction;
            Users = users;

            // re-apply settings to new users
            SelectedButton.PerformClick();
        }

        void VoiceToolstripButton_ButtonClick(object sender, EventArgs e)
        {
            if (SelectedButton == OffButton)
                VoiceActivatedButton.PerformClick();

            else if (SelectedButton == VoiceActivatedButton)
                OffButton.PerformClick();

            if (SelectedButton == MuteButton)
                VoiceActivatedButton.PerformClick();
        }

        void OffButton_Clicked(object sender, EventArgs args)
        {
            SetSelected(OffButton);

            Voices.ResetWindow(WindowID);

            // get updates if user talks, just dont output it to speakers
            Users.ForEach(user => Voices.ListenTo(WindowID, user, AudioDirection.None));
        }

        void VoiceActivatedButton_Clicked(object sender, EventArgs args)
        {
            SetSelected(VoiceActivatedButton);

            Voices.ResetWindow(WindowID);
            
            Users.ForEach(user =>
            {
                Voices.ListenTo(WindowID, user, Direction);
                Voices.SpeakTo(WindowID, user);
            });
        }

        void PushtoTalkButton_Clicked(object sender, EventArgs args)
        {
            SetSelected(PushtoTalkButton);

            Voices.ResetWindow(WindowID);

            Users.ForEach(user => Voices.ListenTo(WindowID, user, Direction));
        }

        void VoiceToolstripButton_MouseDown(object sender, MouseEventArgs e)
        {
            if(SelectedButton == PushtoTalkButton)
                Users.ForEach(user => Voices.SpeakTo(WindowID, user));
        }

        void VoiceToolstripButton_MouseUp(object sender, MouseEventArgs e)
        {
            if (SelectedButton == PushtoTalkButton)
                Voices.Mute(WindowID);
        }

        void MuteButton_Clicked(object sender, EventArgs args)
        {
            SetSelected(MuteButton);

            Voices.ResetWindow(WindowID);

            Users.ForEach(user => Voices.ListenTo(WindowID, user, Direction));

            Invalidate();
        }

        void VoiceService_VolumeUpdate(int inMax, int outMax)
        {
            VolumeIn = inMax * 5 / short.MaxValue;
            VolumeOut = outMax * 5 / short.MaxValue;

            Invalidate();
        }

        void VoiceToolstripButton_Paint(object sender, PaintEventArgs e)
        {
            // 22 x 22, 3 pix border on each side

            // volume in
            int x = 8;
            int y = 4;

            if (SelectedButton == OffButton)
            {
                x = 3;
                y = 14;
            }

            DrawVolume(e, x, y, VolumeIn);

            // volume out
            if (SelectedButton != MuteButton && SelectedButton != OffButton)
                DrawVolume(e, 8, 15, VolumeOut);
        }

        Pen LowVolPen = new Pen(Color.LimeGreen);
        Pen MidVolPen = new Pen(Color.Orange);
        Pen HiVolPen = new Pen(Color.Red);
        

        private void DrawVolume(PaintEventArgs e, int x, int y, int volume)
        {
            if (volume == 0)
                return;

            for (int i = 0; i <= 5; i++)
            {
                if (i > volume)
                    break;

                Pen volPen = LowVolPen;
                if (i == 4)
                    volPen = MidVolPen;
                else if (i == 5)
                    volPen = HiVolPen;

                e.Graphics.DrawLine(volPen, x + i * 2, y, x + i * 2, y + 3);
            }
        }
        
        void SettingsButton_Clicked(object sender, EventArgs args)
        {
            VoiceSettings settings = new VoiceSettings(Voices);

            settings.ShowDialog();
        }

        private void SetSelected(ToolStripMenuItem button)
        {
            Image = button.Image;

            SelectedButton = button;
        }

        protected override void Dispose(bool disposing)
        {
            Voices.UnregisterWindow(WindowID);

            base.Dispose(disposing);
        }

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using RiseOp.Interface;


namespace RiseOp.Services.Voice
{
    internal partial class VoiceSettings : CustomIconForm 
    {
        VoiceService Voices;


        internal VoiceSettings(VoiceService voices)
        {
            InitializeComponent();

            Voices = voices;

            // recording
            for (int i = 0; i < WinMM.waveInGetNumDevs(); i++)
            {
                WinMM.WaveInCaps device = new WinMM.WaveInCaps();

                WinMM.ErrorCheck(WinMM.waveInGetDevCaps(i, ref device, Marshal.SizeOf(device)));

                DeviceInCombo.Items.Add(device.szPname);
            }

            DeviceInCombo.SelectedIndex = 0;

            // playback
            for (int i = 0; i < WinMM.waveOutGetNumDevs(); i++)
            {
                WinMM.WaveOutCaps device = new WinMM.WaveOutCaps();

                WinMM.ErrorCheck(WinMM.waveOutGetDevCaps(i, ref device, Marshal.SizeOf(device)));

                DeviceOutCombo.Items.Add(device.szPname);

            }

            DeviceOutCombo.SelectedIndex = 0;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            Voices.RecordingDevice = DeviceInCombo.SelectedIndex;
            Voices.PlaybackDevice = DeviceOutCombo.SelectedIndex;

            Voices.ResetDevices();

            Close();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;


namespace RiseOp.Services.Voice
{
    class PlayAudio : IDisposable
    {
        VoiceService Voices;
        RemoteVoice User;

        internal int FrameSize;
        int PlayingDevice = -1;

        IntPtr WaveHandle;
        WinMM.WaveFormat Format;
        WinMM.WaveDelegate CallbackHandler;
        AutoResetEvent RecordEvent = new AutoResetEvent(false);

        int BufferSize;
        int FilledBuffers;
        int NextBuffer;
        const int BufferCount = 5; // 1/10 of a second in buffers, 20ms each
        PlayBuffer[] Buffers = new PlayBuffer[BufferCount];

        Speex.SpeexBits DecodeBits;
        IntPtr SpeexDecoder;
        int SpeexMode;

        Queue<byte[]> AudioQueue = new Queue<byte[]>();

        int HistoryLength = 10 * 50; // 10 seconds, 50 clips/second
        Queue<byte[]> History = new Queue<byte[]>();


        internal PlayAudio(VoiceService voices, int frameSize, RemoteVoice user)
        {
            Voices = voices;
            User = user;

            CallbackHandler = new WinMM.WaveDelegate(WaveCallback);


            FrameSize = frameSize;

            // if 20ms, at high quality (16khz) is 320 samples at 2 bytes each
            if (FrameSize == 320)
            {
                Format = new WinMM.WaveFormat(16000, 16, 2);
                BufferSize = 320 * 2 * 2; // 2 bytes each frame, 2 channels
                SpeexMode = Speex.SPEEX_MODEID_WB;
            }
            else if(FrameSize == 160)
            {
                Format = new WinMM.WaveFormat(8000, 16, 2);
                BufferSize = 160 * 2 * 2;
                SpeexMode = Speex.SPEEX_MODEID_NB;
            }
            else
            {
                Dispose();
                return;
            }


            try
            {
                // init speex
                Speex.speex_bits_init(ref DecodeBits);
                IntPtr modePtr = Speex.speex_lib_get_mode(SpeexMode);
                SpeexDecoder = Speex.speex_decoder_init(modePtr);

                // init wave
                WinMM.ErrorCheck(WinMM.waveOutOpen(out WaveHandle, PlayingDevice, Format, CallbackHandler, 0, WinMM.CALLBACK_FUNCTION));

                for (int i = 0; i < BufferCount; i++)
                    Buffers[i] = new PlayBuffer(i, WaveHandle, BufferSize);
            }
            catch (Exception ex)
            {
                Dispose();
                Voices.Core.Network.UpdateLog("Voice", "Error starting playing: " + ex.Message);
            }
        }

        internal void WaveCallback(IntPtr hdrvr, int uMsg, int dwUser, ref WinMM.WaveHdr wavhdr, int dwParam2)
        {
            if (uMsg == WinMM.MM_WOM_DONE)
            {
                lock (AudioQueue)
                {
                    FilledBuffers--;

                    FillBuffers();
                }
            }
        }

        public void Dispose()
        {
            try
            {
                WinMM.ErrorCheck(WinMM.waveOutReset(WaveHandle));

                // free buffers
                foreach (PlayBuffer buffer in Buffers)
                    buffer.Dispose();
                Buffers = null;

                // free speex
                Speex.speex_bits_destroy(ref DecodeBits);
                Speex.speex_decoder_destroy(SpeexDecoder);

                WinMM.ErrorCheck(WinMM.waveOutClose(WaveHandle));
            }
            catch (Exception ex)
            {
                Voices.Core.Network.UpdateLog("Voice", "Error Disposing Player: " + ex.Message);
            }

            // remove wave out stream from user structure
            foreach(ulong routing in User.Streams.Keys)
                if (User.Streams[routing] == this)
                {
                    User.Streams.Remove(routing);
                    break;
                }
        }

        internal void Receive_AudioData(byte[] data)
        {
            // enqueue, trigger fill audio buffers
            lock (AudioQueue)
            {
                AudioQueue.Enqueue(data);

                FillBuffers();
            }
        }

        private void FillBuffers()
        {
            try
            {
                while (FilledBuffers < BufferCount)
                {
                    byte[] data = null;
                    lock (AudioQueue) // either gets called from core, or system thread
                    {

                        if (AudioQueue.Count == 0)
                            return;

                        data = AudioQueue.Dequeue();
                    }
                    // keep a log of received audio that user can back track through

                    History.Enqueue(data);

                    while (History.Count > HistoryLength)
                        History.Dequeue();



                    // decode
                    Speex.speex_bits_reset(ref DecodeBits);

                    Speex.speex_bits_read_from(ref DecodeBits, data, data.Length);

                    byte[] mono = new byte[FrameSize * 2];
                    int success = Speex.speex_decode_int(SpeexDecoder, ref DecodeBits, mono);

                    if (success != 0)
                        continue;

                    // get volume
                    short maxVolume = 0;
                    for (int i = 0; i < mono.Length / 2; i++)
                    {
                        short val = BitConverter.ToInt16(mono, i * 2);
                        if (val > maxVolume)
                            maxVolume = val;
                    }


                    foreach (int window in User.ListeningTo.Keys)
                        if (Voices.MaxVolume.ContainsKey(window) && maxVolume > Voices.MaxVolume[window].Param1)
                            Voices.MaxVolume[window].Param1 = maxVolume;


                    // find out where audio should come out from, if at all
                    // return down here so that even if user not listening, window shows volume bar
                    AudioDirection direction = User.GetDirection();

                    if (direction == AudioDirection.None)
                        continue;


                    // shifting to one side
                    PlayBuffer buffer = Buffers[NextBuffer];

                    for (int i = 0; i < mono.Length / 2; i++)
                        switch (direction)
                        {
                            case AudioDirection.Both:
                                Buffer.BlockCopy(mono, i * 2, buffer.Data, i * 4, 2); // left
                                Buffer.BlockCopy(mono, i * 2, buffer.Data, i * 4 + 2, 2); // right
                                break;

                            case AudioDirection.Left:
                                Buffer.BlockCopy(mono, i * 2, buffer.Data, i * 4, 2); // left
                                break;

                            case AudioDirection.Right:
                                Buffer.BlockCopy(mono, i * 2, buffer.Data, i * 4 + 2, 2); // right
                                break;
                        }


                    WinMM.ErrorCheck(WinMM.waveOutWrite(WaveHandle, ref buffer.Header, Marshal.SizeOf(buffer.Header)));

                    FilledBuffers++;

                    NextBuffer++;
                    if (NextBuffer >= BufferCount)
                        NextBuffer = 0;
                }
            }
            catch (Exception ex)
            {
                Voices.Core.RunInCoreAsync(() =>
                {
                    Dispose();
                    Voices.Core.Network.UpdateLog("Voice", "Error filling buffers: " + ex.Message);
                });
            }
        }
    }

    internal enum AudioDirection { None, Left, Right, Both }

    internal class PlayBuffer : IDisposable
    {
        IntPtr WaveHandle;

        internal WinMM.WaveHdr Header;
        GCHandle HeaderHandle;

        internal byte[] Data;
        GCHandle DataHandle;
        internal IntPtr DataPtr;


        internal PlayBuffer(int index, IntPtr handle, int size)
        {
            WaveHandle = handle;

            HeaderHandle = GCHandle.Alloc(Header, GCHandleType.Pinned);

            Data = new byte[size];
            DataHandle = GCHandle.Alloc(Data, GCHandleType.Pinned);
            DataPtr = DataHandle.AddrOfPinnedObject();

            //Header.dwUser = new IntPtr(index); //(IntPtr)GCHandle.Alloc(this);
            Header.lpData = DataHandle.AddrOfPinnedObject();
            Header.dwBufferLength = size;

            WinMM.ErrorCheck(WinMM.waveOutPrepareHeader(WaveHandle, ref Header, Marshal.SizeOf(Header)));
        }

        public void Dispose()
        {
            if (HeaderHandle.IsAllocated)
            {
                WinMM.ErrorCheck(WinMM.waveOutUnprepareHeader(WaveHandle, ref Header, Marshal.SizeOf(Header)));

                HeaderHandle.Free();
            }

            if (DataHandle.IsAllocated)
                DataHandle.Free();
        }
    }


}

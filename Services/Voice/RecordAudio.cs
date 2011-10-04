using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;


namespace DeOps.Services.Voice
{
    class RecordAudio : IDisposable
    {
        VoiceService Voices;

        IntPtr WaveHandle;
        WinMM.WaveFormat Format;
        WinMM.WaveDelegate CallbackHandler;
        AutoResetEvent RecordEvent = new AutoResetEvent(false);

        bool HighQuality = true;
        internal int FrameSize;
        int SampleRate;

        int BufferSize;
        int NextBuffer;
        const int BufferCount = 5; // 1/10 of a second in buffers, 20ms each
        RecordBuffer[] Buffers = new RecordBuffer[BufferCount];

        Speex.SpeexBits EncodeBits;
        IntPtr SpeexEncoder;
        int SpeexMode;
        byte[] EncodedBytes;
        IntPtr PreProcessor;
        //internal IntPtr EchoState;


        internal RecordAudio(VoiceService voices)
        {
            Voices = voices;

            CallbackHandler = new WinMM.WaveDelegate(WaveCallback);


            // if 20ms, at high quality (16khz) is 320 samples at 2 bytes each
            if (HighQuality)
            {
                SampleRate = 16000;
                BufferSize = 320 * 2;
                SpeexMode = Speex.SPEEX_MODEID_WB;
            }
            else
            {
                SampleRate = 8000;
                BufferSize = 160 * 2;
                SpeexMode = Speex.SPEEX_MODEID_NB;
            }

            //LastBuffer = new byte[BufferSize];

            try
            {
                InitSpeexEncoder();

                Format = new WinMM.WaveFormat(SampleRate, 16, 1);
                WinMM.ErrorCheck(WinMM.waveInOpen(out WaveHandle, Voices.RecordingDevice, Format, CallbackHandler, 0, WinMM.CALLBACK_FUNCTION));

                for (int i = 0; i < BufferCount; i++)
                    Buffers[i] = new RecordBuffer(i, WaveHandle, BufferSize);

                WinMM.ErrorCheck(WinMM.waveInStart(WaveHandle));
            }
            catch (Exception ex)
            {
                Dispose();
                Voices.Core.Network.UpdateLog("Voice", "Error starting recording: " + ex.Message);
            }
        }

        private void InitSpeexEncoder()
        {
            // init
            Speex.speex_bits_init(ref EncodeBits);

            // get narrow band mode
            IntPtr modePtr = Speex.speex_lib_get_mode(SpeexMode);

            SpeexEncoder = Speex.speex_encoder_init(modePtr);

            //int zeroNoError = 0;

            int tmp = 0; // no variable bit rate
            Speex.speex_encoder_ctl(SpeexEncoder, Speex.SPEEX_SET_VBR, ref tmp);
            
            tmp = 4; // ok quality
            Speex.speex_encoder_ctl(SpeexEncoder, Speex.SPEEX_SET_QUALITY, ref tmp);
            
            tmp = 1; // uses a little more cpu for better processing
            Speex.speex_encoder_ctl(SpeexEncoder, Speex.SPEEX_SET_COMPLEXITY, ref tmp);

            tmp = 1; // voice activated, deadspace is not encoded
            Speex.speex_encoder_ctl(SpeexEncoder, Speex.SPEEX_SET_VAD, ref tmp);

            tmp = 1; // dead space no transmission
            Speex.speex_encoder_ctl(SpeexEncoder, Speex.SPEEX_SET_DTX, ref tmp);

            Speex.speex_encoder_ctl(SpeexEncoder, Speex.SPEEX_GET_FRAME_SIZE, ref FrameSize);
            Debug.Assert(FrameSize == BufferSize / 2);
            if (FrameSize != BufferSize / 2)
                throw new Exception("Frame size " + FrameSize + " did agree with buffer " + BufferSize);

            /* Turn this off if you want to measure SNR (on by default) */
            tmp = 1;
            Speex.speex_encoder_ctl(SpeexEncoder, Speex.SPEEX_SET_HIGHPASS, ref tmp);

    
            // pre-processor
            PreProcessor = Speex.speex_preprocess_state_init(FrameSize, SampleRate);

            tmp = 1;
            Speex.speex_preprocess_ctl(PreProcessor, Speex.SPEEX_PREPROCESS_SET_DENOISE, ref tmp);

            // echo cancelation
            //EchoState = Speex.speex_echo_state_init(FrameSize, FrameSize * 3); // 100ms tail length suggested

            //Speex.speex_preprocess_ctl(PreProcessor, Speex.SPEEX_PREPROCESS_SET_ECHO_STATE, EchoState); 



            EncodedBytes = new byte[BufferSize];
        }

        int AddedBuffers;

        internal void WaveCallback(IntPtr hdrvr, int uMsg, int dwUser, ref WinMM.WaveHdr wavhdr, int dwParam2)
        {
            if (uMsg == WinMM.MM_WIM_DATA)
            {
                // use dwUser parameter of header to keep buffers in sync?
                // doesnt seem like buffers are getting out of sync at all

                AddedBuffers--;

                Voices.AudioEvent.Set();
            }
        }

        internal void ProcessBuffers()
        {
            try
            {
                while (AddedBuffers < BufferCount)
                {
                    RecordBuffer buffer = Buffers[NextBuffer];

                    if (buffer.Added)
                        EncodeAudio(buffer);

                        /*if (!EncodeAudio(buffer) && LastBufferSet)
                        {
                            // no data sent - send last buffer so that audio doesnt snap, but fades out
                            // reverse - so waves match up
                            byte[] frame = new byte[2];
                            int backpos = 0;
                            for (int i = 0; i < FrameSize / 2; i++)
                            {
                                backpos = LastBuffer.Length - 2 - i * 2;
                                Buffer.BlockCopy(LastBuffer, i * 2, frame, 0, 2); // front to temp
                                Buffer.BlockCopy(LastBuffer, backpos, LastBuffer, i * 2, 2); // back to front
                                Buffer.BlockCopy(frame, 0, LastBuffer, backpos, 2); // temp to back
                            }

                            // fade so no sharp end to the sound
                            for (int i = 0; i < FrameSize; i++)
                            {
                                short value = BitConverter.ToInt16(LastBuffer, i * 2);
                                value = (short) (value * (FrameSize - i) / FrameSize);
                                BitConverter.GetBytes(value).CopyTo(LastBuffer, i * 2);
                            }

                            LastBuffer.CopyTo(buffer.Data, 0);
                            
                            int tmp = 0;
                            Speex.speex_encoder_ctl(SpeexEncoder, Speex.SPEEX_SET_VAD, ref tmp);
                            Speex.speex_encoder_ctl(SpeexEncoder, Speex.SPEEX_SET_DTX, ref tmp);

                            //EncodeAudio(buffer);

                            tmp = 1;
                            Speex.speex_encoder_ctl(SpeexEncoder, Speex.SPEEX_SET_VAD, ref tmp);
                            Speex.speex_encoder_ctl(SpeexEncoder, Speex.SPEEX_SET_DTX, ref tmp);


                            LastBufferSet = false;
                        }*/

                    WinMM.ErrorCheck(WinMM.waveInAddBuffer(WaveHandle, ref buffer.Header, Marshal.SizeOf(buffer.Header)));
                    buffer.Added = true;

                    NextBuffer++;
                    if (NextBuffer >= BufferCount)
                        NextBuffer = 0;

                    AddedBuffers++;
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false);

                Voices.Core.RunInCoreAsync(() =>
                {
                    Voices.Core.Network.UpdateLog("Voice", "Error in record thread: " + ex.Message);
                    Dispose();
                });
            }
        }

        //byte[] LastBuffer;
        //bool LastBufferSet;
        //byte[] EchoBuff;

        private bool EncodeAudio(RecordBuffer buffer)
        {
            // done in a seperate function to avoid buffer from being re-assigned while delegate is being processed

            short maxVolume = 0;
            for (int i = 0; i < BufferSize / 2; i++)
            {
                short val = BitConverter.ToInt16(buffer.Data, i * 2);
                if (val > maxVolume)
                    maxVolume = val;
            }

            // pre-process
            Speex.speex_preprocess_run(PreProcessor, buffer.Data); 

            // echo cancel
            //if (EchoBuff == null) EchoBuff = new byte[BufferSize];
            //Speex.speex_echo_capture(EchoState, buffer.Data, EchoBuff);
            //EchoBuff.CopyTo(buffer.Data, 0);

            // encode
            Speex.speex_bits_reset(ref EncodeBits);

            int success = Speex.speex_encode_int(SpeexEncoder, buffer.DataPtr, ref EncodeBits);

            if (success == 0) // dtx returns 0 if no data
                return false;

            int written = Speex.speex_bits_write(ref EncodeBits, EncodedBytes, EncodedBytes.Length);

            // filler is 10b high quality, 6b low quality, dont write filler only good audio
            if (written > 10)
            {
                //buffer.Data.CopyTo(LastBuffer, 0);
                //LastBufferSet = true;

                byte[] safeBuffer = Utilities.ExtractBytes(EncodedBytes, 0, written);

                // pass frame size because recorder could be null by the time event gets there
                Voices.Core.RunInCoreAsync(() => Voices.Recorder_AudioData(safeBuffer, maxVolume, FrameSize));
                
                return true;
            }

            return false;
        }


        public void Dispose()
        {
            try
            {
                WinMM.ErrorCheck(WinMM.waveInReset(WaveHandle));

                // free buffers
                foreach (RecordBuffer buffer in Buffers)
                    buffer.Dispose();
                Buffers = null;
                
                // free speex
                Speex.speex_bits_destroy(ref EncodeBits);
                Speex.speex_encoder_destroy(SpeexEncoder);
                Speex.speex_preprocess_state_destroy(PreProcessor);
                //Speex.speex_echo_state_destroy(EchoState); 

                WinMM.ErrorCheck(WinMM.waveInClose(WaveHandle));
            }
            catch (Exception ex)
            {
                Voices.Core.Network.UpdateLog("Voice", "Error Disposing: " + ex.Message);
            }

            Voices.Recorder = null;
        }
    }


    internal class RecordBuffer : IDisposable
    {
        IntPtr WaveHandle;

        internal WinMM.WaveHdr Header;
        GCHandle HeaderHandle;

        internal byte[] Data;
        GCHandle DataHandle;
        internal IntPtr DataPtr;

        internal bool Added;

        internal RecordBuffer(int index, IntPtr handle, int size)
        {
            WaveHandle = handle;

            HeaderHandle = GCHandle.Alloc(Header, GCHandleType.Pinned);
            
            Data = new byte[size];
            DataHandle = GCHandle.Alloc(Data, GCHandleType.Pinned);
            DataPtr = DataHandle.AddrOfPinnedObject();

            //Header.dwUser = new IntPtr(index); //(IntPtr)GCHandle.Alloc(this);
            Header.lpData = DataHandle.AddrOfPinnedObject();
            Header.dwBufferLength = size;

            WinMM.ErrorCheck(WinMM.waveInPrepareHeader(WaveHandle, ref Header, Marshal.SizeOf(Header)));
        }

        public void Dispose()
        {
            if (HeaderHandle.IsAllocated)
            {
                WinMM.ErrorCheck(WinMM.waveInUnprepareHeader(WaveHandle, ref Header, Marshal.SizeOf(Header)));
              
                HeaderHandle.Free();
            }

            if (DataHandle.IsAllocated)
                DataHandle.Free();
        }
    }
}

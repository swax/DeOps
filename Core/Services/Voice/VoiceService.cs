using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Transport;


namespace DeOps.Services.Voice
{
    public delegate void VolumeUpdateHandler(int inMax, int outMax);


    public class VoiceService : OpService
    {
        public string Name { get { return "Voice"; } }
        public uint ServiceID { get { return (uint)ServiceIDs.Voice; } }

        public OpCore Core;
        public DhtNetwork Network;

        public Dictionary<int, VolumeUpdateHandler> VolumeUpdate = new Dictionary<int, VolumeUpdateHandler>(); // gui event

        public Dictionary<ulong, RemoteVoice> RemoteVoices = new Dictionary<ulong, RemoteVoice>();
        public ThreadedList<PlayAudio> Players = new ThreadedList<PlayAudio>();

        public Dictionary<ulong, List<int>> SpeakingTo = new Dictionary<ulong, List<int>>(); // user, window

        public RecordAudio Recorder;

        public int RecordingDevice = -1;
        public int PlaybackDevice = -1;

        int UpdateTimeout = 1000 / 4; // 200ms, 5/second
        Stopwatch LastUpdate = new Stopwatch();
        public Dictionary<int, Tuple<int, int>> MaxVolume = new Dictionary<int, Tuple<int, int>>(); // window, volume<in,out>
        

        public VoiceService(OpCore core)
        {
            Core = core;
            Network = core.Network;

            Core.SecondTimerEvent += Core_SecondTimer;
            
            Network.RudpControl.SessionData[ServiceID, 0] += new SessionDataHandler(Session_Data);
            Network.LightComm.Data[ServiceID, 0] += new LightDataHandler(LightComm_ReceiveData);

            LastUpdate.Start();
        }

        public void Dispose()
        {
            Core.SecondTimerEvent -= Core_SecondTimer;
            
            Network.RudpControl.SessionData[ServiceID, 0] -= new SessionDataHandler(Session_Data);
            Network.LightComm.Data[ServiceID, 0] -= new LightDataHandler(LightComm_ReceiveData);

            ResetDevices();

            SpeakingTo.Clear();
            RemoteVoices.Clear();
            VolumeUpdate.Clear();
        }

        public void ResetDevices()
        {
            // kill thread
            if (AudioThread != null)
            {
                ThreadRunning = false;
                AudioEvent.Set();
                AudioThread.Join(2000);
                AudioThread = null;
            }

            // deconstruct all record/play streams
            if (Recorder != null)
            {
                Recorder.Dispose();
                Recorder = null;
            }

            Players.ForEach(p => p.Dispose());
            Players.SafeClear();
            
            // will auto be recreated
        }

        public void SimTest()
        {
            return;
        }

        public void SimCleanup()
        {
            return;
        }

        void Core_SecondTimer()
        {
            UpdateVolume();

            if (SpeakingTo.Count == 0)
                RecordingActive = false;

            // hearing audio does not time out, we keep it so the history can always be had
        }

        Thread AudioThread;
        bool ThreadRunning;
        public AutoResetEvent AudioEvent = new AutoResetEvent(false);
        bool RecordingActive;

        void StartAudioThread(bool record)
        {
            if (record)
            {
                if (Recorder == null)
                {
                    RecordAudio tmp = new RecordAudio(this);
                    Recorder = tmp; // use temp so audio thread doesn't use before ready
                }

                RecordingActive = true;
                AudioEvent.Set();
            }

            if (AudioThread != null && AudioThread.IsAlive)
                return;

            AudioThread = new Thread(new ThreadStart(RunAudioThread));
            AudioThread.Name = "Voice Thread";
            ThreadRunning = true;
            AudioThread.Start();
        }

        void RunAudioThread()
        {
            while (ThreadRunning)
            {
                AudioEvent.WaitOne();

                if (Recorder != null && RecordingActive)
                    lock(Recorder)
                        Recorder.ProcessBuffers();

                Players.LockReading(() => Players.ForEach(p => p.ProcessBuffers()));
            }
        }

        public void RegisterWindow(int window, VolumeUpdateHandler volumeEvent)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => RegisterWindow(window, volumeEvent));
                return;
            }

            MaxVolume[window] = new Tuple<int, int>(0, 0);

            VolumeUpdate[window] = volumeEvent;
        }

        public void ResetWindow(int window)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => ResetWindow(window));
                return;
            }

            // remove all entries in in/out for window id
            Mute(window);

            foreach (RemoteVoice remote in RemoteVoices.Values)
                if (remote.ListeningTo.ContainsKey(window))
                    remote.ListeningTo.Remove(window);
        }

        public void UnregisterWindow(int window)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => UnregisterWindow(window));
                return;
            }

            ResetWindow(window);

            if (MaxVolume.ContainsKey(window))
                MaxVolume.Remove(window);

            if (VolumeUpdate.ContainsKey(window))
                VolumeUpdate.Remove(window);
        }

        public void ListenTo(int window, ulong user, AudioDirection direction)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => ListenTo(window, user, direction));
                return;
            }

            // incoming voice from user will be outputted to speaker, and window notified

            if (!RemoteVoices.ContainsKey(user))
                RemoteVoices[user] = new RemoteVoice();

            RemoteVoice remote = RemoteVoices[user];

            remote.ListeningTo[window] = direction;
        }

        public void SpeakTo(int window, ulong user)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => SpeakTo(window, user));
                return;
            }

            // voice into microphone will be recorded and sent to this user
            if (!SpeakingTo.ContainsKey(user))
                SpeakingTo[user] = new List<int>();

            if(!SpeakingTo[user].Contains(window))
                SpeakingTo[user].Add(window);


            StartAudioThread(true);
        }

        public void Mute(int window)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => Mute(window));
                return;
            }

            // stops sending microphone input to window (mute)
            
            foreach (ulong user in SpeakingTo.Keys)
                if (SpeakingTo[user].Contains(window))
                    SpeakingTo[user].Remove(window);

            foreach (ulong user in SpeakingTo.Keys.Where(user => SpeakingTo[user].Count == 0).ToArray())
                SpeakingTo.Remove(user);
        }
        
        public void Recorder_AudioData(byte[] audio, int maxVolume, int frameSize)
        {
            // IM/Chat services keep these connections alive

            // send audio data, rudp to all users in hearing, tag with audio quality info

            AudioPacket packet = new AudioPacket() { Audio = audio, FrameSize = frameSize };

            int[] windows = new int[0];

            // for each user in the speaking
            foreach (ulong user in SpeakingTo.Keys)
            {
                foreach (int window in SpeakingTo[user])
                    if (MaxVolume.ContainsKey(window) && maxVolume > MaxVolume[window].Param2)
                        MaxVolume[window].Param2 = maxVolume;

                foreach (RudpSession session in Network.RudpControl.GetActiveSessions(user))
                    session.SendUnreliable(ServiceID, 0, packet);
                    //Core.Network.LightComm.SendUnreliable(session.Comm.PrimaryAddress, ServiceID, 0, packet);

                //foreach (RudpSession session in Network.RudpControl.GetActiveSessions(user))
                //    session.SendData(ServiceID, 0, packet, true);
            }

            UpdateVolume();
        }

        private void UpdateVolume()
        {
            if (LastUpdate.ElapsedMilliseconds > UpdateTimeout)
            {
                // thread safe get volume of incoming audio
                foreach (RemoteVoice remote in RemoteVoices.Values)
                {
                    foreach (int window in remote.ListeningTo.Keys)
                        if (MaxVolume.ContainsKey(window) && remote.VolumeIn > MaxVolume[window].Param1)
                            MaxVolume[window].Param1 = remote.VolumeIn;

                    remote.VolumeIn = 0;
                }

                // alert each window with its current volume status in/out and reset
                foreach (int window in MaxVolume.Keys)
                {
                    Tuple<int, int> volume = MaxVolume[window];

                    Core.RunInGuiThread(VolumeUpdate[window], volume.Param1, volume.Param2);

                    volume.Param1 = 0;
                    volume.Param2 = 0;
                }

                LastUpdate.Reset();
                LastUpdate.Start();
            }
        }

        void LightComm_ReceiveData(DhtClient client, byte[] data)
        {
            Comm_ReceiveData(client, data);
        }

        void Session_Data(RudpSession session, byte[] data)
        {
            Comm_ReceiveData(session, data);     
        }

        void Comm_ReceiveData(DhtClient client, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (G2Protocol.ReadPacket(root))
            {
                switch (root.Name)
                {
                    case VoicePacket.Audio:
                        ReceiveAudio(AudioPacket.Decode(root), client);
                        break;
                }
            }
        }

        private void ReceiveAudio(AudioPacket packet, DhtClient client)
        {
            if (!RemoteVoices.ContainsKey(client.UserID))
                RemoteVoices[client.UserID] = new RemoteVoice();

            RemoteVoice user = RemoteVoices[client.UserID];

            if (!user.Streams.ContainsKey(client.RoutingID))
            {
                user.Streams[client.RoutingID] = new PlayAudio(this, packet.FrameSize, user);
                Players.SafeAdd(user.Streams[client.RoutingID]);
            }

            PlayAudio stream = user.Streams[client.RoutingID];

            // reset if user changed quality setting
            if (stream.FrameSize != packet.FrameSize)
            {
                stream.Dispose();
                user.Streams[client.RoutingID] = new PlayAudio(this, packet.FrameSize, user);
                Players.SafeAdd(user.Streams[client.RoutingID]);
                stream = user.Streams[client.RoutingID];
            }

            StartAudioThread(false);

            stream.Receive_AudioData(packet.Audio);

            UpdateVolume();
        }
    }

    public class RemoteVoice
    {
        // window and direction audio comes from
        public Dictionary<int, AudioDirection> ListeningTo = new Dictionary<int, AudioDirection>();

        // routing ID, and audio stream for that user
        public Dictionary<ulong, PlayAudio> Streams = new Dictionary<ulong, PlayAudio>();

        public int VolumeIn;


        public AudioDirection GetDirection()
        {
            AudioDirection result = AudioDirection.None;

            foreach (AudioDirection direction in ListeningTo.Values)
            {
                if (direction == AudioDirection.None)
                    continue;

                if (result == AudioDirection.None)
                    result = direction;

                else if ( direction == AudioDirection.Both ||
                         (direction == AudioDirection.Left && result == AudioDirection.Right) || 
                         (direction == AudioDirection.Right && result == AudioDirection.Left))
                    result = AudioDirection.Both;


                if (result == AudioDirection.Both)
                    break;
            }

            return result;
        }
    }

    public class VoicePacket
    {
        public const byte Audio = 0x10;
    }

    public class AudioPacket : G2Packet
    {
        const byte Packet_FrameSize = 0x10;


        public byte[] Audio;
        public int FrameSize;


        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame packet = protocol.WritePacket(null, VoicePacket.Audio, Audio);

                protocol.WritePacket(packet, Packet_FrameSize, CompactNum.GetBytes(FrameSize));

                return protocol.WriteFinish();
            }
        }

        public static AudioPacket Decode(G2Header root)
        {
            AudioPacket packet = new AudioPacket();
            
            if (G2Protocol.ReadPayload(root))
                packet.Audio = Utilities.ExtractBytes(root.Data, root.PayloadPos, root.PayloadSize);

            G2Protocol.ResetPacket(root);

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_FrameSize:
                        packet.FrameSize = CompactNum.ToInt32(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return packet;
        }
    }
}

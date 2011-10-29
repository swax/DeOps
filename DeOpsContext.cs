using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Packets;

using DeOps.Interface;
using DeOps.Simulator;

using DeOps.Services.Share;
using DeOps.Services.Update;
using System.Drawing;

// v1.0.0 s1
// v1.0.1 s2
// v1.0.2 s3
// v1.0.3 s4
// v1.0.4 s5
// v1.0.5 s6
// v1.0.6 s7
// v1.0.7 s8
// v1.0.8 s9
// v1.0.9
// v1.1.0 s10
// v1.1.1 s11
// v1.1.2 s12
// v1.1.3 s13

namespace DeOps
{
    public class DeOpsContext
    {
        internal OpCore Lookup;
        internal LookupSettings LookupConfig;
        internal ThreadedList<OpCore> Cores = new ThreadedList<OpCore>();

        Timer SecondTimer;

        //string UpdatePath; // news page used to alert users of non-autoupdates

        internal UpdateInfo SignedUpdate;
        internal uint LocalSeqVersion = 13;

        internal BandwidthLog Bandwidth = new BandwidthLog(10);
        internal Dictionary<uint, string> KnownServices = new Dictionary<uint, string>();

        internal Thread ContextThread;

        internal SimInstance Sim;

        internal LightLicense LicenseProof;

        public Action<string[]> ShowLogin;
        public Func<LookupSettings, bool> NotifyUpdateReady;

        public string StartupPath;

        public Icon DefaultIcon;

        internal DeOpsContext(string startupPath, Icon defaultIcon)
        {
            StartupPath = startupPath;
            DefaultIcon = defaultIcon;

            LookupConfig = new LookupSettings(startupPath);

            ContextThread = Thread.CurrentThread;

            // start timers
            SecondTimer = new Timer(SecondTimer_Tick, null, 0, 1000);

            // check for updates - update through network, use news page to notify user of updates
            //new System.Threading.Thread(CheckForUpdates).Start();
            SignedUpdate = UpdateService.LoadUpdate(LookupConfig);
        }

        internal DeOpsContext(SimInstance sim, string startupPath, Icon defaultIcon)
        {
            StartupPath = startupPath;
            DefaultIcon = defaultIcon;

            LookupConfig = new LookupSettings(startupPath);

            // starting up simulated context context->simulator->instances[]->context
            Sim = sim;
        }

        float FastestUploadSpeed = 10;

        internal void SecondTimer_Tick(object state)
        {
            // flag set, actual timer code run in thread per core

            if (Lookup != null)
                Lookup.SecondTimer();

            Cores.SafeForEach(c => c.SecondTimer());

            // bandwidth
            Bandwidth.NextSecond();

            // fastest degrades over time, min is 10kb/s
            FastestUploadSpeed--;
            FastestUploadSpeed = Math.Max(Bandwidth.Average(Bandwidth.Out, 10), FastestUploadSpeed);
            FastestUploadSpeed = Math.Max(10, FastestUploadSpeed);

            AssignUploadSlots();
        }

        internal void AssignUploadSlots()
        {
            int activeTransfers = 0;
            OpCore next = null;

            Cores.SafeForEach(core =>
            {
                activeTransfers += core.Transfers.ActiveUploads;

                if (next == null || core.Transfers.NeedUploadWeight > next.Transfers.NeedUploadWeight)
                    next = core;
            });

            // max number of active transfers 15
            if (next == null || activeTransfers >= 15)
                return;

            // allocate a min of 5kb/s per transfer
            // allow a min of 2 transfers
            // if more than 10kb/s free, after accounting for upload speed allow another transfer
            // goal push transfers down to around 5kb/s, 30 secs to finish 256kb chunk
            if (activeTransfers < 2 || FastestUploadSpeed - 5 * activeTransfers > 10)
            {
                next.Transfers.NeedUploadWeight = 0; // do here so that if core is crashed/throwing exceptions - other cores can still u/l

                next.RunInCoreAsync(() => next.Transfers.StartUpload());
            }
        }

        internal void AddCore(OpCore core)
        {
            Cores.SafeAdd(core);

            CheckLookup();
        }

        internal void RemoveCore(OpCore removed)
        {
            if (removed == Lookup)
                return;

            Cores.LockWriting(delegate()
            {
                foreach (OpCore core in Cores)
                    if (core == removed)
                    {
                        Cores.Remove(core);
                        break;
                    }
            });

            CheckLookup();
        }

        internal void CheckLookup()
        {
            // called from gui thread

            bool runLookup = false;

            Cores.LockReading(delegate()
            {
                foreach (OpCore core in Cores)
                    if (core.User.Settings.OpAccess != AccessType.Secret ||
                        core.User.Settings.GlobalIM)
                        runLookup = true;

                // if public cores exist, sign into global
                if (runLookup && Lookup == null)
                {
                    Lookup = new OpCore(this);
                }

                // else destroy global context
                if (!runLookup && Lookup != null)
                {
                    Lookup.Exit();
                    Lookup = null;
                }
            });
        }

        internal bool CanUpdate()
        {
            if (SignedUpdate == null)
                return false; // nothing to update with

            return (SignedUpdate.Loaded && SignedUpdate.SequentialVersion > LocalSeqVersion);
        }

        public void RaiseLogin(string[] args)
        {
            if (ShowLogin != null)
                ShowLogin(args);
        }

        public void RaiseUpdateReady(LookupSettings config)
        {
            if (NotifyUpdateReady != null)
                NotifyUpdateReady(config);
        }
    }
}

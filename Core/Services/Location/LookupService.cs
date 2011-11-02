using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Transport;

using DeOps.Services.Assist;


namespace DeOps.Services.Location
{
    public class LookupService : OpService 
    {
        public string Name { get { return "Lookup"; } }
        public uint ServiceID { get { return (uint)ServiceIDs.Lookup; } }

        OpCore Core;
        DhtNetwork Network;

        public TempCache LookupCache;
        const uint DataTypeCache= 0x01;



        public LookupService(OpCore core)
        {
            Core = core;
            Network = core.Network;


            LookupCache = new TempCache(Network, ServiceID, DataTypeCache);

            // specify time out
            // specify how many results to send back
            // send back newest results?
            // put ttl 
        }

        public void Dispose()
        {

        }

        public void SimTest()
        {
        }

        public void SimCleanup()
        {
        }


    }
}

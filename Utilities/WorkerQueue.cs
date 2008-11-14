using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;


namespace RiseOp.Utility
{
    internal class WorkerQueue : IDisposable 
    {
        Thread WorkerHandle;
        internal LinkedList<Tuple<Action, object>> Pending = new LinkedList<Tuple<Action, object>>();

        bool KillThread;

        string Name = "WorkerQueue";

        internal WorkerQueue(string name)
        {
            Name = name;
        }

        public void Dispose()
        {
            KillThread = true;

            if (WorkerHandle != null)
                Debug.Assert(WorkerHandle.Join(5000));
        }

        internal void Enqueue(Action action)
        {
            Enqueue(action, null);
        }

        internal void Enqueue(Action action, object arg)
        {
            // enqueue file for processing
            lock (Pending)
                Pending.AddLast(new Tuple<Action, object>(action, arg));

            // hashing
            if (WorkerHandle == null || !WorkerHandle.IsAlive)
            {
                WorkerHandle = new Thread(Worker);
                WorkerHandle.Start();
            }  
        }

        void Worker()
        {
            Tuple<Action, object> next = null;

            // while files on processing list
            while (Pending.Count > 0 && !KillThread)
            {
                lock (WorkerHandle)
                    next = Pending.First.Value;

                try
                {
                    next.Param1.Invoke();
                }
                catch { }

                lock (WorkerHandle) // dont remove right away so pending count is accurate while processing
                    Pending.RemoveFirst();
            }

            WorkerHandle = null;
        }

    }
}

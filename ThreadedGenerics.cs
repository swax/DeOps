using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace RiseOp
{
    internal class ThreadedDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        // default functions accessible but lock checked when accessed

        // special safe overrides provided for common functions

        internal ReaderWriterLock Access = new ReaderWriterLock();

        //LockCookie Cookie;

        #region Overrides

        internal new TValue this[TKey key]
        {
            get
            {
                Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

                return base[key];
            }
            set
            {
                Debug.Assert(Access.IsWriterLockHeld);

                base[key] = value;
            }
        }

        internal new Dictionary<TKey, TValue>.KeyCollection Keys
        {
            get
            {
                Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

                return base.Keys;
            }
        }

        internal new Dictionary<TKey, TValue>.ValueCollection Values
        {
            get
            {
                Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

                return base.Values;
            }
        }

        internal new int Count
        {
            get
            {
                Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

                return base.Count;
            }
        }

        internal new bool ContainsKey(TKey key)
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.ContainsKey(key);
        }

        internal new void Add(TKey key, TValue value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Add(key, value);
        }

        internal new void Remove(TKey key)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Remove(key);
        }

        internal new void Clear()
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Clear();
        }

        #endregion

        #region CustomOps

        /*internal void ToWriteLock()
        {
            Cookie = Access.UpgradeToWriterLock(-1);
        }

        internal void ToReadLock()
        {
            Access.DowngradeFromWriterLock(ref Cookie);
        }*/

        internal delegate void VoidType();

        internal void LockReading(VoidType code)
        {
            Access.AcquireReaderLock(-1);
            try
            {
                code();
            }
            finally { Access.ReleaseReaderLock(); }
        }

        internal void LockWriting(VoidType code)
        {
            if (Access.IsReaderLockHeld)
            {
                LockCookie cookie = Access.UpgradeToWriterLock(-1);
                try
                {
                    code();
                }
                finally { Access.DowngradeFromWriterLock(ref cookie); }
            }

            else
            {
                Access.AcquireWriterLock(-1);
                try
                {
                    code();
                }
                finally { Access.ReleaseWriterLock(); }
            }
        }

        internal delegate bool MatchType(TValue value);

        internal void RemoveWhere(MatchType isMatch)
        {
            List<TKey> removeKeys = new List<TKey>();

            LockReading(delegate()
            {
                foreach (KeyValuePair<TKey, TValue> pair in this)
                    if (isMatch(pair.Value))
                        removeKeys.Add(pair.Key);
            });

            if (removeKeys.Count > 0)
                LockWriting(delegate()
                {
                    foreach (TKey id in removeKeys)
                        Remove(id);
                });
        }

        internal void SafeAdd(TKey key, TValue value)
        {
            LockWriting(delegate()
            {
                base[key] = value;
            });
        }

        internal bool SafeTryGetValue(TKey key, out TValue value)
        {
            // cant pass out through lockreading anonymous delegate
            Access.AcquireReaderLock(-1);
            try
            {
                return base.TryGetValue(key, out value);
            }
            finally { Access.ReleaseReaderLock(); }

        }

        internal bool SafeContainsKey(TKey key)
        {
            Access.AcquireReaderLock(-1);
            try
            {
                return base.ContainsKey(key);
            }
            finally { Access.ReleaseReaderLock(); }
        }


        internal void SafeRemove(TKey key)
        {
            LockWriting(delegate()
            {
                base.Remove(key);
            });
        }


        internal int SafeCount
        {
            get
            {
                Access.AcquireReaderLock(-1);
                try
                {
                    return base.Count;
                }
                finally { Access.ReleaseReaderLock(); }

            }
        }

        internal void SafeClear()
        {
            LockWriting(delegate()
            {
                base.Clear();
            });
        }

        #endregion

    }

    internal class ThreadedList<T> : List<T>
    {
        internal ReaderWriterLock Access = new ReaderWriterLock();

        internal delegate void VoidType();


        internal new int Count
        {

            get
            {
                Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

                return base.Count;
            }
        }

        public new List<T>.Enumerator GetEnumerator()
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.GetEnumerator();
        }

        internal new bool Contains(T value)
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.Contains(value);
        }

        internal new void Add(T value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Add(value);
        }

        internal new void Remove(T value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Remove(value);
        }

        internal new void Clear()
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Clear();
        }

        internal void LockReading(VoidType code)
        {
            Access.AcquireReaderLock(-1);
            try
            {
                code();
            }
            finally { Access.ReleaseReaderLock(); }
        }

        internal void LockWriting(VoidType code)
        {
            if (Access.IsReaderLockHeld)
            {
                LockCookie cookie = Access.UpgradeToWriterLock(-1);
                try
                {
                    code();
                }
                finally { Access.DowngradeFromWriterLock(ref cookie); }
            }

            else
            {
                Access.AcquireWriterLock(-1);
                try
                {
                    code();
                }
                finally { Access.ReleaseWriterLock(); }
            }
        }

        internal void SafeAdd(T value)
        {
            LockWriting(delegate()
            {
                base.Add(value);
            });
        }

        internal void SafeRemove(T value)
        {
            LockWriting(delegate()
            {
                base.Remove(value);
            });
        }

        internal bool SafeContains(T value)
        {
            Access.AcquireReaderLock(-1);
            try
            {
                return base.Contains(value);
            }
            finally { Access.ReleaseReaderLock(); }
        }

        internal int SafeCount
        {
            get
            {
                Access.AcquireReaderLock(-1);
                try
                {
                    return base.Count;
                }
                finally { Access.ReleaseReaderLock(); }
            }
        }

        internal void SafeClear()
        {
            LockWriting(delegate()
            {
                base.Clear();
            });
        }
    }

    internal class ThreadedSortedList<TKey, TValue> : SortedList<TKey, TValue>
    {
        internal ReaderWriterLock Access = new ReaderWriterLock();

        internal delegate void VoidType();


        internal new int Count
        {

            get
            {
                Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

                return base.Count;
            }
        }

        public new IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.GetEnumerator();
        }

        public new IList<TKey> Keys
        {
            get
            {
                Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

                return base.Keys;
            }
        }

        public new IList<TValue> Values
        {
            get
            {
                Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

                return base.Values;
            }
        }

        internal new bool ContainsKey(TKey key)
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.ContainsKey(key);
        }

        internal new bool ContainsValue(TValue value)
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.ContainsValue(value);
        }

        internal new void Add(TKey key, TValue value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Add(key, value);
        }

        internal new void Remove(TKey key)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Remove(key);
        }

        internal new int IndexOfValue(TValue value)
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.IndexOfValue(value);
        }

        internal new void RemoveAt(int index)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.RemoveAt(index);
        }

        internal new void Clear()
        {
            Debug.Assert(Access.IsWriterLockHeld);
            base.Clear();
        }

        internal void LockReading(VoidType code)
        {
            Access.AcquireReaderLock(-1);
            try
            {
                code();
            }
            finally { Access.ReleaseReaderLock(); }
        }

        internal void LockWriting(VoidType code)
        {
            if (Access.IsReaderLockHeld)
            {
                LockCookie cookie = Access.UpgradeToWriterLock(-1);
                try
                {
                    code();
                }
                finally { Access.DowngradeFromWriterLock(ref cookie); }
            }

            else
            {
                Access.AcquireWriterLock(-1);
                try
                {
                    code();
                }
                finally { Access.ReleaseWriterLock(); }
            }
        }

        internal void SafeAdd(TKey key, TValue value)
        {
            LockWriting(delegate()
            {
                base.Add(key, value);
            });
        }

        internal void SafeRemove(TKey key)
        {
            LockWriting(delegate()
            {
                base.Remove(key);
            });
        }

        internal bool SafeContainsKey(TKey key)
        {
            Access.AcquireReaderLock(-1);
            try
            {
                return base.ContainsKey(key);
            }
            finally { Access.ReleaseReaderLock(); }
        }

        internal bool SafeContainsValue(TValue value)
        {
            Access.AcquireReaderLock(-1);
            try
            {
                return base.ContainsValue(value);
            }
            finally { Access.ReleaseReaderLock(); }
        }

        internal int SafeCount
        {
            get
            {
                Access.AcquireReaderLock(-1);
                try
                {
                    return base.Count;
                }
                finally { Access.ReleaseReaderLock(); }
            }
        }

        internal void SafeClear()
        {
            LockWriting(delegate()
            {
                base.Clear();
            });
        }
    }

    internal class ThreadedLinkedList<T> : LinkedList<T>
    {
        internal ReaderWriterLock Access = new ReaderWriterLock();

        internal delegate void VoidType();


        public new LinkedList<T>.Enumerator GetEnumerator()
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.GetEnumerator();
        }

        internal new void AddAfter(LinkedListNode<T> node, T value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.AddAfter(node, value);
        }

        internal new void AddAfter(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.AddAfter(node, newNode);
        }

        internal new void AddBefore(LinkedListNode<T> node, T value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.AddBefore(node, value);
        }

        internal new void AddBefore(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.AddBefore(node, newNode);
        }

        internal new void AddFirst(T value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.AddFirst(value);
        }

        internal new void AddFirst(LinkedListNode<T> node)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.AddFirst(node);
        }

        internal new void AddLast(T value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.AddLast(value);
        }

        internal new void AddLast(LinkedListNode<T> node)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.AddLast(node);
        }

        internal new void Clear()
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Clear();
        }

        internal new bool Contains(T value)
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.Contains(value);
        }

        internal new int Count
        {
            get
            {
                Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

                return base.Count;
            }
        }

        internal new bool Remove(T value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            return base.Remove(value);
        }

        internal new void Remove(LinkedListNode<T> node)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Remove(node);
        }

        internal new void RemoveFirst()
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.RemoveFirst();
        }

        internal new void RemoveLast()
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.RemoveLast();
        }

        internal new LinkedListNode<T> First
        {
            get
            {
                Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

                return base.First;
            }
        }

        internal new LinkedListNode<T> Last
        {
            get
            {
                Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

                return base.Last;
            }
        }

        internal new LinkedListNode<T> Find(T value)
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.Find(value);
        }

        internal new LinkedListNode<T> FindLast(T value)
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.FindLast(value);
        }




        internal void LockReading(VoidType code)
        {
            Access.AcquireReaderLock(-1);
            try
            {
                code();
            }
            finally { Access.ReleaseReaderLock(); }
        }

        internal void LockWriting(VoidType code)
        {
            if (Access.IsReaderLockHeld)
            {
                LockCookie cookie = Access.UpgradeToWriterLock(-1);
                try
                {
                    code();
                }
                finally { Access.DowngradeFromWriterLock(ref cookie); }
            }

            else
            {
                Access.AcquireWriterLock(-1);
                try
                {
                    code();
                }
                finally { Access.ReleaseWriterLock(); }
            }
        }

        internal void SafeAddFirst(T value)
        {
            LockWriting(delegate()
            {
                base.AddFirst(value);
            });
        }

        internal void SafeAddLast(T value)
        {
            LockWriting(delegate()
            {
                base.AddLast(value);
            });
        }

        internal void SafeAddAfter(LinkedListNode<T> node, T value)
        {
            LockWriting(delegate()
            {
                base.AddAfter(node, value);
            });
        }

        internal void SafeAddBefore(LinkedListNode<T> node, T value)
        {
            LockWriting(delegate()
            {
                base.AddBefore(node, value);
            });
        }


        internal void SafeRemove(T value)
        {
            LockWriting(delegate()
            {
                base.Remove(value);
            });
        }

        internal void SafeRemoveFirst()
        {
            LockWriting(delegate()
            {
                base.RemoveFirst();
            });
        }

        internal void SafeRemoveLast()
        {
            LockWriting(delegate()
            {
                base.RemoveLast();
            });
        }

        /* internal bool SafeContains(T value)
         {
             Access.AcquireReaderLock(-1);
             try
             {
                 return base.Contains(value);
             }
             finally { Access.ReleaseReaderLock(); }
         }*/

        internal int SafeCount
        {
            get
            {
                Access.AcquireReaderLock(-1);
                try
                {
                    return base.Count;
                }
                finally { Access.ReleaseReaderLock(); }
            }
        }

        internal void SafeClear()
        {
            LockWriting(delegate()
            {
                base.Clear();
            });
        }

        internal LinkedListNode<T> SafeFirst
        {
            get
            {
                Access.AcquireReaderLock(-1);
                try
                {
                    return base.First;
                }
                finally { Access.ReleaseReaderLock(); }
            }
        }

        internal LinkedListNode<T> SafeLast
        {
            get
            {
                Access.AcquireReaderLock(-1);
                try
                {
                    return base.Last;
                }
                finally { Access.ReleaseReaderLock(); }
            }
        }
    }
}

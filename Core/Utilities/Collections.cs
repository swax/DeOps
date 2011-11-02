using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace DeOps
{
    public static class GenericExtensions
    {
        public static void ForEach<T>(this T[] array, Action<T> code)
        {
            foreach (T item in array)
                code.Invoke(item);
        }
    }

    public class ThreadedDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        // default functions accessible but lock checked when accessed

        // special safe overrides provided for common functions

        public ReaderWriterLock Access = new ReaderWriterLock();

        //LockCookie Cookie;

        #region Overrides

        public new TValue this[TKey key]
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

        public new Dictionary<TKey, TValue>.KeyCollection Keys
        {
            get
            {
                Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

                return base.Keys;
            }
        }

        public new Dictionary<TKey, TValue>.ValueCollection Values
        {
            get
            {
                Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

                return base.Values;
            }
        }

        public new int Count
        {
            get
            {
                Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

                return base.Count;
            }
        }

        public new bool ContainsKey(TKey key)
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.ContainsKey(key);
        }

        public new void Add(TKey key, TValue value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Add(key, value);
        }

        public new void Remove(TKey key)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Remove(key);
        }

        public new void Clear()
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Clear();
        }

        #endregion

        #region CustomOps

        /*public void ToWriteLock()
        {
            Cookie = Access.UpgradeToWriterLock(-1);
        }

        public void ToReadLock()
        {
            Access.DowngradeFromWriterLock(ref Cookie);
        }*/

        public delegate void VoidType();

        public void LockReading(VoidType code)
        {
            Access.AcquireReaderLock(-1);
            try
            {
                code();
            }
            finally { Access.ReleaseReaderLock(); }
        }

        public void LockWriting(VoidType code)
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

        public delegate bool MatchType(TValue value);

        public void RemoveWhere(MatchType isMatch)
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

        public void SafeAdd(TKey key, TValue value)
        {
            LockWriting(delegate()
            {
                base[key] = value;
            });
        }

        public bool SafeTryGetValue(TKey key, out TValue value)
        {
            // cant pass out through lockreading anonymous delegate
            Access.AcquireReaderLock(-1);
            try
            {
                return base.TryGetValue(key, out value);
            }
            finally { Access.ReleaseReaderLock(); }

        }

        public bool SafeContainsKey(TKey key)
        {
            Access.AcquireReaderLock(-1);
            try
            {
                return base.ContainsKey(key);
            }
            finally { Access.ReleaseReaderLock(); }
        }


        public void SafeRemove(TKey key)
        {
            LockWriting(delegate()
            {
                base.Remove(key);
            });
        }


        public int SafeCount
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

        public void SafeClear()
        {
            LockWriting(delegate()
            {
                base.Clear();
            });
        }

        #endregion

    }

    public class ThreadedList<T> : List<T>
    {
        public ReaderWriterLock Access = new ReaderWriterLock();

        public delegate void VoidType();


        public new int Count
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

        public new bool Contains(T value)
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.Contains(value);
        }

        public new void Add(T value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Add(value);
        }

        public new void Remove(T value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Remove(value);
        }

        public new void Clear()
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Clear();
        }

        public void LockReading(VoidType code)
        {
            Access.AcquireReaderLock(-1);
            try
            {
                code();
            }
            finally { Access.ReleaseReaderLock(); }
        }

        public void SafeForEach(Action<T> action)
        {
            LockReading(() =>
            {
                foreach (T item in this)
                    action.Invoke(item);
            });
        }

        public void LockWriting(VoidType code)
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

        public void SafeAdd(T value)
        {
            LockWriting(delegate()
            {
                base.Add(value);
            });
        }

        public void SafeRemove(T value)
        {
            LockWriting(delegate()
            {
                base.Remove(value);
            });
        }

        public bool SafeContains(T value)
        {
            Access.AcquireReaderLock(-1);
            try
            {
                return base.Contains(value);
            }
            finally { Access.ReleaseReaderLock(); }
        }

        public int SafeCount
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

        public void SafeClear()
        {
            LockWriting(delegate()
            {
                base.Clear();
            });
        }
    }

    public class ThreadedSortedList<TKey, TValue> : SortedList<TKey, TValue>
    {
        public ReaderWriterLock Access = new ReaderWriterLock();

        public delegate void VoidType();


        public new int Count
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

        public new bool ContainsKey(TKey key)
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.ContainsKey(key);
        }

        public new bool ContainsValue(TValue value)
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.ContainsValue(value);
        }

        public new void Add(TKey key, TValue value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Add(key, value);
        }

        public new void Remove(TKey key)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Remove(key);
        }

        public new int IndexOfValue(TValue value)
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.IndexOfValue(value);
        }

        public new void RemoveAt(int index)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.RemoveAt(index);
        }

        public new void Clear()
        {
            Debug.Assert(Access.IsWriterLockHeld);
            base.Clear();
        }

        public void LockReading(VoidType code)
        {
            Access.AcquireReaderLock(-1);
            try
            {
                code();
            }
            finally { Access.ReleaseReaderLock(); }
        }

        public void LockWriting(VoidType code)
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

        public void SafeAdd(TKey key, TValue value)
        {
            LockWriting(delegate()
            {
                base.Add(key, value);
            });
        }

        public void SafeRemove(TKey key)
        {
            LockWriting(delegate()
            {
                base.Remove(key);
            });
        }

        public bool SafeContainsKey(TKey key)
        {
            Access.AcquireReaderLock(-1);
            try
            {
                return base.ContainsKey(key);
            }
            finally { Access.ReleaseReaderLock(); }
        }

        public bool SafeContainsValue(TValue value)
        {
            Access.AcquireReaderLock(-1);
            try
            {
                return base.ContainsValue(value);
            }
            finally { Access.ReleaseReaderLock(); }
        }

        public int SafeCount
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

        public void SafeClear()
        {
            LockWriting(delegate()
            {
                base.Clear();
            });
        }
    }

    public class ThreadedLinkedList<T> : LinkedList<T>
    {
        public ReaderWriterLock Access = new ReaderWriterLock();

        public delegate void VoidType();


        public new LinkedList<T>.Enumerator GetEnumerator()
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.GetEnumerator();
        }

        public new void AddAfter(LinkedListNode<T> node, T value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.AddAfter(node, value);
        }

        public new void AddAfter(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.AddAfter(node, newNode);
        }

        public new void AddBefore(LinkedListNode<T> node, T value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.AddBefore(node, value);
        }

        public new void AddBefore(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.AddBefore(node, newNode);
        }

        public new void AddFirst(T value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.AddFirst(value);
        }

        public new void AddFirst(LinkedListNode<T> node)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.AddFirst(node);
        }

        public new void AddLast(T value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.AddLast(value);
        }

        public new void AddLast(LinkedListNode<T> node)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.AddLast(node);
        }

        public new void Clear()
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Clear();
        }

        public new bool Contains(T value)
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.Contains(value);
        }

        public new int Count
        {
            get
            {
                Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

                return base.Count;
            }
        }

        public new bool Remove(T value)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            return base.Remove(value);
        }

        public new void Remove(LinkedListNode<T> node)
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.Remove(node);
        }

        public new void RemoveFirst()
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.RemoveFirst();
        }

        public new void RemoveLast()
        {
            Debug.Assert(Access.IsWriterLockHeld);

            base.RemoveLast();
        }

        public new LinkedListNode<T> First
        {
            get
            {
                Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

                return base.First;
            }
        }

        public new LinkedListNode<T> Last
        {
            get
            {
                Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

                return base.Last;
            }
        }

        public new LinkedListNode<T> Find(T value)
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.Find(value);
        }

        public new LinkedListNode<T> FindLast(T value)
        {
            Debug.Assert(Access.IsReaderLockHeld || Access.IsWriterLockHeld);

            return base.FindLast(value);
        }




        public void LockReading(VoidType code)
        {
            Access.AcquireReaderLock(-1);
            try
            {
                code();
            }
            finally { Access.ReleaseReaderLock(); }
        }

        public void LockWriting(VoidType code)
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

        public void SafeAddFirst(T value)
        {
            LockWriting(delegate()
            {
                base.AddFirst(value);
            });
        }

        public void SafeAddLast(T value)
        {
            LockWriting(delegate()
            {
                base.AddLast(value);
            });
        }

        public void SafeAddAfter(LinkedListNode<T> node, T value)
        {
            LockWriting(delegate()
            {
                base.AddAfter(node, value);
            });
        }

        public void SafeAddBefore(LinkedListNode<T> node, T value)
        {
            LockWriting(delegate()
            {
                base.AddBefore(node, value);
            });
        }


        public void SafeRemove(T value)
        {
            LockWriting(delegate()
            {
                base.Remove(value);
            });
        }

        public void SafeRemoveFirst()
        {
            LockWriting(delegate()
            {
                base.RemoveFirst();
            });
        }

        public void SafeRemoveLast()
        {
            LockWriting(delegate()
            {
                base.RemoveLast();
            });
        }

        /* public bool SafeContains(T value)
         {
             Access.AcquireReaderLock(-1);
             try
             {
                 return base.Contains(value);
             }
             finally { Access.ReleaseReaderLock(); }
         }*/

        public int SafeCount
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

        public void SafeClear()
        {
            LockWriting(delegate()
            {
                base.Clear();
            });
        }

        public LinkedListNode<T> SafeFirst
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

        public LinkedListNode<T> SafeLast
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

    public class Tuple<T1>
    {
        public T1 Param1;

        public Tuple(T1 t1)
        {
            Param1 = t1;
        }

        public override string ToString()
        {
            return Param1.ToString();
        }

        public override int GetHashCode()
        {
            return Param1.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            Tuple<T1> tuple = obj as Tuple<T1>;

            return Param1.Equals(tuple.Param1);
        }
    }

    public class Tuple<T1, T2> : Tuple<T1>
    {
        public T2 Param2;

        public Tuple(T1 t1, T2 t2)
            : base(t1)
        {
            Param2 = t2;
        }

        public override string ToString()
        {
            return base.ToString() + " - " + Param2.ToString();
        }

        public override int GetHashCode()
        {
            return Param2.GetHashCode() ^ base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            Tuple<T1, T2> tuple = obj as Tuple<T1, T2>;

            return Param2.Equals(tuple.Param2) && base.Equals(obj);
        }
    }

    public class Tuple<T1, T2, T3> : Tuple<T1, T2>
    {
        public T3 Param3;

        public Tuple(T1 t1, T2 t2, T3 t3)
            : base(t1, t2)
        {
            Param3 = t3;
        }


        public override string ToString()
        {
            return base.ToString() + " - " + Param3.ToString();
        }

        public override int GetHashCode()
        {
            return Param3.GetHashCode() ^ base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            Tuple<T1, T2, T3> tuple = obj as Tuple<T1, T2, T3>;

            return Param3.Equals(tuple.Param3) && base.Equals(obj);
        }
    }

    public class CircularBuffer<T> : IEnumerable<T>
    {
        public T[] Buffer;
        public int CurrentPos = -1;
        public int Length;

        public int Capacity
        {
            set
            {
                // copy prev elements
                T[] copy = new T[Length];

                for (int i = 0; i < Length && i < value; i++)
                    copy[i] = this[i];

                // re-init buff
                Buffer = new T[value];
                CurrentPos = -1;
                Length = 0;

                // add back values
                Array.Reverse(copy);
                foreach (T init in copy)
                    Add(init);
            }
            get
            {
                return Buffer.Length;
            }
        }


        public CircularBuffer(int capacity)
        {
            Capacity = capacity;
        }

        public T this[int index]
        {
            get
            {
                return Buffer[ToCircleIndex(index)];
            }
            set
            {
                Buffer[ToCircleIndex(index)] = value;
            }
        }

        int ToCircleIndex(int index)
        {
            // linear index to circular index

            if (CurrentPos == -1)
                throw new Exception("Index value not valid");

            if (index >= Length)
                throw new Exception("Index value exceeds bounds of array");

            int circIndex = CurrentPos - index;

            if (circIndex < 0)
                circIndex = Buffer.Length + circIndex;

            return circIndex;
        }

        public void Add(T value)
        {
            if (Buffer == null || Buffer.Length == 0)
                return;

            CurrentPos++;

            // circle around
            if (CurrentPos >= Buffer.Length)
                CurrentPos = 0;

            Buffer[CurrentPos] = value;

            if (Length <= CurrentPos)
                Length = CurrentPos + 1;
        }


        public void Clear()
        {
            Buffer = new T[Capacity];
            CurrentPos = -1;
            Length = 0;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetNext();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetNext();
        }

        private IEnumerator<T> GetNext()
        {
            if (CurrentPos == -1)
                yield break;

            // iterate from most recent to beginning
            for (int i = CurrentPos; i >= 0; i--)
                yield return Buffer[i];

            // iterate the back down
            if (Length == Buffer.Length)
                for (int i = Length - 1; i > CurrentPos; i--)
                    yield return Buffer[i];
        }

    }
}

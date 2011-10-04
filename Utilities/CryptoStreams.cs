using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using DeOps.Implementation.Protocol;

namespace DeOps.Implementation
{
    // used to store sub-hashes for files that are transferred over the network
    internal delegate void ProcessTagsHandler(PacketStream stream);

    internal class IVCryptoStream : CryptoStream
    {

        IVCryptoStream(Stream stream, ICryptoTransform transform, CryptoStreamMode mode)
            : base(stream, transform, mode)
        {

        }

        // this class saves the IV at the beginning of the file and loads it again during reading
        internal static IVCryptoStream Load(string path, byte[] key)
        {
            FileStream file = File.OpenRead(path);

            return IVCryptoStream.Load(file, key);
        }

        internal static IVCryptoStream Load(Stream stream, byte[] key)
        {
            // already disposed by called if fails
            byte[] iv = new byte[16];
            stream.Read(iv, 0, 16);

            RijndaelManaged crypt = new RijndaelManaged();
            crypt.Key = key;
            crypt.IV = iv;

            return new IVCryptoStream(stream, crypt.CreateDecryptor(), CryptoStreamMode.Read);
        }

        internal static IVCryptoStream Save(string path, byte[] key)
        {
            return Save(path, key, null);
        }

        internal static IVCryptoStream Save(string path, byte[] key, byte[] iv)
        {
            FileStream file = new FileStream(path, FileMode.Create);

            RijndaelManaged crypt = new RijndaelManaged();
            crypt.Key = key;

            if (iv == null)
                crypt.GenerateIV();
            else
            {
                Debug.Assert(iv.Length == crypt.IV.Length);
                crypt.IV = iv;
            }

            try
            {
                file.Write(crypt.IV, 0, crypt.IV.Length);
            }
            catch
            {
                file.Dispose();
            }

            return new IVCryptoStream(file, crypt.CreateEncryptor(), CryptoStreamMode.Write);

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (CanRead)
                    ReadtoEnd(this);
            }

            base.Dispose(disposing);
        }

        void ReadtoEnd(Stream stream)
        {
            //crit bug in crypto stream, cant open file read part of it and close
            // doing so would cause an "padding is invalid and cannot be removed" error
            // only solution is that when reading crypto we must read to end all the time so that Close() wont fail

            byte[] buffer = new byte[4096];

            while (stream.Read(buffer, 0, 4096) == 4096)
                ;
        }
    }


    internal class TaggedStream : FileStream
    {
        long InternalSize;


        internal TaggedStream(string path, G2Protocol protocol)
            : base(path, FileMode.Open, FileAccess.Read, FileShare.Read)
        {
            Init(path, protocol, null);
        }

        internal TaggedStream(string path, G2Protocol protocol, ProcessTagsHandler processTags)
            : base(path, FileMode.Open, FileAccess.Read, FileShare.Read)
        {
            Init(path, protocol, processTags);
        }

        void Init(string path, G2Protocol protocol, ProcessTagsHandler processTags)
        {
            Seek(-8, SeekOrigin.End);

            byte[] sizeBytes = new byte[8];
            Read(sizeBytes, 0, sizeBytes.Length);
            long fileSize = BitConverter.ToInt64(sizeBytes, 0);

            if (processTags != null)
            {
                // read internal packets
                Seek(fileSize, SeekOrigin.Begin);

                PacketStream stream = new PacketStream(this, protocol, FileAccess.Read);

                // dont need to close packetStream
                processTags.Invoke(stream);
            }

            // set internal size down here so reading packet stream works without mixing up true file lenght
            InternalSize = fileSize;

            // set back to the beginning of the file
            Seek(0, SeekOrigin.Begin);
        }

        public override long Length
        {
            get
            {
                return (InternalSize == 0) ? base.Length : InternalSize;
            }
        }

        public override int Read(byte[] array, int offset, int count)
        {
            if (Position + count > Length)
                count = (int)(Length - Position);

            return base.Read(array, offset, count);
        }
    }

}

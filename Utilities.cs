using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Text;
using System.Threading;
using System.IO;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Interface.TLVex;


namespace RiseOp
{

	internal static class Utilities
	{  
        internal static byte[] GetPasswordKey(string password, byte[] salt)
        {
            // to prevent rainbow attack salt needs to be hashed with password
            byte[] textBytes = UTF8Encoding.UTF8.GetBytes(password);
            byte[] passBytes = new byte[salt.Length + textBytes.Length];
            salt.CopyTo(passBytes, 0);
            textBytes.CopyTo(passBytes, salt.Length);

            SHA256Managed sha256 = new SHA256Managed();
            for (int i = 0; i < 25; i++)
                passBytes = sha256.ComputeHash(passBytes);

            return passBytes;
        }

        internal static bool MemCompare(byte[] a, byte[] b)
        {
            if (a == null && b == null)
                return true;

            if (a == null || b == null)
                return false;

            if (a.Length != b.Length)
                return false;

            return MemCompare(a, 0, b, 0, a.Length);
        }

		internal static bool MemCompare(byte[] aBuff, int aOffset, byte[] bBuff, int bOffset, int count)
		{
			for(int i = 0, aPos = aOffset, bPos = bOffset; i < count; i++, aPos++, bPos++)
				if(aBuff[aPos] != bBuff[bPos])
					return false;

			return true;
		}

		internal static int GetBit(int bits, int pos)
		{
			return (((1 << pos) & bits) >= 1) ? 1 : 0;
		}

		internal static int GetBit(UInt64 bits, int pos)
		{
            pos = 63 - pos;
			return (((((UInt64) 1) << pos ) & bits) > 0) ? 1 : 0;
		}

		internal static void SetBit(ref UInt64 bits, int pos, int val)
		{
            pos = 63 - pos;

			if(val == 0)
				bits &= ~(((UInt64) 1) << pos);
			else
				bits |= ((UInt64) 1) << pos; 
		}

		internal static string IDtoBin(UInt64 id)
		{
			string bin = "";

			for(int i = 0; i < 12; i++)
				if((id & ((UInt64)1 << 63 - i)) > 0)
					bin += "1";
				else
					bin += "0";

			return bin;
		}

        internal static ulong RandUInt64(Random rnd)
        {
            byte[] bytes = new byte[8];

            rnd.NextBytes(bytes);

            return BitConverter.ToUInt64(bytes, 0);
        }

        internal static ulong StrongRandUInt64(RNGCryptoServiceProvider rnd)
        {
            byte[] bytes = new byte[8];
            
            rnd.GetBytes(bytes);

            return BitConverter.ToUInt64(bytes, 0);
        }

		internal static byte[] ExtractBytes(byte[] buffer, int offset, int length)
		{
			byte[] extracted = new byte[length];

			Buffer.BlockCopy(buffer, offset, extracted, 0, length);

			return extracted;
		}

		internal static string BytestoHex(byte[] data)
		{
			return Utilities.BytestoHex(data, 0, data.Length, false);
		}

		internal static string BytestoHex(byte[] data, int offset, int size, bool space)
		{
			StringBuilder hex = new StringBuilder();

			for(int i = offset; i < offset + size; i++)
			{
				hex.Append( String.Format("{0:x2}", data[i]) );

				if(space)
					hex.Append(" ");
			}

			return hex.ToString();
		}

		internal static byte[] HextoBytes(string hex)
		{
			if(hex.Length % 2 != 0)
				return null;

			byte[] bin = new byte[hex.Length / 2];

			hex = hex.ToUpper();

			for(int i = 0; i < hex.Length; i++)
			{
				int val = hex[i];
				val -= 48; // 0 - 9

				if(val > 9) // A - F
					val -= 7;

				if(val > 15) // invalid char read
					return null;
					
				if(i % 2 == 0)
					bin[i/2] = (byte) (val << 4);
				else
					bin[(i-1)/2] |= (byte) val;
			}

			return bin;
		}

		internal static string BytestoAscii(byte[] data, int offset, int size)
		{
			StringBuilder ascii = new StringBuilder();

			for(int i = offset; i < offset + size; i++)
				if(data[i] >= 33 && data[i] <= 126)
					ascii.Append(" " + (char) data[i] + " ");
				else
					ascii.Append(" . ");

			return ascii.ToString();
		}	

        internal static UInt64 KeytoID(RSAParameters pubParams)
        {
            return Utilities.KeytoID(pubParams.Modulus);
        }

		internal static UInt64 KeytoID(byte[] key)
		{
			SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider();

            byte[] pubHash = sha.ComputeHash(key);

			return BitConverter.ToUInt64(pubHash, 0); // first 8 bytes of sha1 of internal key
		}

        internal static void ShaHashFile(string path, ref byte[] hash, ref long size)
        {
            FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider();
            hash = sha.ComputeHash(file);
            size = file.Length;

            file.Close();
        }

        internal static void HashTagFile(string path, ref byte[] hash, ref long size)
        {
            FileStream file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);

            // record size of file
            long originalSize = file.Length;

            // md5 hash 128k chunks of file
            SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider();
            
            int read = 0;
            byte chunkBits = 7; // 128kb chunks
            int chunkSize = (int) Math.Pow(2, chunkBits) * 1024;
            int buffSize = file.Length > chunkSize ? chunkSize : (int) file.Length;
            byte[] chunk = new byte[buffSize];
            List<byte[]> hashes = new List<byte[]>();

            read = 1;
            while (read > 0)
            {
                read = file.Read(chunk, 0, buffSize);

                if (read > 0)
                    hashes.Add(sha.ComputeHash(chunk, 0, read));
            }

            // attach chunk hashes to end of file
            foreach (byte[] chunkhash in hashes)
                file.Write(chunkhash, 0, chunkhash.Length);

            // attach the size of each chunk
            file.WriteByte(chunkBits);

            // attach original size to end of file
            byte[] sizeBytes = BitConverter.GetBytes(originalSize);
            file.Write(sizeBytes, 0, sizeBytes.Length);

            // sha1 hash tagged file
            file.Seek(0, SeekOrigin.Begin);
            hash = sha.ComputeHash(file);
            size = file.Length;

            file.Close();
        }

        internal static string CryptType(object crypt)
        {
            if (crypt.GetType() == typeof(RijndaelManaged))
            {
                RijndaelManaged key = (RijndaelManaged)crypt;

                return "aes " + key.KeySize.ToString();
            }

            if (crypt.GetType() == typeof(RSACryptoServiceProvider))
            {
                RSACryptoServiceProvider key = (RSACryptoServiceProvider)crypt;

                return "rsa " + key.KeySize;
            }

            throw new Exception("Unknown Encryption Type");
        }

        internal static bool CheckSignedData(byte[] key, byte[] data, byte[] sig)
        {
            // check signature
            RSACryptoServiceProvider rsa = Utilities.KeytoRsa(key);

            return rsa.VerifyData(data, new SHA1CryptoServiceProvider(), sig);
        }

        internal static RSACryptoServiceProvider KeytoRsa(byte[] key)
        {
            RSAParameters param = new RSAParameters();
            param.Modulus = key;
            param.Exponent = new byte[] { 1, 0, 1 };

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(param);
            
            return rsa;
        }



        internal static byte[] EncryptBytes(byte[] data, byte[] key)
        {
            RijndaelManaged crypt = new RijndaelManaged();
            crypt.Key = key;
            crypt.GenerateIV();
            crypt.Padding = PaddingMode.PKCS7;

            ICryptoTransform encryptor = crypt.CreateEncryptor();
            byte[] transformed = encryptor.TransformFinalBlock(data, 0, data.Length);

            byte[] final = new byte[crypt.IV.Length + transformed.Length];

            crypt.IV.CopyTo(final, 0);
            transformed.CopyTo(final, crypt.IV.Length);

            return final;
        }

        internal static byte[] DecryptBytes(byte[] data, int length, byte[] key)
        {
            RijndaelManaged crypt = new RijndaelManaged();
            crypt.Key     = key;
            crypt.IV      = Utilities.ExtractBytes(data, 0, crypt.IV.Length);
            crypt.Padding = PaddingMode.PKCS7;

            ICryptoTransform decryptor = crypt.CreateDecryptor();

            return decryptor.TransformFinalBlock(data, crypt.IV.Length, length - crypt.IV.Length);
        }

        internal static void InsertSubNode(TreeListNode parent, TreeListNode node)
        {
            int index = 0;

            foreach (TreeListNode entry in parent.Nodes)
                if (string.Compare(node.Text, entry.Text, true) < 0)
                {
                    parent.Nodes.Insert(index, node);
                    return;
                }
                else
                    index++;

            parent.Nodes.Insert(index, node);
        }

        internal static string CryptFilename(OpCore core, string name)
        {
            // hash, base64 name with ~ instead of /, use for link as well

            SHA1Managed sha1 = new SHA1Managed();
            byte[] hash = new SHA1Managed().ComputeHash(UTF8Encoding.UTF8.GetBytes(name));

            return Utilities.ToBase64String(hash);

            /*RijndaelManaged crypt = core.User.Settings.FileKey;

            UTF8Encoding converter = new UTF8Encoding();
            ICryptoTransform transform = crypt.CreateEncryptor();

            byte[] buffer = converter.GetBytes(name);
            return BytestoHex(transform.TransformFinalBlock(buffer, 0, buffer.Length));*/
        }

        internal static string CryptFilename(OpCore core, ulong id, byte[] hash)
        {
            byte[] buffer = new byte[8 + hash.Length];
            BitConverter.GetBytes(id).CopyTo(buffer, 0);
            hash.CopyTo(buffer, 8);

            SHA1Managed sha1 = new SHA1Managed();
            byte[] totalHash = new SHA1Managed().ComputeHash(hash);

            return Utilities.ToBase64String(totalHash);

            /*RijndaelManaged crypt = core.User.Settings.FileKey;

            // prevent nodes with same hash for file from overwriting, or even attacking each other

            byte[] buffer = new byte[8 + hash.Length];
            BitConverter.GetBytes(id).CopyTo(buffer, 0);
            hash.CopyTo(buffer, 8);

            ICryptoTransform transform = crypt.CreateEncryptor();

            return BytestoHex(transform.TransformFinalBlock(buffer, 0, buffer.Length));*/
        }

        internal static string ToBase64String(byte[] hash)
        {
            string base64 = Convert.ToBase64String(hash);

            return base64.Replace('/', '~');
        }

        internal static byte[] FromBase64String(string base64)
        {
            base64 = base64.Replace('~', '/');

            return Convert.FromBase64String(base64);
        }

        const long BytesInKilo = 1024;
        const long BytesInMega = 1024 * 1024;
        const long BytesInGiga = 1024 * 1024 * 1024;

        internal static string ByteSizetoString(long bytes)
        {
            if (bytes > BytesInGiga)
                return string.Format("{0} GB", bytes / BytesInGiga);

            if (bytes > BytesInMega)
                return string.Format("{0} MB", bytes / BytesInMega);

            if (bytes > BytesInKilo)
                return string.Format("{0} KB", bytes / BytesInKilo);

            return string.Format("{0} B", bytes);
        }

        internal static string ByteSizetoDecString(long bytes)
        {
            if (bytes > BytesInGiga)
                return string.Format("{0:#.00} GB", (double)bytes / (double)BytesInGiga);

            if (bytes > BytesInMega)
                return string.Format("{0:#.00} MB", (double)bytes / (double)BytesInMega);

            if (bytes > BytesInKilo)
                return string.Format("{0:#.00} KB", (double)bytes / (double)BytesInKilo);

            return string.Format("{0} B", bytes);
        }

        internal static void ReadtoEnd(Stream stream)
        {
            //crit bug in crypto stream, cant open file read part of it and close
            // doing so would cause an "padding is invalid and cannot be removed" error
            // only solution is that when reading crypto we must read to end all the time so that Close() wont fail

            byte[] buffer = new byte[4096];
            
            while (stream.Read(buffer, 0, 4096) == 4096)
                ;
        }

        static public System.Drawing.SizeF MeasureDisplayString(System.Drawing.Graphics graphics, string text, System.Drawing.Font font)
        {
            const int width = 32;

            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width, 1, graphics);
            System.Drawing.SizeF size = graphics.MeasureString(text, font);
            System.Drawing.Graphics anagra = System.Drawing.Graphics.FromImage(bitmap);

            int measured_width = (int)size.Width;

            if (anagra != null)
            {
                anagra.Clear(System.Drawing.Color.White);
                anagra.DrawString(text + "|", font, System.Drawing.Brushes.Black, width - measured_width, -font.Height / 2);

                for (int i = width - 1; i >= 0; i--)
                {
                    measured_width--;
                    if (bitmap.GetPixel(i, 0).R == 0)
                    {
                        break;
                    }
                }
            }

            return new System.Drawing.SizeF(measured_width, size.Height);
        }

        static public int MeasureDisplayStringWidth(System.Drawing.Graphics graphics, string text, System.Drawing.Font font)
        {
            return (int)MeasureDisplayString(graphics, text, font).Width;
        }

        internal static string FormatTime(DateTime time)
        {
            // convert from utc
            time = time.ToLocalTime();

            // Thu 4/5/2006 at 4:59pm

            string formatted = time.ToString("ddd M/d/yy");
            formatted += " at ";
            formatted += time.ToString("h:mm");
            formatted += time.ToString("tt").ToLower();

            return formatted;
        }

        internal static void MoveReplace(string source, string dest)
        {
            File.Copy(source, dest, true);
            File.Delete(source);
        }

        internal static void PruneMap(Dictionary<ulong, uint> map, ulong local, int max)
        {
            if (map.Count < max)
                return;

            List<ulong> removeIDs = new List<ulong>();

            while (map.Count > 0 && map.Count > max)
            {
                ulong furthest = local;

                foreach (ulong id in map.Keys)
                    if ((id ^ local) > (furthest ^ local))
                        furthest = id;

                map.Remove(furthest);
            }
        }

        /*internal static void RemoveWhere(Dictionary<TKey, TValue> map, MatchType isMatch)
        {
            List<TKey> removeKeys = new List<TKey>();

            foreach (KeyValuePair<TKey, TValue> pair in this)
                if (isMatch(pair.Value))
                    removeKeys.Add(pair.Key);


            if (removeKeys.Count > 0)
                foreach (TKey id in removeKeys)
                    Remove(id);

        }*/

        internal static string StripOneLevel(string path)
        {
            int pos = path.LastIndexOf('\\');

            if (pos == -1)
                return "";

            return path.Substring(0, pos);
        }

        internal static void OpenFolder(string path)
        {
            string windir = Environment.GetEnvironmentVariable("WINDIR");
            System.Diagnostics.Process prc = new System.Diagnostics.Process();
            prc.StartInfo.FileName = windir + @"\explorer.exe";
            prc.StartInfo.Arguments = path;
            prc.Start();
        }

        internal static string CommaIze(string num)
        {
            string final = "";

            while (num.Length > 3)
            {
                final = "," + num.Substring(num.Length - 3, 3) + final;
                num = num.Substring(0, num.Length - 3);
            }

            final = num + final;

            return final;
        }

        public static void CopyDirectory(string sourcePath, string destPath)
        {
            if (destPath[destPath.Length - 1] != Path.DirectorySeparatorChar)
                destPath += Path.DirectorySeparatorChar;

            if (!Directory.Exists(destPath))
                Directory.CreateDirectory(destPath);

            String[] files = Directory.GetFileSystemEntries(sourcePath);

            foreach (string path in files)
            {
                // if path is sub dir
                if (Directory.Exists(path))
                    CopyDirectory(path, destPath + Path.GetDirectoryName(path));

                else
                    File.Copy(path, destPath + Path.GetFileName(path), true);
            }
        }

        internal static byte[] GetSalt(int amount, int buffsize, RNGCryptoServiceProvider rnd)
        {
            byte[] salt = new byte[amount];
            rnd.GetBytes(salt);

            byte[] final = new byte[buffsize];
            salt.CopyTo(final, 0);

            return final;
        }

        /*internal static void AddSalt(byte[] original, RNGCryptoServiceProvider rnd, int amount)
        {
            byte[] salt = new byte[amount];
            rnd.GetBytes(salt);

            byte[] final = new byte[original.Length + amount];

            salt.CopyTo(final, 0);
            original.CopyTo(final, amount);

            return final;
        }*/

        internal static byte[] CombineArrays(byte[] a, byte[] b)
        {
            byte[] c = new byte[a.Length + b.Length];

            a.CopyTo(c, 0);
            b.CopyTo(c, a.Length);

            return c;
        }

        internal static byte[] GenerateKey(RNGCryptoServiceProvider rnd, int bits)
        {
            byte[] key = new byte[bits / 8];
            rnd.GetBytes(key);
            return key;
        }
    }


    internal class ListViewColumnSorter : IComparer
    {
        internal int ColumnToSort;
        internal SortOrder OrderOfSort;
        internal CaseInsensitiveComparer ObjectCompare;

        internal ListViewColumnSorter()
        {
            ColumnToSort = 0;
            OrderOfSort = SortOrder.None;
            ObjectCompare = new CaseInsensitiveComparer();
        }

        public int Compare(object x, object y)
        {
            int compareResult;
            ListViewItem listviewX, listviewY;

            // Cast the objects to be compared to ListViewItem objects
            listviewX = (ListViewItem)x;
            listviewY = (ListViewItem)y;

            // Compare the two items
            compareResult = ObjectCompare.Compare(listviewX.SubItems[ColumnToSort].Text, listviewY.SubItems[ColumnToSort].Text);

            // Calculate correct return value based on object comparison
            if (OrderOfSort == SortOrder.Ascending)
                return compareResult;
            else if (OrderOfSort == SortOrder.Descending)
                return (-compareResult);
            else
                return 0;
        }
    }
}

namespace RiseOp.Implementation
{
	internal class BufferData
	{
		internal byte[] Source;
		internal int    Start;
		internal int    Size;

		internal BufferData(byte[] source)
		{
			Source = source;
			Size   = source.Length;

			Debug.Assert(Source.Length >= Size - Start);
		}

		internal BufferData(byte[] source, int start, int size)
		{
			Source = source;
			Start  = start;
			Size   = size;

			Debug.Assert(Source.Length >= Size - Start);
		}

		internal void Reset()
		{
			Start = 0;
			Size  = Source.Length;
		}
	}

	internal class MovingAvg
	{	
		int   Entries;
		int[] Elements;
		int   Pos;
		int   Total;
		int   SecondSum;

		internal MovingAvg(int size)
		{
			Elements = new int[size];
		}

		internal void Input(int val)
		{
			SecondSum += val;
		}

		internal void Next()
		{
			if(Entries < Elements.Length)
				Entries++;

			if(Pos == Elements.Length)
				Pos = 0;

			Total         -= Elements[Pos];
			Elements[Pos]  = SecondSum;
			Total         += SecondSum;

			SecondSum = 0;

			Pos++;
		}

		internal int GetAverage()
		{
			if(Entries > 0)
				return Total / Entries;
			
			return 0;
		}
	}

    internal class AttachedFile
    {
        internal string FilePath;
        internal string Name;
        internal long Size;

        internal AttachedFile(string path)
        {
            FilePath = path;

            Name = Path.GetFileName(FilePath);

            FileInfo info = new FileInfo(path);
            Size = info.Length;

        }

        public override string ToString()
        {
            return Name + " (" + Utilities.ByteSizetoString(Size) + ")";
        }
    }

    internal class IVCryptoStream
    {
        // this class saves the IV at the beginning of the file and loads it again during reading
        internal static CryptoStream Load(string path, byte[] key)
        {
            FileStream file = new FileStream(path, FileMode.Open);

            return IVCryptoStream.Load(file, key);
        }

        internal static CryptoStream Load(Stream stream, byte[] key)
        {
            byte[] iv = new byte[16];
            stream.Read(iv, 0, 16);

            RijndaelManaged crypt = new RijndaelManaged();
            crypt.Key = key;
            crypt.IV = iv;

            return new CryptoStream(stream, crypt.CreateDecryptor(), CryptoStreamMode.Read);
        }

        internal static CryptoStream Save(string path, byte[] key)
        {
            FileStream file = new FileStream(path, FileMode.Create);

            RijndaelManaged crypt = new RijndaelManaged();
            crypt.Key = key;
            crypt.GenerateIV();

            file.Write(crypt.IV, 0, crypt.IV.Length);

            return new CryptoStream(file, crypt.CreateEncryptor(), CryptoStreamMode.Write);
        }
    }

    internal class ServiceEvent<TDelegate>
    {
        internal Dictionary<uint, Dictionary<uint, TDelegate>> HandlerMap = new Dictionary<uint, Dictionary<uint, TDelegate>>();

        internal TDelegate this[uint service, uint type]
        {
            get
            {
                if (Contains(service, type))
                    return HandlerMap[service][type];

                return default(TDelegate);
            }
            set
            {
                // adding handler
                if (value != null)
                {
                    if (!HandlerMap.ContainsKey(service))
                        HandlerMap[service] = new Dictionary<uint, TDelegate>();

                    HandlerMap[service][type] = value;
                }

                // removing handler
                else
                {
                    if (HandlerMap.ContainsKey(service))
                    {
                        if (HandlerMap[service].ContainsKey(type))
                            HandlerMap[service].Remove(type);

                        if (HandlerMap[service].Count == 0)
                            HandlerMap.Remove(service);
                    }
                }
            }
        }

        internal bool Contains(uint service, uint type)
        {
            return HandlerMap.ContainsKey(service) && HandlerMap[service].ContainsKey(type);
        }
    }

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

            if(removeKeys.Count > 0)
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
                finally { Access.DowngradeFromWriterLock( ref cookie); }
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

    internal class Tuple<T, U>
    {
        internal T First;
        internal U Second;

        internal Tuple(T first, U second)
        {
            First = first;
            Second = second;
        }

        public override int GetHashCode()
        {
            return First.GetHashCode() ^ Second.GetHashCode();
        }
    }

    internal class TaggedStream : FileStream
    {
        long InternalSize;

        internal TaggedStream(string path)
            : base(path, FileMode.Open, FileAccess.Read, FileShare.Read)
        {
            Seek(-8, SeekOrigin.End);

            byte[] sizeBytes = new byte[8];
            Read(sizeBytes, 0, sizeBytes.Length);
            InternalSize = BitConverter.ToInt64(sizeBytes, 0);

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

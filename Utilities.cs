using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Implementation.Protocol.Special;

using RiseOp.Interface;
using RiseOp.Interface.TLVex;


namespace RiseOp
{
    // at high security, low threats and above must be checked
    enum ThreatLevel { Low = 3, Medium = 5, High = 7 }
    enum SecurityLevel { Low = 7, Medium = 5, High = 3 }


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

        internal static bool VerifyPassphrase(OpCore core, ThreatLevel threat)
        {
            //crit revise
            if (threat != ThreatLevel.High)
                return true;

            bool trying = true;

            while (trying)
            {
                GetTextDialog form = new GetTextDialog(core, core.User.GetTitle(), "Enter Passphrase", "");

                form.StartPosition = FormStartPosition.CenterScreen;
                form.ResultBox.UseSystemPasswordChar = true;

                if (form.ShowDialog() != DialogResult.OK)
                    return false;

                byte[] key = GetPasswordKey(form.ResultBox.Text, core.User.PasswordSalt);

                if (MemCompare(core.User.PasswordKey, key))
                    return true;

                MessageBox.Show("Wrong passphrase", "RiseOp");
            }

            return false;
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

		internal static bool GetBit(int bits, int pos)
		{
			return (((1 << pos) & bits) > 0);
		}

		internal static bool GetBit(UInt64 bits, int pos)
		{
            pos = 63 - pos;

            return (((1UL << pos) & bits) > 0);
		}

		internal static void SetBit(ref UInt64 bits, int pos, bool val)
		{
            pos = 63 - pos;

			if(val)
                bits |= 1UL << pos;
            else
				bits &= ~1UL << pos; 
		}

        internal static ulong FlipBit(ulong value, int pos)
        {
            pos = 63 - pos;

            return value ^= 1UL << pos;
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
            using (FileStream file = File.OpenRead(path))
            {
                SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider();
                hash = sha.ComputeHash(file);
                size = file.Length;
            }
        }


        internal static void Md5HashFile(string path, ref byte[] hash, ref long size)
        {
            using (FileStream file = File.OpenRead(path))
            {
                MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                hash = md5.ComputeHash(file);
                size = file.Length;
            }
        }

        internal static void HashTagFile(string path, G2Protocol protocol, ref byte[] hash, ref long size)
        {
            using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {
                // record size of file
                long originalSize = file.Length;

                // sha hash 128k chunks of file
                SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider();

                int read = 0;
                int chunkSize = 128; // 128kb chunks
                int chunkBytes = chunkSize * 1024;
                int buffSize = file.Length > chunkBytes ? chunkBytes : (int)file.Length;
                byte[] chunk = new byte[buffSize];
                List<byte[]> hashes = new List<byte[]>();

                read = 1;
                while (read > 0)
                {
                    read = file.Read(chunk, 0, buffSize);

                    if (read > 0)
                        hashes.Add(sha.ComputeHash(chunk, 0, read));
                }

                // write packets - 200 sub-hashes per packet
                int writePos = 0;
                int hashesLeft = hashes.Count;

                while (hashesLeft > 0)
                {
                    int writeCount = (hashesLeft > 100) ? 100 : hashesLeft;

                    hashesLeft -= writeCount;

                    SubHashPacket packet = new SubHashPacket();
                    packet.ChunkSize = chunkSize;
                    packet.TotalCount = hashes.Count;
                    packet.SubHashes = new byte[20 * writeCount];

                    for (int i = 0; i < writeCount; i++)
                        hashes[writePos++].CopyTo(packet.SubHashes, 20 * i);

                    byte[] encoded = packet.Encode(protocol);

                    file.Write(encoded, 0, encoded.Length);
                }

                // write null - end packets
                file.WriteByte(0);

                // attach original size to end of file
                byte[] sizeBytes = BitConverter.GetBytes(originalSize);
                file.Write(sizeBytes, 0, sizeBytes.Length);

                // sha1 hash tagged file
                file.Seek(0, SeekOrigin.Begin);
                hash = sha.ComputeHash(file);
                size = file.Length;
            }
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

            byte[] salt = Utilities.ExtractBytes(core.User.Settings.FileKey, 0, 4);

            byte[] final = Utilities.CombineArrays(salt, UTF8Encoding.UTF8.GetBytes(name));

            SHA1Managed sha1 = new SHA1Managed();
            byte[] hash = new SHA1Managed().ComputeHash(final);

            return Utilities.ToBase64String(hash);
        }

        internal static string CryptFilename(OpCore core, ulong id, byte[] hash)
        {
            // we salt so there are no common file names between users
            byte[] salt = Utilities.ExtractBytes(core.User.Settings.FileKey, 0, 4);

            byte[] buffer = new byte[4 + 8 + hash.Length];
            salt.CopyTo(buffer, 0);
            BitConverter.GetBytes(id).CopyTo(buffer, 4);
            hash.CopyTo(buffer, 12);

            SHA1Managed sha1 = new SHA1Managed();
            byte[] totalHash = new SHA1Managed().ComputeHash(hash);

            return Utilities.ToBase64String(totalHash);
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

        internal static string CommaIze(object value)
        {
            string final = "";

            string num = value.ToString();

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

        internal static bool IsLocalIP(IPAddress address)
        {
            if (address.IsIPv6LinkLocal)
                return true;

            if (address.AddressFamily == AddressFamily.InterNetworkV6 )
                return false;

            byte[] ip = address.GetAddressBytes();


            //1.*.*.*
            //2.*.*.*
            //5.*.*.*
            //6.*.*.*
            //10.*.*.*
            if (ip[0] == 1 || ip[0] == 2 || ip[0] == 5 || ip[0] == 6 || ip[0] == 10)
                return true;

            //127.0.0.1
            if (ip[0] == 127 && ip[1] == 0 && ip[2] == 0 && ip[3] == 1)
                return true;

            //169.254.*.*
            if (ip[0] == 169 && ip[1] == 254)
                return true;

            //172.16.*.* - 172.31.*.*
            if (ip[0] == 172 && (16 <= ip[1] && ip[1] <= 31))
                return true;

            //192.168.*.*
            if (ip[0] == 192 && ip[1] == 168)
                return true;

            return false;
        }

        internal static string ToRtf(string text)
        {
            return "{\\rtf1\\ansi\\ansicpg1252\\deff0\\deflang1033{\\fonttbl{\\f0\\fnil\\fcharset0 Tahoma;}}\r\n{\\colortbl ;\\red0\\green0\\blue0;}\r\n\\viewkind4\\uc1\\pard\\cf1\\f0\\fs20 " + text + "\\cf0}\r\n";
        }

        internal static string GetQuip(string body)
        {
            // rtf to short text quip
            RichTextBox box = new RichTextBox();
            box.Rtf = body;

            string quip = box.Text;
            quip = quip.Replace('\r', ' ');
            quip = quip.Replace('\n', ' ');

            if (quip.Length > 50)
                quip = quip.Substring(0, 50) + "...";

            return quip;
        }

        public static bool AreAllSet(this BitArray array, bool value)
        {
            foreach (bool bit in array)
                if (bit != value)
                    return false;

            return true;
        }

        public static void SortedAdd(this ToolStripItemCollection strip, ToolStripMenuItem item)
        {
            int i = 0;

            for(i = 0; i < strip.Count; i++)
                if (string.Compare(strip[i].Text, item.Text) > 0)
                {
                    strip.Insert(i, item);
                    return;
                }

            strip.Insert(i, item);
        }

        public static byte[] ToBytes(this BitArray array)
        {
            // size info not transmitted, must be send seperately

            int size = array.Length / 8;

            if (array.Length % 8 > 0)
                size += 1;

            byte[] bytes = new byte[size];

            for(int i = 0; i < size; i++)
                for (int x = 0; x < 8; x++)
                {
                    int index = i * 8 + x ;
                    if (index < array.Length && array[index])
                        bytes[i] = (byte)(bytes[i] | (1 << x));
                }

            return bytes;
        }

        public static bool Compare(this BitArray array, BitArray check)
        {
            if (array.Length != check.Length)
                return false;

            for (int i = 0; i < array.Length; i++)
                if (array[i] != check[i])
                    return false;

            return true;
        }

        public static BitArray ToBitArray(byte[] bytes, int length)
        {
            BitArray array = new BitArray(length);

            for(int i = 0; i < bytes.Length; i++)
                for (int x = 0; x < 8; x++)
                {
                    int index = i * 8 + x;

                    if (index >= length)
                        break;

                    if ((bytes[i] & (1 << x)) > 0)
                        array.Set(index, true);
                }

            return array;
        }

        public static int GetDistance(Point start, Point end)
        {
            int x = end.X - start.X;
            int y = end.Y - start.Y;

            return (int)Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
        }

        internal static string WebDownloadString(string url)
        {
            // WebClient DownloadString does the same thing but has a bug that causes it to hang indefinitely
            // in some situations when host is not responding, this we know times out, doesnt cause app close to hang

            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Timeout = 7000;

            WebResponse response = request.GetResponse();

            Stream responseStream = response.GetResponseStream();
            StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8);
            string responseText = streamReader.ReadToEnd();
            response.Close();
            
            return responseText;
        }

        internal static string RtftoColor(string rtf, Color color)
        {
            rtf = rtf.Replace("red0", "red" + color.R);
            rtf = rtf.Replace("blue0", "blue" + color.B);
            rtf = rtf.Replace("green0", "green" + color.G);

            return rtf;
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
                    Utilities.ReadtoEnd(this);
            }

            base.Dispose(disposing);
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

        public override string ToString()
        {
            return First + " - " + Second;
        }
    }

    internal class CircularBuffer<T> : IEnumerable
    {
        internal T[] Buffer;
        internal int CurrentPos = -1;
        internal int Length;

        internal int Capacity
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


        internal CircularBuffer(int capacity)
        {
            Capacity = capacity;
        }

        internal T this[int index]
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

        internal void Add(T value)
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

        public IEnumerator GetEnumerator()
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

        internal void Clear()
        {
            Buffer = new T[Capacity];
            CurrentPos = -1;
            Length = 0;
        }
    }

    // used to store sub-hashes for files that are transferred over the network
    internal delegate void ProcessTagsHandler(PacketStream stream);

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

    internal class BandwidthLog
    {
        internal CircularBuffer<int> In;
        internal CircularBuffer<int> Out;

        internal int InPerSec;
        internal int OutPerSec;


        internal BandwidthLog(int size)
        {
            In = new CircularBuffer<int>(size);
            Out = new CircularBuffer<int>(size);
        }

        internal void NextSecond()
        {
            In.Add(InPerSec);
            InPerSec = 0;

            Out.Add(OutPerSec);
            OutPerSec = 0;
        }

        internal void Resize(int seconds)
        {
            In.Capacity = seconds;
            Out.Capacity = seconds;
        }

        internal float InOutAvg(int period)
        {
            return Average(In, period) + Average(Out, period);
        }

        internal float InAvg()
        {
            return Average(In, In.Length);
        }

        internal float OutAvg()
        {
            return Average(Out, Out.Length);
        }

        internal float Average(CircularBuffer<int> buff, int period)
        {
            float avg = 0;

            int i = 0;
            for (; i < period && i < buff.Length; i++)
                avg += buff[i];

            return (i > 0) ? avg / i : 0;
        }
    }
}

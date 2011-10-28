using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Protocol.Special;


namespace DeOps
{
    // at high security, low threats and above must be checked
    public enum ThreatLevel { Low = 3, Medium = 5, High = 7 }
    enum SecurityLevel { Low = 7, Medium = 5, High = 3 }
    enum TextFormat { Plain = 0, RTF = 1, HTML = 2 }


	internal static partial class Utilities
	{  

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

        internal static byte[] CombineArrays(byte[] a, byte[] b)
        {
            byte[] c = new byte[a.Length + b.Length];

            a.CopyTo(c, 0);
            b.CopyTo(c, a.Length);

            return c;
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

        public static bool AreAllSet(this BitArray array, bool value)
        {
            foreach (bool bit in array)
                if (bit != value)
                    return false;

            return true;
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

        internal static void ExtractAttachedFile(string source, byte[] key, long fileStart, long[] attachments, int index, string destination)
        {
            using (TaggedStream tagged = new TaggedStream(source, new G2Protocol()))
            using (IVCryptoStream crypto = IVCryptoStream.Load(tagged, key))
            {
                // get past packet section of file
                const int buffSize = 4096;
                byte[] buffer = new byte[4096];

                long bytesLeft = fileStart;
                while (bytesLeft > 0)
                {
                    int readSize = (bytesLeft > buffSize) ? buffSize : (int)bytesLeft;
                    int read = crypto.Read(buffer, 0, readSize);
                    bytesLeft -= read;
                }

                // setup write file
                using (FileStream outstream = new FileStream(destination, FileMode.Create, FileAccess.Write))
                {
                    // read files, write the right one :P
                    for (int i = 0; i < attachments.Length; i++)
                    {
                        bytesLeft = attachments[i];

                        while (bytesLeft > 0)
                        {
                            int readSize = (bytesLeft > buffSize) ? buffSize : (int)bytesLeft;
                            int read = crypto.Read(buffer, 0, readSize);
                            bytesLeft -= read;

                            if (i == index)
                                outstream.Write(buffer, 0, read);
                        }
                    }
                }
            }
        }
    }
}

namespace DeOps.Implementation
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

/********************************************************************************

	De-Ops: Decentralized Operations
	Copyright (C) 2006 John Marshall Group, Inc.

	By contributing code you grant John Marshall Group an unlimited, non-exclusive
	license to your contribution.

	For support, questions, commercial use, etc...
	E-Mail: swabby@c0re.net

********************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Text;
using System.IO;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Interface.TLVex;


namespace DeOps
{
	/// <summary>
	/// Summary description for Utilities.
	/// </summary>
	internal static class Utilities
	{
		internal static RijndaelManaged PasswordtoRijndael(string password)
		{
			// Encrypt password with sha1
			UnicodeEncoding strEncoder     = new UnicodeEncoding();
			SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
			byte[] passwordHash = sha1.ComputeHash( strEncoder.GetBytes(password) );

			byte[] key = new byte[32];
			byte[] iv  = new byte[16];

			for(int i = 0; i < 2; i++) // key is first 16 bytes from password hash, doubled
				Buffer.BlockCopy(passwordHash, 0, key, i * 16, 16);
			for(int i = 0; i < 4; i++) // IV is last 4 bytes of password hash quadrupled
				Buffer.BlockCopy(passwordHash, 16, iv, i * 4, 4);

			RijndaelManaged rijndael = new RijndaelManaged();
			rijndael.Key = key;
			rijndael.IV  = iv;

			return rijndael;
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

		internal static IPAddress BytestoIP(byte[] data, int startPos)
		{
			// we need this because IPAddress constructor in 1.1 is bugged

			if(data.Length - startPos < 4) 
				return IPAddress.Parse("0.0.0.0");

			string address = "";

			for(int i = startPos; i < startPos + 4; i++)
				address += data[i].ToString() + ".";

			return IPAddress.Parse( address.TrimEnd('.') );
		}

		internal static byte[] AddresstoBytes(IPEndPoint ip)
		{
			byte[] bytes = new byte[6];

            ip.Address.GetAddressBytes().CopyTo(bytes, 0);
            BitConverter.GetBytes((ushort)ip.Port).CopyTo(bytes, 4);

			return bytes;
		}

		internal static byte[] EncodeAddresses(ArrayList addresses)
		{
			int offset = 0;

			// ip 4b, udp 2b = 6b per contact
			byte[] buffer = new byte[addresses.Count * 6];

			foreach(IPEndPoint address in addresses)
			{
                AddresstoBytes(address).CopyTo(buffer, offset);
				offset += 6;
			}
			
			return buffer;
		}

		internal static ArrayList DecodeAddresses(byte[] data)
		{	
			return DecodeAddresses(data, 0, data.Length);
		}

		internal static ArrayList DecodeAddresses(byte[] data, int payloadPos, int payloadSize)
		{
			ArrayList addresses = new ArrayList();

			int offset = 0;

			while(offset < payloadSize)
			{	
				IPAddress ip   = Utilities.BytestoIP( data, payloadPos + offset);
				ushort    port = BitConverter.ToUInt16(data, payloadPos + offset + 4);
				
				addresses.Add( new IPEndPoint(ip, port));

				offset += 6;
			}

			return addresses;
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

        internal static void CheckSignedData(byte[] key, byte[] data, byte[] sig)
        {
            // check signature
            RSACryptoServiceProvider rsa = Utilities.KeytoRsa(key);

            if (!rsa.VerifyData(data, new SHA1CryptoServiceProvider(), sig))
                throw new Exception("Signature check failed");
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



        internal static byte[] EncryptBytes(byte[] data, RijndaelManaged crypt)
        {
            crypt.GenerateIV();
            crypt.Padding = PaddingMode.PKCS7;
            ICryptoTransform encryptor = crypt.CreateEncryptor();
            byte[] transformed = encryptor.TransformFinalBlock(data, 0, data.Length);

            byte[] final = new byte[crypt.IV.Length + transformed.Length];

            crypt.IV.CopyTo(final, 0);
            transformed.CopyTo(final, crypt.IV.Length);

            return final;
        }

        internal static byte[] DecryptBytes(byte[] data, int length, RijndaelManaged crypt)
        {
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

        internal static string CryptFilename(RijndaelManaged crypt, string name)
        {
            UTF8Encoding converter = new UTF8Encoding();
            ICryptoTransform transform = crypt.CreateEncryptor();

            byte[] buffer = converter.GetBytes(name);
            return BytestoHex(transform.TransformFinalBlock(buffer, 0, buffer.Length));
        }

        internal static string CryptFilename(RijndaelManaged crypt, ulong id, byte[] hash)
        {
            // prevent nodes with same hash for file from overwriting, or even attacking each other

            byte[] buffer = new byte[8 + hash.Length];
            BitConverter.GetBytes(id).CopyTo(buffer, 0);
            hash.CopyTo(buffer, 8);

            ICryptoTransform transform = crypt.CreateEncryptor();

            return BytestoHex(transform.TransformFinalBlock(buffer, 0, buffer.Length));
        }

        internal static bool InBounds(ulong zero, ulong distance, ulong id)
        {
            if ((id ^ zero) <= distance)
                return true;

            return false;
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

        internal static string StripOneLevel(string path)
        {
            int pos = path.LastIndexOf('\\');

            if (pos == -1)
                return "";

            return path.Substring(0, pos);
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

}

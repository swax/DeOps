using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;


namespace RiseOp.Implementation.Protocol
{
	internal enum G2ReadResult { PACKET_GOOD, PACKET_INCOMPLETE, PACKET_ERROR, STREAM_END };

	/// <summary>
	/// Summary description for G2Protocol.
	/// </summary>
	internal class G2Protocol
	{
		const int MAX_FRAMES     = 500;
		const int MAX_WRITE_SIZE = 32768;
		const int MAX_FINAL_SIZE = 65536;
		const int G2_PACKET_BUFF = (65536+1024);

		int    WriteOffset;
		byte[] WriteData = new byte[MAX_WRITE_SIZE];
		

		ArrayList Frames = new ArrayList();

		int    FinalSize;
		byte[] FinalPacket = new byte[MAX_FINAL_SIZE];
		

		ASCIIEncoding StringEnc = new ASCIIEncoding();
        internal UTF8Encoding UTF = new UTF8Encoding();

        internal object WriteSection = new object();


		internal G2Protocol()
		{

		}

		internal G2Frame WritePacket(G2Frame root, byte name, byte[] payload)
		{
			// If new packet
			if(root == null)
			{
				if(WriteOffset != 0)
				{
					Frames.Clear();
					WriteOffset = 0;

					// caused by error building packet before
					throw new Exception("Packet Frames not clear");//Debug.Assert(false); // Careful, can be caused by previous assert further down call stack
				}

				FinalSize = 0;
			}

			if(Frames.Count > MAX_FRAMES)
			{
				Debug.Assert(false);
				return null;
			}

			// Create new frame
			G2Frame packet = new G2Frame();

			packet.Parent = root;
			packet.Name   = name;
	
			if(payload != null)
			{
				if(WriteOffset + payload.Length > MAX_WRITE_SIZE)
				{
					Debug.Assert(false);
					return null;
				}

				packet.PayloadLength = payload.Length;
				packet.PayloadPos    = WriteOffset;
                payload.CopyTo(WriteData, WriteOffset);
				WriteOffset += payload.Length;
			}

			Frames.Add(packet);

			return packet;
		}

		internal byte[] WriteFinish()
		{
			// Reverse iterate through packet structure, set lengths
			Frames.Reverse();
			foreach(G2Frame packet in Frames)
			{		
				if(packet.InternalLength > 0 && packet.PayloadLength > 0)
					packet.InternalLength += 1; // For endstream byte

				packet.PayloadOffset   =  packet.InternalLength;
				packet.InternalLength  += packet.PayloadLength;

				packet.LenLen = 0;
				while(packet.InternalLength >= Math.Pow(256, packet.LenLen) )
					packet.LenLen++;

				Debug.Assert(packet.LenLen < 8);

				packet.HeaderLength = 1 + packet.LenLen + 1;

				if( packet.Parent != null)
				{
					packet.Parent.InternalLength += packet.HeaderLength + packet.InternalLength;	
					packet.Parent.Compound = 1;
				}
			}

			// Iterate through packet stucture, build packet
			Frames.Reverse(); // back to forwards
			foreach(G2Frame packet in Frames)
			{
				int nextByte = 0;

				if( packet.Parent != null)
				{
					Debug.Assert(packet.Parent.NextChild != 0);
					nextByte = packet.Parent.NextChild;
					packet.Parent.NextChild += packet.HeaderLength + packet.InternalLength;
				}
				else // beginning of packet
				{
					FinalSize = packet.HeaderLength + packet.InternalLength;
				}

				byte control = 0;
				control |= (byte) (packet.LenLen << 5);
                control |= (byte) (1 << 3);
				control |= (byte) (packet.Compound << 2);
		
				Buffer.SetByte(FinalPacket, nextByte, control);
				nextByte += 1;

				// DNA should not pass packets greater than 4096, though pass through packets could be bigger
				if(packet.HeaderLength + packet.InternalLength > MAX_WRITE_SIZE)
				{
					Debug.Assert(false);

					Frames.Clear();
					WriteOffset = 0;
					FinalSize   = 0;

					return null;
				}

				byte [] lenData = BitConverter.GetBytes(packet.InternalLength);
				Buffer.BlockCopy(lenData, 0, FinalPacket, nextByte, packet.LenLen);
				nextByte += packet.LenLen;

                FinalPacket[nextByte] = packet.Name;
				nextByte += 1;

				if(packet.Compound == 1)
					packet.NextChild = nextByte;

				if( packet.PayloadLength != 0)
				{
					int finalPos = nextByte + packet.PayloadOffset;
					Buffer.BlockCopy(WriteData, packet.PayloadPos, FinalPacket, finalPos, packet.PayloadLength);

					if(packet.Compound == 1) // Set stream end
					{
						finalPos -= 1;
						Buffer.SetByte(FinalPacket, finalPos, 0);
					}
				}
			}

			Debug.Assert(FinalSize != 0);

			Frames.Clear();
			WriteOffset = 0;

            return Utilities.ExtractBytes(FinalPacket, 0, FinalSize);
		}

		internal static G2ReadResult ReadNextPacket( G2Header packet, ref int readPos, ref int readSize )
		{
			if( readSize == 0 )
				return G2ReadResult.PACKET_INCOMPLETE;

			int beginPos   = readPos;
			int beginSize  = readSize;

			packet.PacketPos = readPos;
			
			// Read Control Byte
			byte control = Buffer.GetByte(packet.Data, readPos);

			readPos  += 1;
			readSize -= 1;

			if ( control == 0 ) 
				return G2ReadResult.STREAM_END;

			byte lenLen  = (byte) ( (control & 0xE0) >> 5); // 11100000
			byte nameLen = (byte) ( (control & 0x18) >> 3); // 00011000 
			byte flags   = (byte)   (control & 0x07);       // 00000111

			bool bigEndian  = (flags & 0x02) != 0; 
			bool isCompound = (flags & 0x04) != 0; 

			if( bigEndian )
				return G2ReadResult.PACKET_ERROR;

			packet.HasChildren = isCompound;
			
			// Read Packet Length
			packet.InternalSize = 0;
			if( lenLen != 0)
			{	
				if(readSize < lenLen)
				{
					readPos  = beginPos;
					readSize = beginSize;
					return G2ReadResult.PACKET_INCOMPLETE;
				}
				
				byte[] lenData = new byte[8]; // create here because lenLen is less than 8 in size
				Buffer.BlockCopy(packet.Data, readPos, lenData, 0, lenLen);

				packet.InternalSize = BitConverter.ToInt32(lenData, 0); // only 4 bytes supported so far

				Debug.Assert(MAX_FINAL_SIZE < G2_PACKET_BUFF);
				if(packet.InternalSize >= MAX_FINAL_SIZE)
				{
					//Debug.Assert(false);
					return G2ReadResult.PACKET_ERROR;
				}

				readPos  += lenLen;
				readSize -= lenLen;
			}

			// Read Packet Name
			if(readSize < nameLen)
			{
				readPos  = beginPos;
				readSize = beginSize;
				return G2ReadResult.PACKET_INCOMPLETE;
			}

            if(nameLen != 1)
                return G2ReadResult.PACKET_ERROR;

			/*if(packet.Name.Length + 1 + nameLen > MAX_NAME_SIZE - 1)
			{
				Debug.Assert(false);
				packet.Name = "ERROR";
			}
			else
			{
				packet.Name += "/" + StringEnc.GetString(packet.Data, readPos, nameLen);
			}*/

            packet.Name = packet.Data[readPos];

			readPos  += nameLen;
			readSize -= nameLen;

			// Check if full packet length available in stream
			if(readSize < packet.InternalSize)
			{
				readPos  = beginPos;
				readSize = beginSize;
				return G2ReadResult.PACKET_INCOMPLETE;
			}

			packet.InternalPos = (packet.InternalSize > 0) ? readPos : 0;
			
			packet.NextBytePos   = packet.InternalPos;
			packet.NextBytesLeft = packet.InternalSize;

			readPos  += packet.InternalSize;
			readSize -= packet.InternalSize;

			packet.PacketSize = 1 + lenLen + nameLen + packet.InternalSize;

			return G2ReadResult.PACKET_GOOD;
		}

		internal static bool ReadPayload(G2Header packet)
		{
			ResetPacket(packet);

			G2Header child = new G2Header(packet.Data);

			G2ReadResult streamStatus = G2ReadResult.PACKET_GOOD;
			while( streamStatus == G2ReadResult.PACKET_GOOD )
				streamStatus = ReadNextChild(packet, child);

			if(streamStatus == G2ReadResult.STREAM_END)
			{
				if( packet.NextBytesLeft > 0)
				{
					packet.PayloadPos  = packet.NextBytePos;
					packet.PayloadSize = packet.NextBytesLeft;

					return true;
				}
			}
			else if( packet.NextBytesLeft > 0)
			{
				// Payload Read Error
				//m_pG2Comm->m_pCore->DebugLog("G2 Network", "Payload Read Error: " + HexDump(packet.Packet, packet.PacketSize));
			}

			return false;
		}

		internal static void ResetPacket(G2Header packet)
		{
			packet.NextBytePos   = packet.InternalPos;
			packet.NextBytesLeft = packet.InternalSize;

			packet.PayloadPos  = 0;
			packet.PayloadSize = 0;
		}

		internal static G2ReadResult ReadNextChild( G2Header root, G2Header child)
		{
			if( !root.HasChildren )
				return G2ReadResult.STREAM_END;

			return ReadNextPacket(child, ref root.NextBytePos, ref root.NextBytesLeft);
		}

        internal bool ReadPacket(G2Header root)
        {
            int start  = 0;
            int length = root.Data.Length;

            if (G2ReadResult.PACKET_GOOD == ReadNextPacket(root, ref start, ref length))
                return true;

            return false;
        }

        internal int WriteToFile(G2Packet packet, Stream stream)
        {
            byte[] data = packet.Encode(this);

            stream.Write(data, 0, data.Length);

            return data.Length;
        }
    }

	/// <summary>
	/// Summary description for G2Frame.
	/// </summary>
	internal class G2Frame
	{
		internal G2Frame Parent;

		internal int HeaderLength;
        internal int InternalLength;
	
		internal byte Name;

		internal byte LenLen;
		internal byte Compound;

		internal int   NextChild;

		internal int   PayloadPos;
		internal int   PayloadLength;
        internal int   PayloadOffset;


		internal G2Frame()
		{
			
		}
	}

    internal class PacketStream
    {
        G2Protocol Protocol;
        Stream     ParentStream;
        FileAccess Access;

        byte[] ReadBuffer;
        int    ReadSize;
        int    Start;

        G2ReadResult ReadStatus = G2ReadResult.PACKET_INCOMPLETE;


        internal PacketStream(Stream stream, G2Protocol protocol, FileAccess access)
        {
            ParentStream = stream;
            Protocol = protocol;
            Access = access;

            if(access == FileAccess.Read)
                ReadBuffer = new byte[4096]; // break/resume relies on 4kb buffer
        }

        internal bool ReadPacket(ref G2Header root)
        {
            root = new G2Header(ReadBuffer);

            // continue from left off, read another goo packete
            if (ReadNext(root))
                return true;

            if (ReadStatus != G2ReadResult.PACKET_INCOMPLETE)
                return false;

            // re-align
            if (ReadSize > 0)
                Buffer.BlockCopy(ReadBuffer, Start, ReadBuffer, 0, ReadSize);

            // incomplete, or just started, read some more from file
            Start = 0;
            ReadSize += ParentStream.Read(ReadBuffer, ReadSize, ReadBuffer.Length - ReadSize);

            if (ReadNext(root))
                return true;


            return false;
        }

        private bool ReadNext(G2Header root)
        {
            if (ReadSize > 0)
            {
                ReadStatus = G2Protocol.ReadNextPacket(root, ref Start, ref ReadSize);

                if (ReadStatus == G2ReadResult.PACKET_GOOD)
                    return true;
            }

            return false;
        }

        internal void WritePacket(G2Packet packet)
        {
            byte[] data = packet.Encode(Protocol);
            ParentStream.Write(data, 0, data.Length);
        }

        internal byte[] Break()
        {
            byte[] remaining = Utilities.ExtractBytes(ReadBuffer, Start, ReadSize);

            ReadSize = 0;
            ReadStatus = G2ReadResult.PACKET_INCOMPLETE;

            return remaining;
        }

        internal void Resume(byte[] data, int size)
        {
            Start = 0;
            ReadSize = size;
            data.CopyTo(ReadBuffer, 0);
            ReadStatus = G2ReadResult.PACKET_INCOMPLETE;
        }

        internal void Close()
        {
            Utilities.ReadtoEnd(ParentStream);

            ParentStream.Close();
        }
    }

    internal static class CompactNum
    {
        internal static byte[] GetBytes(uint num)
        {
            if (num <= byte.MaxValue)
                return BitConverter.GetBytes((byte)num);

            else if (num <= ushort.MaxValue)
                return BitConverter.GetBytes((ushort)num);

            else
                return BitConverter.GetBytes(num);
        }

        internal static byte[] GetBytes(long num)
        {
            Debug.Assert(num >= 0); // kinda stupid, all the file size nums are long, compact doesnt support negs yet

            if (num <= uint.MaxValue)
                return CompactNum.GetBytes((uint)num);

            else
                return BitConverter.GetBytes(num);
        }

        internal static uint ToUInt32(byte[] data, int pos, int size)
        {
            if (size == 1)
                return (uint)data[pos];

            else if (size == 2)
                return (uint)BitConverter.ToUInt16(data, pos);

            else
                return BitConverter.ToUInt32(data, pos);

        }

        internal static long ToInt64(byte[] data, int pos, int size)
        {
            if (size <= 4)
                return (long)CompactNum.ToUInt32(data, pos, size);

            else
                return BitConverter.ToInt64(data, pos);

        }
    }
}

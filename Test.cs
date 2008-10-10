using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

using RiseOp.Services.Storage;
using RiseOp.Services.Location;
using RiseOp.Services.Transfer;
using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;


namespace RiseOp
{
    class TestFile
    {

    }


	internal class Test
	{
		internal Test()
        {
            Random rnd = new Random();

            /* // testing selecting next chunk to send
            ChunkBit[] chunks = new ChunkBit[10];

            for(int i = 0; i < chunks.Length; i++)
            {
                chunks[i].Index = i;
                chunks[i].Popularity = rnd.Next(4);
            }

            ChunkBit[] selected = (from chunk in chunks
                                   orderby rnd.Next() 
                                   orderby chunk.Popularity
                                   select chunk).ToArray();

            */

            /* test getting bucket index of remote node
            Random rnd = new Random();
            ulong local = Utilities.RandUInt64(rnd);
            string localStr = Utilities.IDtoBin(local);

            for (int i = 0; i < 1000; i++)
            {
                ulong remote = Utilities.RandUInt64(rnd);
                string remoteStr = Utilities.IDtoBin(remote);

                for (int x = 0; x < 64; x++)
                    if (Utilities.GetBit(local, x) != Utilities.GetBit(remote, x))
                        break;
                    else
                        index++;

                int y = 0;
            }*/


            /*
            TaggedStream file = new TaggedStream(oldPath, Network.Protocol);
            CryptoStream crypto = IVCryptoStream.Load(file, local.File.Header.FileKey);
            oldStream = new PacketStream(crypto, Protocol, FileAccess.Read);
            */

            /*List<Tuple<int, string>> tups = new List<Tuple<int, string>>();

            tups.Add(new Tuple<int, string>(1, "a"));
            tups.Add(new Tuple<int, string>(2, "b"));
            tups.Add(new Tuple<int, string>(3, "c"));
            tups.Add(new Tuple<int, string>(4, "d"));

            var z = tups.ToDictionary(t => t.First);


            Dictionary<int, string> d1 = new Dictionary<int, string>();
            Dictionary<int, string> d2 = new Dictionary<int, string>();


            d1[1] = "a";
            d1[2] = "b";

            d1[3] = "c";
            d1[4] = "d";

            var x = d1.Concat(d2);

            int y = 1;*/

            // test compact num
            /*
            sbyte a = CompactNum.ToInt8(CompactNum.GetBytes(sbyte.MaxValue), 0, 1);
            short b = CompactNum.ToInt16(CompactNum.GetBytes(short.MaxValue), 0, 2);
            int c = CompactNum.ToInt32(CompactNum.GetBytes(int.MaxValue), 0, 4);
            long d = CompactNum.ToInt64(CompactNum.GetBytes(long.MaxValue), 0, 8);

            sbyte e = CompactNum.ToInt8(CompactNum.GetBytes(sbyte.MinValue), 0, 1);
            short f = CompactNum.ToInt16(CompactNum.GetBytes(short.MinValue), 0, 2);
            int g = CompactNum.ToInt32(CompactNum.GetBytes(int.MinValue), 0, 4);
            long h = CompactNum.ToInt64(CompactNum.GetBytes(long.MinValue), 0, 8);

            byte i = CompactNum.ToUInt8(CompactNum.GetBytes(byte.MaxValue), 0, 1);
            ushort j = CompactNum.ToUInt16(CompactNum.GetBytes(ushort.MaxValue), 0, 2);
            uint k = CompactNum.ToUInt32(CompactNum.GetBytes(uint.MaxValue), 0, 4);
            ulong l = CompactNum.ToUInt64(CompactNum.GetBytes(ulong.MaxValue), 0, 8);
            */

            /*  // bitarray to/from bytes test
             * Random rnd = new Random();

            for (int runs = 0; runs < 10000; runs++)
            {
                int bits = rnd.Next(1, 2000);

                BitArray original = new BitArray(bits);

                for (int i = 0; i < bits; i++)
                    original.Set(i, rnd.Next(2) == 1);

                byte[] buff = original.ToBytes();

                BitArray check = Utilities.ToBitArray(buff, bits);

                byte[] buff2 = check.ToBytes();


                Debug.Assert(Utilities.MemCompare(buff, buff2));
            }*/




            // test encode web request and get reply


            /*string cacheKey = "O+6IRs7GY1r/JIk+DFY/VK+i8pFTWhsDfNH9R3j3f9Q=";

            RijndaelManaged crypt = new RijndaelManaged();
            crypt.BlockSize = 256;
            crypt.Padding = PaddingMode.Zeros;
            crypt.GenerateIV();
            crypt.Key = Convert.FromBase64String(cacheKey);

            Random rnd = new Random();

            string combined = "";

            if (true)
            {
                string type = "publish";
                ulong op = 1476981679885938220;
                //IPAddress adddress = IPAddress.Parse("24.218.20.180");
                IPAddress adddress = new IPAddress(rnd.Next());
                ulong user = Utilities.RandUInt64(rnd);
                ushort tcp = (ushort)rnd.Next();
                ushort udp = (ushort)rnd.Next();

                combined = type + ":" + op + "/" +
                                   user + "/" +
                                   adddress + "/" +
                                   tcp + "/" +
                                   udp;
            }

            if (false)
            {
                string type = "query";
                ulong op = 1476981679885938220;
                combined = type + ":" + op;
            }

            if (false)
            {
                ulong op = 1476981679885938220;
                combined = "ping" + ":" + op;
            }
            
            byte[] data = ASCIIEncoding.ASCII.GetBytes(combined);
            byte[] encrypted = crypt.CreateEncryptor().TransformFinalBlock(data, 0, data.Length);


            // php decode requests
            byte[] ivEnc = Utilities.CombineArrays(crypt.IV, encrypted);
            string request = Convert.ToBase64String(ivEnc);

            string response = Utilities.WebDownloadString("http://www.riseop.com/cache/update.php?get=" + Uri.EscapeDataString(request));

            // php encode response
            byte[] decoded = Convert.FromBase64String(response);

            // decode response
            crypt.IV = Utilities.ExtractBytes(decoded, 0, 32);

            data = Utilities.ExtractBytes(decoded, 32, decoded.Length - 32);
            byte[] decrypted = crypt.CreateDecryptor().TransformFinalBlock(data, 0, data.Length);

            response = ASCIIEncoding.ASCII.GetString(decrypted);
            response = response.Trim('\0');

            string[] lines = response.Split('\n');


            int x = 0;*/

            /*IPAddress address = IPAddress.Parse("1.2.3.4");

            byte[] transmit = address.GetAddressBytes();

            IPAddress check = new IPAddress(transmit);

            IPAddress address6 = IPAddress.Parse("2001:db8::1428:57ab");

            byte[] transmit6 = address6.GetAddressBytes();

            IPAddress check6 = new IPAddress(transmit6);*/
		}

        /*void TestSigning()
        {
            RSACryptoServiceProvider Issuer = new RSACryptoServiceProvider();
            RSACryptoServiceProvider Test2048 = new RSACryptoServiceProvider(2048);

            RSAParameters pub1024 = Issuer.ExportParameters(false);
            RSAParameters priv1024 = Issuer.ExportParameters(true);

            RSAParameters priv2048 = Test2048.ExportParameters(true);

            Random rndGen = new Random();

            // some data
            byte[] data = new byte[512];
            rndGen.NextBytes(data);
            
            // sign it with private key
            byte[] sig = Issuer.SignData(data, new SHA1CryptoServiceProvider());

            // verify signature with internal key
            bool validated = Issuer.VerifyData(data, new SHA1CryptoServiceProvider(), sig);

            // how should internal key data be sent?
           
        }*/

        /*internal static void StreamCompression()
        {
            byte[] SendBuffer    = new byte[2096];
            byte[] ReceiveBuffer = new byte[2096];
            byte[] PacketBuffer  = new byte[2096];

            Deflater Deflate = new Deflater(Deflater.DEFAULT_COMPRESSION, true);
            Inflater Inflate = new Inflater(true);

            Random rand = new Random(234234);


            int SendBytesReady = 0;

            for(int i = 0; i < 10000; i++)
            {
                // sending
                if( !Deflate.IsNeedingInput )
                    throw new Exception("Deflater does not need input, packet dropped");
            */
        /************************
         * byte[] input = new byte[rand.Next(300, 500)];

        for(int j = 0; j < input.Length; j++)
            input[j] = (byte) rand.Next(byte.MaxValue);

        Deflate.SetInput(input);
				
        SendBytesReady += Deflate.Deflate(SendBuffer, SendBytesReady, SendBuffer.Length - SendBytesReady);
        ************************/
        /*
            byte[] input = new byte[rand.Next(300, 500)];

            for(int j = 0; j < input.Length; j++)
                input[j] = (byte) rand.Next(byte.MaxValue);

            Deflate.SetInput(input);
				
            Deflate.Flush();
            SendBytesReady += Deflate.Deflate(SendBuffer, SendBytesReady, SendBuffer.Length - SendBytesReady);
				
            input = new byte[rand.Next(300, 500)];

            for(int j = 0; j < input.Length; j++)
                input[j] = (byte) rand.Next(byte.MaxValue);

            Deflate.SetInput(input);

            SendBytesReady += Deflate.Deflate(SendBuffer, SendBytesReady, SendBuffer.Length - SendBytesReady);
				
            Deflate.Flush();
            SendBytesReady += Deflate.Deflate(SendBuffer, SendBytesReady, SendBuffer.Length - SendBytesReady);
				
	

            byte[] wireData = Utilities.ExtractBytes(SendBuffer, 0, SendBytesReady);

            // receiving
            if(!Inflate.IsNeedingInput)
                throw new Exception("Inflate not needing input");
		
            if(Inflate.IsNeedingInput)
                Inflate.SetInput(wireData, 0, wireData.Length);

            int bytesInflated = Inflate.Inflate(PacketBuffer, 0, PacketBuffer.Length);

            int z = 0;
            z++;
        }

        int x = 0;
        x++;
    }*/

        /*internal static void Bin2Hex()
        {
            RNGCryptoServiceProvider rndGen = new RNGCryptoServiceProvider();

            byte[] testBytes = new byte[1000];

            rndGen.GetBytes(testBytes);

            string hex = Utilities.BytestoHex(testBytes);

            byte[] check = Utilities.HextoBytes(hex);

            if( Utilities.MemCompare(testBytes, 0, check, 0, 1000))
            {
                int i = 0;
                i++;
            }
        }
	
        internal static void G2Packets()
        {
            System.Text.ASCIIEncoding stringEnc = new System.Text.ASCIIEncoding();

            // encode packet
            G2Protocol protocol = new G2Protocol();
			
            G2Frame names = protocol.WritePacket(null, "Dogs", stringEnc.GetBytes("These are some dogs"));
            protocol.WritePacket(names, "Pug",  stringEnc.GetBytes("Rex"));
            protocol.WritePacket(names, "Rapper", stringEnc.GetBytes("Snoop"));
            protocol.WriteFinish();


            // decode packet
            byte[] readStream = new byte[1000];
            int readPos  = 100;
            int readSize = protocol.FinalSize;
            Buffer.BlockCopy(protocol.FinalPacket, 0, readStream, 100, protocol.FinalSize);


            // read packet
            G2Header undefPacket = new G2Header(readStream);
			
            protocol.ReadNextPacket(undefPacket, ref readPos, ref readSize);

            if( G2Protocol.ReadPayload(undefPacket) )
            {
                string test = stringEnc.GetString(readStream, undefPacket.PayloadPos, undefPacket.PayloadSize);
                test += " ";
            }

            G2Protocol.ResetPacket(undefPacket);

            G2Header childPacket = new G2Header(readStream);
            G2ReadResult childStatus = G2ReadResult.PACKET_GOOD;

            while( childStatus == G2ReadResult.PACKET_GOOD )
            {
                childStatus = G2Protocol.ReadNextChild( undefPacket, childPacket );

                if( childStatus != G2ReadResult.PACKET_GOOD )
                    continue;

                if( childPacket.Name == "/Dogs/Pug")
                    if( G2Protocol.ReadPayload(childPacket) && childPacket.PayloadSize != 0)
                    {
                        string test = stringEnc.GetString(readStream, childPacket.PayloadPos, childPacket.PayloadSize);
                        test += " ";
                    }

                if( childPacket.Name == "/Dogs/Rapper")
                    if( G2Protocol.ReadPayload(childPacket) && childPacket.PayloadSize != 0)
                    {
                        string test = stringEnc.GetString(readStream, childPacket.PayloadPos, childPacket.PayloadSize);
                        test += " ";
                    }
            }
        }

        internal static void Compression(string Data)
        {
            System.Text.ASCIIEncoding encoder = new System.Text.ASCIIEncoding();
			
            BufferData srcData = new BufferData(encoder.GetBytes(Data));

            BufferData dstData = new BufferData(new byte[20000]);

            // compress
            Utilities.Compress(srcData, ref dstData);

            // decompress
            BufferData testData = new BufferData(new byte[20000]);
					
            Utilities.Decompress(dstData, ref testData);
			

            // compare
            string CheckData = encoder.GetString(testData.Source, 0, testData.Size);

            Debug.Assert(CheckData == Data);
        }

        void Encrypt()
        {
            //Generate a public/private key pair.
            RSACryptoServiceProvider RSA = new RSACryptoServiceProvider(2048);

            ASCIIEncoding StringEnc = new ASCIIEncoding();
		
            byte[] test = RSA.Encrypt(StringEnc.GetBytes("Does it work?"), false);



            string original = "A small message xxx";
            string roundtrip;
            ASCIIEncoding textConverter = new ASCIIEncoding();
            RijndaelManaged myRijndael = new RijndaelManaged();
            byte[] fromEncrypt;
            byte[] encrypted;
            byte[] toEncrypt;
            byte[] key;
            byte[] IV;

            //Create a new key and initialization vector.
            myRijndael.GenerateKey();
            myRijndael.GenerateIV();

            //Get the key and IV.
            key = myRijndael.Key;
            IV = myRijndael.IV;

            //Get an encryptor.
            ICryptoTransform encryptor = myRijndael.CreateEncryptor(key, IV);
            
            //Encrypt the data.
            MemoryStream msEncrypt = new MemoryStream();
            CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

            //Convert the data to a byte array.
            toEncrypt = textConverter.GetBytes(original);

            //Write all data to the crypto stream and flush it.
            csEncrypt.Write(toEncrypt, 0, toEncrypt.Length);
            csEncrypt.FlushFinalBlock();

            //Get encrypted array of bytes.
            encrypted = msEncrypt.ToArray();
        }

        internal static void UdpNetwork()
        {
            // create 2 nodes, send large and small packets between them
            // tests sending normal or compressed data and its retrieval
            // debugger required to validate test
        }
	
        internal static void NumberConversion()
        {
            System.Security.Cryptography.RNGCryptoServiceProvider random = new System.Security.Cryptography.RNGCryptoServiceProvider();

				
            for(int i = 0; i < 100000; i++)
            {
				

                byte[] eightBytes = new byte[8];

                random.GetBytes(eightBytes);

                UInt64 num = BitConverter.ToUInt3264(eightBytes, 0);

                eightBytes = BitConverter.GetBytes(num);

                UInt64 check = BitConverter.ToUInt3264(eightBytes, 0);

                if(num != check)
                {
                    int x = 0; 
                    x++;
                }
            }

            int r = 0; 
            r++;
        }	
    */
    }
}

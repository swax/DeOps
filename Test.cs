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
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;

using DeOps.Components.Storage;
using DeOps.Implementation;
using DeOps.Implementation.Protocol;

namespace DeOps
{
    class TestFile
    {
        byte[] Hash = new byte[50];
    }

	/// <summary>
	/// Summary description for Tests.
	/// </summary>
	internal class Test
	{
		internal Test()
        {

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

            if( protocol.ReadPayload(undefPacket) )
            {
                string test = stringEnc.GetString(readStream, undefPacket.PayloadPos, undefPacket.PayloadSize);
                test += " ";
            }

            protocol.ResetPacket(undefPacket);

            G2Header childPacket = new G2Header(readStream);
            G2ReadResult childStatus = G2ReadResult.PACKET_GOOD;

            while( childStatus == G2ReadResult.PACKET_GOOD )
            {
                childStatus = protocol.ReadNextChild( undefPacket, childPacket );

                if( childStatus != G2ReadResult.PACKET_GOOD )
                    continue;

                if( childPacket.Name == "/Dogs/Pug")
                    if( protocol.ReadPayload(childPacket) && childPacket.PayloadSize != 0)
                    {
                        string test = stringEnc.GetString(readStream, childPacket.PayloadPos, childPacket.PayloadSize);
                        test += " ";
                    }

                if( childPacket.Name == "/Dogs/Rapper")
                    if( protocol.ReadPayload(childPacket) && childPacket.PayloadSize != 0)
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

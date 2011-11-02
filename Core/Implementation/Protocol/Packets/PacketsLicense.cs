using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DeOps.Implementation.Protocol.Packets
{
    public class LicensePacket
    {
        public const byte Full = 0x10;
        public const byte Light = 0x20;
    }

    public class FullLicense : G2Packet
    {
        const byte Packet_Signature = 0x10;

        const byte Packet_LicenseID = 0x20;
        const byte Packet_Name      = 0x30;
        const byte Packet_Email     = 0x40;
        const byte Packet_Address   = 0x50;
        const byte Packet_Receipt   = 0x60;
        const byte Packet_Date      = 0x70;
        const byte Packet_Index     = 0x80;

        // public
        public ulong LicenseID;
        public string Name = "";
        public string Email = "";
        public string Address = "";
        public DateTime Date;
        public int Index;

        // private
        public bool SaveExtra = false;
        public string Receipt = "";

        public byte[] Embedded;
        public byte[] Signature;


#if DEBUG
        public void Sign(G2Protocol protocol, RSACryptoServiceProvider JMG_KEY)
        {
            lock (protocol.WriteSection)
            {
                G2Frame cust = protocol.WritePacket(null, 1, null);

                protocol.WritePacket(cust, Packet_LicenseID, BitConverter.GetBytes(LicenseID));
                protocol.WritePacket(cust, Packet_Name, UTF8Encoding.UTF8.GetBytes(Name));
                protocol.WritePacket(cust, Packet_Email, UTF8Encoding.UTF8.GetBytes(Email));
                protocol.WritePacket(cust, Packet_Address, UTF8Encoding.UTF8.GetBytes(Address));    
                protocol.WritePacket(cust, Packet_Date, CompactNum.GetBytes(Date.ToBinary()));
                protocol.WritePacket(cust, Packet_Index, BitConverter.GetBytes(Index));

                Embedded = protocol.WriteFinish();
            }

            Signature = JMG_KEY.SignData(Embedded, new SHA1CryptoServiceProvider());
        }
#endif

        public override byte[] Encode(G2Protocol protocol)
        {
            Debug.Assert(Signature != null);
            Debug.Assert(Embedded != null);

            lock (protocol.WriteSection)
            {
                G2Frame ouside = protocol.WritePacket(null, LicensePacket.Full, Embedded);

                protocol.WritePacket(ouside, Packet_Signature, Signature);

                if(SaveExtra)
                    protocol.WritePacket(ouside, Packet_Receipt, UTF8Encoding.UTF8.GetBytes(Receipt));

                return protocol.WriteFinish();
            }
        }

        public static FullLicense Decode(G2Header root)
        {
            // public key
            RSACryptoServiceProvider JMG_KEY = new RSACryptoServiceProvider();
            JMG_KEY.FromXmlString("<RSAKeyValue><Modulus>tE2IUcTiZLCWvg7FwRdw9b12PvOJscfIM9wFMEByN8D+cKbtYI7YHdNFWN4rt0ZRrlcT/V07WhdNajSMgte2kBxqoL7rZ4vJ9fkL5xQrgSwaX80EGQIyyVGL1Y0W06AcJtIlH1VRok0SeVXr6NCE+HEVjRQmn3Npo9qCLzPeUzk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>");


            FullLicense info = new FullLicense();

            if (G2Protocol.ReadPayload(root))
                info.Embedded = Utilities.ExtractBytes(root.Data, root.PayloadPos, root.PayloadSize);

            G2Protocol.ResetPacket(root);

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Signature:
                        info.Signature = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Receipt:
                        info.Receipt = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            // verify signature
            byte[] jmgPublic = JMG_KEY.ExportParameters(false).Modulus;
            if (!Utilities.CheckSignedData(jmgPublic, info.Embedded, info.Signature))
                return null;


            root = new G2Header(info.Embedded);
            if (!G2Protocol.ReadPacket(root))
                return null;

            child = new G2Header(info.Embedded);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_LicenseID:
                        info.LicenseID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Name:
                        info.Name = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Email:
                        info.Email = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Address:
                        info.Address = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Date:
                        info.Date = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_Index:
                        info.Index = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;
                }
            }

            return info;
        }

    }




    public class LightLicense : G2Packet
    {
        const byte Packet_Signature = 0x10;

        const byte Packet_Name = 0x20;
        const byte Packet_LicenseID = 0x30;
        const byte Packet_Index = 0x40;
        const byte Packet_Date = 0x50;


        public string Name;
        public ulong LicenseID;
        public int Index;
        public DateTime Date;


        public byte[] Embedded;
        public byte[] Signature;

        public LightLicense() { }

        public LightLicense(FullLicense full) 
        {
            Name = full.Name;
            LicenseID = full.LicenseID;
            Index = full.Index;
            Date = full.Date;
        }

#if DEBUG
        public void Sign(G2Protocol protocol, RSACryptoServiceProvider JMG_KEY)
        {
            lock (protocol.WriteSection)
            {
                G2Frame inside = protocol.WritePacket(null, 1, null);

                protocol.WritePacket(inside, Packet_Name, UTF8Encoding.UTF8.GetBytes(Name));
                protocol.WritePacket(inside, Packet_LicenseID, BitConverter.GetBytes(LicenseID));
                protocol.WritePacket(inside, Packet_Index, BitConverter.GetBytes(Index));
                protocol.WritePacket(inside, Packet_Date, CompactNum.GetBytes(Date.ToBinary()));

                Embedded = protocol.WriteFinish();
            }

            Signature = JMG_KEY.SignData(Embedded, new SHA1CryptoServiceProvider());
        }
#endif

        public override byte[] Encode(G2Protocol protocol)
        {
            Debug.Assert(Signature != null);
            Debug.Assert(Embedded != null);

            lock (protocol.WriteSection)
            {
                G2Frame ouside = protocol.WritePacket(null, LicensePacket.Light, Embedded);

                protocol.WritePacket(ouside, Packet_Signature, Signature);

                return protocol.WriteFinish();
            }
        }

        public static LightLicense Decode(byte[] data)
        {
            G2Header root = new G2Header(data);
            if (G2Protocol.ReadPacket(root))
                return Decode(root);

            return null;
        }

        public static LightLicense Decode(G2Header root)
        {
            // public key
            RSACryptoServiceProvider JMG_KEY = new RSACryptoServiceProvider();
            JMG_KEY.FromXmlString("<RSAKeyValue><Modulus>tE2IUcTiZLCWvg7FwRdw9b12PvOJscfIM9wFMEByN8D+cKbtYI7YHdNFWN4rt0ZRrlcT/V07WhdNajSMgte2kBxqoL7rZ4vJ9fkL5xQrgSwaX80EGQIyyVGL1Y0W06AcJtIlH1VRok0SeVXr6NCE+HEVjRQmn3Npo9qCLzPeUzk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>");


            LightLicense info = new LightLicense();

            if (G2Protocol.ReadPayload(root))
                info.Embedded = Utilities.ExtractBytes(root.Data, root.PayloadPos, root.PayloadSize);

            G2Protocol.ResetPacket(root);

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
                if (G2Protocol.ReadPayload(child))
                    if (child.Name == Packet_Signature)
                        info.Signature = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);


            // verify signature
            byte[] jmgPublic = JMG_KEY.ExportParameters(false).Modulus;
            if (!Utilities.CheckSignedData(jmgPublic, info.Embedded, info.Signature))
                return null;


            root = new G2Header(info.Embedded);
            if (!G2Protocol.ReadPacket(root))
                return null;

            child = new G2Header(info.Embedded);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Name:
                        info.Name = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_LicenseID:
                        info.LicenseID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Index:
                        info.Index = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Date:
                        info.Date = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;
                }
            }

            return info;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using DeOps.Implementation.Protocol;


namespace DeOps.Services.Trust
{
    public class TrustPacket
    {
        public const byte ProjectData = 0x10;
        public const byte LinkData    = 0x20;
        public const byte UplinkReq   = 0x30;
        public const byte WebCache    = 0x40;
        public const byte Icon        = 0x50;
        public const byte Splash      = 0x60;
    }

    public class ProjectData : G2Packet
    {
        const byte Packet_ID = 0x10;
        const byte Packet_Name = 0x20;
        const byte Packet_UserName = 0x30;


        public uint ID;
        public string Name = "Unknown";
        public string UserName = "Unknown";


        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame project = protocol.WritePacket(null, TrustPacket.ProjectData, null);

                protocol.WritePacket(project, Packet_ID, BitConverter.GetBytes(ID));
                protocol.WritePacket(project, Packet_Name, UTF8Encoding.UTF8.GetBytes(Name));
                protocol.WritePacket(project, Packet_UserName, UTF8Encoding.UTF8.GetBytes(UserName));

                return protocol.WriteFinish();
            }
        }

        public static ProjectData Decode(G2Header root)
        {
            ProjectData project = new ProjectData();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_ID:
                        project.ID = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Name:
                        project.Name = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_UserName:
                        project.UserName = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return project;
        }
    }

    public class LinkData : G2Packet
    {
        const byte Packet_Project = 0x10;
        const byte Packet_Target = 0x20;
        const byte Packet_Uplink = 0x30;
        const byte Packet_Title = 0x40;


        public uint Project;
        public byte[] Target;
        public bool Uplink;
        public string Title;

        public ulong TargetID;

        LinkData()
        {
        }

        // uplink
        public LinkData(uint project, byte[] target)
        {
            Project = project;
            Target = target;
            Uplink = true;
        }

        // downlink
        public LinkData(uint project, byte[] target, string title)
        {
            Project = project;
            Target = target;
            Uplink = false;
            Title = title;
        }

        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame link = protocol.WritePacket(null, TrustPacket.LinkData, null);

                protocol.WritePacket(link, Packet_Project, BitConverter.GetBytes(Project));
                protocol.WritePacket(link, Packet_Target, Target);
                protocol.WritePacket(link, Packet_Uplink, BitConverter.GetBytes(Uplink));

                if(Title != null)
                    protocol.WritePacket(link, Packet_Title, UTF8Encoding.UTF8.GetBytes(Title));

                return protocol.WriteFinish();
            }
        }

        public static LinkData Decode(G2Header root)
        {
            LinkData link = new LinkData();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Project:
                        link.Project = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Target:
                        link.Target = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        link.TargetID = Utilities.KeytoID(link.Target);
                        break;

                    case Packet_Uplink:
                        link.Uplink = BitConverter.ToBoolean(child.Data, child.PayloadPos);
                        break;

                    case Packet_Title:
                        link.Title = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return link;
        }
    }


    public class UplinkRequest : G2Packet
    {
        const byte Packet_ProjectID = 0x10;
        const byte Packet_LinkVersion = 0x20;
        const byte Packet_TargetVersion = 0x30;
        const byte Packet_Key = 0x40;
        const byte Packet_Target = 0x50;


        public uint ProjectID;
        public uint LinkVersion;
        public uint TargetVersion;
        public byte[] Key;
        public byte[] Target;


        public ulong KeyID;
        public ulong TargetID;
        public byte[] Signed;


        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame request = protocol.WritePacket(null, TrustPacket.UplinkReq, null);

                protocol.WritePacket(request, Packet_ProjectID, BitConverter.GetBytes(ProjectID));
                protocol.WritePacket(request, Packet_LinkVersion, BitConverter.GetBytes(LinkVersion));
                protocol.WritePacket(request, Packet_TargetVersion, BitConverter.GetBytes(TargetVersion));
                protocol.WritePacket(request, Packet_Key, Key);
                protocol.WritePacket(request, Packet_Target, Target);

                return protocol.WriteFinish();
            }
        }

        public static UplinkRequest Decode(G2Header root)
        {
            UplinkRequest request = new UplinkRequest();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_ProjectID:
                        request.ProjectID = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_LinkVersion:
                        request.LinkVersion = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_TargetVersion:
                        request.TargetVersion = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Key:
                        request.Key = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        request.KeyID = Utilities.KeytoID(request.Key);
                        break;

                    case Packet_Target:
                        request.Target = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        request.TargetID = Utilities.KeytoID(request.Target);
                        break;
                }
            }

            return request;
        }
    }

}

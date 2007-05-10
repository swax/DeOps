using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using DeOps.Implementation.Protocol;


namespace DeOps.Components.Storage
{
    internal class StoragePacket
    {
        internal const byte Header = 0x10;
        internal const byte Root   = 0x20;
        internal const byte Folder = 0x30;
        internal const byte File   = 0x40;
        internal const byte Pack   = 0x50;
    }

    internal class StorageHeader : G2Packet
    {
        const byte Packet_Key = 0x10;
        const byte Packet_Version = 0x20;
        const byte Packet_FileHash = 0x30;
        const byte Packet_FileSize = 0x40;
        const byte Packet_FileKey = 0x50;


        internal byte[] Key;
        internal uint Version;
        internal byte[] FileHash;
        internal long FileSize;
        internal RijndaelManaged FileKey = new RijndaelManaged();

        internal ulong KeyID;


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame header = protocol.WritePacket(null, StoragePacket.Header, null);

                protocol.WritePacket(header, Packet_Key, Key);
                protocol.WritePacket(header, Packet_Version, BitConverter.GetBytes(Version));
                protocol.WritePacket(header, Packet_FileHash, FileHash);
                protocol.WritePacket(header, Packet_FileSize, BitConverter.GetBytes(FileSize));
                protocol.WritePacket(header, Packet_FileKey, FileKey.Key);

                return protocol.WriteFinish();
            }
        }

        internal static StorageHeader Decode(G2Protocol protocol, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!protocol.ReadPacket(root))
                return null;

            if (root.Name != StoragePacket.Header)
                return null;

            return StorageHeader.Decode(protocol, root);
        }

        internal static StorageHeader Decode(G2Protocol protocol, G2Header root)
        {
            StorageHeader header = new StorageHeader();
            G2Header child = new G2Header(root.Data);

            while (protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Key:
                        header.Key = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        header.KeyID = Utilities.KeytoID(header.Key);
                        break;

                    case Packet_Version:
                        header.Version = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_FileHash:
                        header.FileHash = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_FileSize:
                        header.FileSize = BitConverter.ToInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_FileKey:
                        header.FileKey.Key = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        header.FileKey.IV = new byte[header.FileKey.IV.Length];
                        break;
                }
            }

            return header;
        }
    }
    internal class StorageRoot : G2Packet
    {
        const byte Packet_Project = 0x10;

        internal uint ProjectID;

        internal StorageRoot() { }

        internal StorageRoot(uint project) { ProjectID = project; }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame root = protocol.WritePacket(null, StoragePacket.Root, null);

                protocol.WritePacket(root, Packet_Project, BitConverter.GetBytes(ProjectID));

                return protocol.WriteFinish();
            }
        }

        internal static StorageRoot Decode(G2Protocol protocol, G2Header header)
        {
            StorageRoot root = new StorageRoot();
            G2Header child = new G2Header(header.Data);

            while (protocol.ReadNextChild(header, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Project:
                        root.ProjectID = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;
                }
            }

            return root;
        }
    }

    [Flags]
    enum StorageFlags : ushort { None = 0, Archived = 1, Unlocked = 2, Modified = 4 }

    [Flags]
    enum StorageActions { None = 0, Created = 1, Modified = 2, Renamed = 4, Deleted = 8, Restored = 16}

    internal class StorageItem : G2Packet
    {
        internal ulong UID;
        internal string Name;
        internal DateTime Date;

        internal ulong Integrated;
        internal StorageFlags Flags;
        internal string Note;

        internal byte Revs;

        internal bool IsFlagged(StorageFlags test)
        {
            return ((Flags & test) != 0);
        }

        internal void RemoveFlag(StorageFlags remove)
        {
            Flags = Flags & ~remove;
        }

        internal void SetFlag(StorageFlags set)
        {
            Flags = Flags | set;
        }
    }

    internal class StorageFolder : StorageItem
    {
        const byte Packet_UID = 0x10;
        const byte Packet_ParentUID = 0x20;
        const byte Packet_Name = 0x30;
        const byte Packet_Date = 0x40;
        const byte Packet_Flags = 0x50;
        const byte Packet_Note = 0x70;
        const byte Packet_Revs = 0x90;
        const byte Packet_Integrated = 0xA0;

        internal ulong ParentUID;


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame folder = protocol.WritePacket(null, StoragePacket.Folder, null);

                protocol.WritePacket(folder, Packet_UID, BitConverter.GetBytes(UID));
                protocol.WritePacket(folder, Packet_ParentUID, BitConverter.GetBytes(ParentUID));
                protocol.WritePacket(folder, Packet_Name, protocol.UTF.GetBytes(Name));
                protocol.WritePacket(folder, Packet_Date, BitConverter.GetBytes(Date.ToBinary()));

                StorageFlags netFlags = Flags & ~(StorageFlags.Modified | StorageFlags.Unlocked);
                protocol.WritePacket(folder, Packet_Flags, BitConverter.GetBytes((ushort)netFlags));

                if(Note != null)
                    protocol.WritePacket(folder, Packet_Note, protocol.UTF.GetBytes(Note));

                protocol.WritePacket(folder, Packet_Revs, BitConverter.GetBytes(Revs));
                protocol.WritePacket(folder, Packet_Integrated, BitConverter.GetBytes(Integrated));

                return protocol.WriteFinish();
            }
        }

        internal static StorageFolder Decode(G2Protocol protocol, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!protocol.ReadPacket(root))
                return null;

            if (root.Name != StoragePacket.Folder)
                return null;

            return StorageFolder.Decode(protocol, root);
        }

        internal static StorageFolder Decode(G2Protocol protocol, G2Header root)
        {
            StorageFolder folder = new StorageFolder();
            G2Header child = new G2Header(root.Data);

            while (protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_UID:
                        folder.UID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_ParentUID:
                        folder.ParentUID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Name:
                        folder.Name = protocol.UTF.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Date:
                        folder.Date = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_Flags:
                        folder.Flags = (StorageFlags) BitConverter.ToUInt16(child.Data, child.PayloadPos);
                        break;

                    case Packet_Note:
                        folder.Note = protocol.UTF.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Revs:
                        folder.Revs = child.Data[child.PayloadPos];
                        break;

                    case Packet_Integrated:
                        folder.Integrated = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;
                }
            }

            return folder;
        }
    }

    internal class StorageFile : StorageItem
    {
        const byte Packet_UID = 0x10;
        const byte Packet_Name = 0x20;
        const byte Packet_Date = 0x30;
        const byte Packet_Flags = 0x40;
        const byte Packet_Note = 0x60;
        const byte Packet_Size = 0x70;
        const byte Packet_Hash = 0x80;
        const byte Packet_FileKey = 0x90;
        const byte Packet_InternalSize = 0xA0;
        const byte Packet_InternalHash = 0xB0;
        const byte Packet_Revs = 0xD0;
        const byte Packet_Integrated = 0xE0;

        internal long Size;
        internal byte[] Hash;
        internal ulong HashID;
        internal RijndaelManaged FileKey = new RijndaelManaged();

        // serves as lookup, so multiple files with same hash aren't duplicated, 
        // especailly since people moving them all around wiould increase the chance of a new key being created for them 
        internal long InternalSize;
        internal byte[] InternalHash;
        internal ulong InternalHashID;

        internal StorageFile()
        {
            FileKey.IV = new byte[FileKey.IV.Length];
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame file = protocol.WritePacket(null, StoragePacket.File, null);

                protocol.WritePacket(file, Packet_UID, BitConverter.GetBytes(UID));
                protocol.WritePacket(file, Packet_Name, protocol.UTF.GetBytes(Name));
                protocol.WritePacket(file, Packet_Date, BitConverter.GetBytes(Date.ToBinary()));

                StorageFlags netFlags = Flags & ~(StorageFlags.Modified | StorageFlags.Unlocked);
                protocol.WritePacket(file, Packet_Flags, BitConverter.GetBytes((ushort)netFlags));
                protocol.WritePacket(file, Packet_Revs, BitConverter.GetBytes(Revs));
                protocol.WritePacket(file, Packet_Integrated, BitConverter.GetBytes(Integrated));

                protocol.WritePacket(file, Packet_Size, BitConverter.GetBytes(Size));
                protocol.WritePacket(file, Packet_Hash, Hash);
                protocol.WritePacket(file, Packet_FileKey, FileKey.Key);
                protocol.WritePacket(file, Packet_InternalSize, BitConverter.GetBytes(InternalSize));
                protocol.WritePacket(file, Packet_InternalHash, InternalHash);

                if (Note != null)
                    protocol.WritePacket(file, Packet_Note, protocol.UTF.GetBytes(Note));

                return protocol.WriteFinish();
            }
        }

        internal static StorageFile Decode(G2Protocol protocol, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!protocol.ReadPacket(root))
                return null;

            if (root.Name != StoragePacket.File)
                return null;

            return StorageFile.Decode(protocol, root);
        }

        internal static StorageFile Decode(G2Protocol protocol, G2Header root)
        {
            StorageFile file = new StorageFile();
            G2Header child = new G2Header(root.Data);

            while (protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_UID:
                        file.UID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Name:
                        file.Name = protocol.UTF.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Date:
                        file.Date = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_Flags:
                        file.Flags = (StorageFlags) BitConverter.ToUInt16(child.Data, child.PayloadPos);
                        break;

                    case Packet_Note:
                        file.Note = protocol.UTF.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Revs:
                        file.Revs = child.Data[child.PayloadPos];
                        break;

                    case Packet_Size:
                        file.Size = BitConverter.ToInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Hash:
                        file.Hash = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        file.HashID = BitConverter.ToUInt64(file.Hash, 0);
                        break;

                    case Packet_FileKey:
                        file.FileKey.Key = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        file.FileKey.IV = new byte[file.FileKey.IV.Length];
                        break;

                    case Packet_InternalSize:
                        file.InternalSize = BitConverter.ToInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_InternalHash:
                        file.InternalHash = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        file.InternalHashID = BitConverter.ToUInt64(file.InternalHash, 0);
                        break;

                    case Packet_Integrated:
                        file.Integrated = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;
                }
            }

            return file;
        }

        internal StorageFile Clone()
        {
            StorageFile clone = new StorageFile();

            clone.UID = UID;
            clone.Name = Name;
            clone.Date = Date;
            clone.Flags = Flags;
            clone.Revs = Revs;

            clone.Size = Size;
            clone.Hash = Hash;
            clone.HashID = HashID;
            clone.FileKey = FileKey;

            clone.InternalSize = InternalSize;
            clone.InternalHash = InternalHash;
            clone.InternalHashID = InternalHashID;

            return clone;
        }
    }
}

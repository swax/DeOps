using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using RiseOp.Implementation.Protocol;


namespace RiseOp.Services.Storage
{
    internal class StoragePacket
    {
        internal const byte Root   = 0x20;
        internal const byte Folder = 0x30;
        internal const byte File   = 0x40;
        internal const byte Pack   = 0x50;
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

        internal static StorageRoot Decode(G2Header header)
        {
            StorageRoot root = new StorageRoot();
            G2Header child = new G2Header(header.Data);

            while (G2Protocol.ReadNextChild(header, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
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
    enum StorageActions { None = 0, Created = 1, Modified = 2, Renamed = 4, Deleted = 8, Restored = 16, Scoped = 32}

    internal class StorageItem : G2Packet
    {
        internal ulong UID;
        internal string Name;
        internal DateTime Date;

        internal ulong IntegratedID;
        internal StorageFlags Flags;
        internal string Note;

        internal byte Revs;

        internal Dictionary<ulong, short> Scope = new Dictionary<ulong, short>();


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
        const byte Packet_Scope = 0xB0;

        internal ulong ParentUID;


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame folder = protocol.WritePacket(null, StoragePacket.Folder, null);

                protocol.WritePacket(folder, Packet_UID, BitConverter.GetBytes(UID));
                protocol.WritePacket(folder, Packet_ParentUID, BitConverter.GetBytes(ParentUID));
                protocol.WritePacket(folder, Packet_Name, UTF8Encoding.UTF8.GetBytes(Name));
                protocol.WritePacket(folder, Packet_Date, BitConverter.GetBytes(Date.ToBinary()));

                StorageFlags netFlags = Flags & ~(StorageFlags.Modified | StorageFlags.Unlocked);
                protocol.WritePacket(folder, Packet_Flags, BitConverter.GetBytes((ushort)netFlags));

                if(Note != null)
                    protocol.WritePacket(folder, Packet_Note, UTF8Encoding.UTF8.GetBytes(Note));

                protocol.WritePacket(folder, Packet_Revs, BitConverter.GetBytes(Revs));
                protocol.WritePacket(folder, Packet_Integrated, BitConverter.GetBytes(IntegratedID));

                byte[] scopefield = new byte[10];
                foreach (ulong id in Scope.Keys)
                {
                    BitConverter.GetBytes(id).CopyTo(scopefield, 0);
                    BitConverter.GetBytes(Scope[id]).CopyTo(scopefield, 8);

                    protocol.WritePacket(folder, Packet_Scope, scopefield);
                }

                return protocol.WriteFinish();
            }
        }


        internal static StorageFolder Decode(byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!G2Protocol.ReadPacket(root))
                return null;

            if (root.Name != StoragePacket.Folder)
                return null;

            return StorageFolder.Decode(root);
        }

        internal static StorageFolder Decode(G2Header root)
        {
            StorageFolder folder = new StorageFolder();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
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
                        folder.Name = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Date:
                        folder.Date = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_Flags:
                        folder.Flags = (StorageFlags) BitConverter.ToUInt16(child.Data, child.PayloadPos);
                        break;

                    case Packet_Note:
                        folder.Note = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Revs:
                        folder.Revs = child.Data[child.PayloadPos];
                        break;

                    case Packet_Integrated:
                        folder.IntegratedID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Scope:
                        folder.Scope[BitConverter.ToUInt64(child.Data, child.PayloadPos)] = BitConverter.ToInt16(child.Data, child.PayloadPos + 8);
                        break;
                }
            }

            return folder;
        }

        internal StorageFolder Clone()
        {
            // clones everything except notes


            StorageFolder clone = new StorageFolder();

            clone.ParentUID = ParentUID;
            clone.UID = UID;
            clone.Name = Name;
            clone.Date = Date;
            clone.Flags = Flags;
            clone.Revs = Revs;

            return clone;
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
        const byte Packet_Scope = 0xF0;

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
                protocol.WritePacket(file, Packet_Name, UTF8Encoding.UTF8.GetBytes(Name));
                protocol.WritePacket(file, Packet_Date, BitConverter.GetBytes(Date.ToBinary()));

                StorageFlags netFlags = Flags & ~(StorageFlags.Unlocked); // allow modified for working
                protocol.WritePacket(file, Packet_Flags, BitConverter.GetBytes((ushort)netFlags));
                protocol.WritePacket(file, Packet_Revs, BitConverter.GetBytes(Revs));
                protocol.WritePacket(file, Packet_Integrated, BitConverter.GetBytes(IntegratedID));

                protocol.WritePacket(file, Packet_Size, CompactNum.GetBytes(Size));
                protocol.WritePacket(file, Packet_Hash, Hash);
                protocol.WritePacket(file, Packet_FileKey, FileKey.Key);
                protocol.WritePacket(file, Packet_InternalSize, CompactNum.GetBytes(InternalSize));
                protocol.WritePacket(file, Packet_InternalHash, InternalHash);

                byte[] scopefield = new byte[10];
                foreach (ulong id in Scope.Keys)
                {
                    BitConverter.GetBytes(id).CopyTo(scopefield, 0);
                    BitConverter.GetBytes(Scope[id]).CopyTo(scopefield, 8);

                    protocol.WritePacket(file, Packet_Scope, scopefield);
                }

                if (Note != null)
                    protocol.WritePacket(file, Packet_Note, UTF8Encoding.UTF8.GetBytes(Note));

                return protocol.WriteFinish();
            }
        }

        internal static StorageFile Decode(byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!G2Protocol.ReadPacket(root))
                return null;

            if (root.Name != StoragePacket.File)
                return null;

            return StorageFile.Decode(root);
        }

        internal static StorageFile Decode(G2Header root)
        {
            StorageFile file = new StorageFile();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_UID:
                        file.UID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Name:
                        file.Name = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Date:
                        file.Date = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_Flags:
                        file.Flags = (StorageFlags) BitConverter.ToUInt16(child.Data, child.PayloadPos);
                        break;

                    case Packet_Note:
                        file.Note = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Revs:
                        file.Revs = child.Data[child.PayloadPos];
                        break;

                    case Packet_Size:
                        file.Size = CompactNum.ToInt64(child.Data, child.PayloadPos, child.PayloadSize);
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
                        file.InternalSize = CompactNum.ToInt64(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_InternalHash:
                        file.InternalHash = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        file.InternalHashID = BitConverter.ToUInt64(file.InternalHash, 0);
                        break;

                    case Packet_Integrated:
                        file.IntegratedID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Scope:
                        file.Scope[BitConverter.ToUInt64(child.Data, child.PayloadPos)] = BitConverter.ToInt16(child.Data, child.PayloadPos + 8);
                        break;
                }
            }

            return file;
        }

        internal StorageFile Clone()
        {
            // clones everything except notes

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

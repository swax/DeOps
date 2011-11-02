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

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Protocol.Special;


namespace DeOps
{

    public static partial class Utilities
    {
        public static byte[] GetPasswordKey(string password, byte[] salt)
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

        public static void ShaHashFile(string path, ref byte[] hash, ref long size)
        {
            using (FileStream file = File.OpenRead(path))
            {
                SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider();
                hash = sha.ComputeHash(file);
                size = file.Length;
            }
        }

        public static void Md5HashFile(string path, ref byte[] hash, ref long size)
        {
            using (FileStream file = File.OpenRead(path))
            {
                MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                hash = md5.ComputeHash(file);
                size = file.Length;
            }
        }

        public static void HashTagFile(string path, G2Protocol protocol, ref byte[] hash, ref long size)
        {
            using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
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

        public static string CryptType(object crypt)
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

        public static bool CheckSignedData(byte[] key, byte[] data, byte[] sig)
        {
            // check signature
            RSACryptoServiceProvider rsa = Utilities.KeytoRsa(key);

            return rsa.VerifyData(data, new SHA1CryptoServiceProvider(), sig);
        }

        public static RSACryptoServiceProvider KeytoRsa(byte[] key)
        {
            RSAParameters param = new RSAParameters();
            param.Modulus = key;
            param.Exponent = new byte[] { 1, 0, 1 };

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(param);

            return rsa;
        }

        public static byte[] EncryptBytes(byte[] data, byte[] key)
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

        public static byte[] DecryptBytes(byte[] data, int length, byte[] key)
        {
            RijndaelManaged crypt = new RijndaelManaged();
            crypt.Key = key;
            crypt.IV = Utilities.ExtractBytes(data, 0, crypt.IV.Length);
            crypt.Padding = PaddingMode.PKCS7;

            ICryptoTransform decryptor = crypt.CreateDecryptor();

            return decryptor.TransformFinalBlock(data, crypt.IV.Length, length - crypt.IV.Length);
        }

        public static byte[] GenerateKey(RNGCryptoServiceProvider rnd, int bits)
        {
            byte[] key = new byte[bits / 8];
            rnd.GetBytes(key);
            return key;
        }

        public static byte[] GetSalt(int amount, int buffsize, RNGCryptoServiceProvider rnd)
        {
            byte[] salt = new byte[amount];
            rnd.GetBytes(salt);

            byte[] final = new byte[buffsize];
            salt.CopyTo(final, 0);

            return final;
        }

        public static string CryptFilename(OpCore core, string name)
        {
            // hash, base64 name with ~ instead of /, use for link as well

            byte[] salt = Utilities.ExtractBytes(core.User.Settings.FileKey, 0, 4);

            byte[] final = Utilities.CombineArrays(salt, UTF8Encoding.UTF8.GetBytes(name));

            SHA1Managed sha1 = new SHA1Managed();
            byte[] hash = new SHA1Managed().ComputeHash(final);

            return Utilities.ToBase64String(hash);
        }

        public static string CryptFilename(OpCore core, ulong id, byte[] hash)
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


        public static RijndaelManaged CommonFileKey(byte[] opKey, byte[] internalHash)
        {
            RijndaelManaged crypt = new RijndaelManaged();

            byte[] key = new byte[crypt.Key.Length];

            // file key is opID and public hash xor'd so that files won't be duplicated on the network
            opKey.CopyTo(key, 0);

            // hash is 16 bytes, key is 32
            for (int i = 0; i < crypt.Key.Length; i++)
                key[i] ^= internalHash[i % 16];

            crypt.Key = key;

            // iv needs to be the same for ident files to gen same file hash
            crypt.IV = new MD5CryptoServiceProvider().ComputeHash(crypt.Key);

            return crypt;
        }

        public static void EncryptTagFile(string source, string destination, RijndaelManaged crypt, G2Protocol protocol, ref byte[] hash, ref long size)
        {
            const int bufferSize = 1024 * 16;
            byte[] buffer = new byte[bufferSize];

            using (IVCryptoStream stream = IVCryptoStream.Save(destination, crypt.Key, crypt.IV))
            {
                using (FileStream localfile = File.OpenRead(source))
                {
                    int read = bufferSize;
                    while (read == bufferSize)
                    {
                        read = localfile.Read(buffer, 0, bufferSize);
                        stream.Write(buffer, 0, read);
                    }
                }

                stream.FlushFinalBlock();
            }

            HashTagFile(destination, protocol, ref hash, ref size);
        }

        public static void DecryptTagFile(string source, string destination, byte[] key, OpCore core)
        {
            int bufferSize = 4096;
            byte[] buffer = new byte[4096]; // needs to be 4k to packet stream break/resume work

            string tempPath = (core != null) ? core.GetTempPath() : destination;
            G2Protocol protocol = (core != null) ? core.Network.Protocol : new G2Protocol();

            using (FileStream tempFile = new FileStream(tempPath, FileMode.Create))
            using (TaggedStream encFile = new TaggedStream(source, protocol))
            using (IVCryptoStream stream = IVCryptoStream.Load(encFile, key))
            {
                int read = bufferSize;
                while (read == bufferSize)
                {
                    read = stream.Read(buffer, 0, bufferSize);
                    tempFile.Write(buffer, 0, read);
                }
            }

            // move to official path
            if (core == null)
                return;

            File.Copy(tempPath, destination, true);
            File.Delete(tempPath);
        }
    }
}


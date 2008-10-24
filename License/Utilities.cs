using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RiseOp
{
    internal static partial class Utilities
    {
        internal static bool CheckSignedData(byte[] key, byte[] data, byte[] sig)
        {
            // check signature
            RSACryptoServiceProvider rsa = Utilities.KeytoRsa(key);

            return rsa.VerifyData(data, new SHA1CryptoServiceProvider(), sig);
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

        internal static byte[] ExtractBytes(byte[] buffer, int offset, int length)
        {
            byte[] extracted = new byte[length];

            Buffer.BlockCopy(buffer, offset, extracted, 0, length);

            return extracted;
        }

        internal static byte[] GetPasswordKey(string password, byte[] salt)
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

        internal static byte[] EncryptBytes(byte[] data, byte[] key)
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

        internal static byte[] DecryptBytes(byte[] data, int length, byte[] key)
        {
            RijndaelManaged crypt = new RijndaelManaged();
            crypt.Key = key;
            crypt.IV = Utilities.ExtractBytes(data, 0, crypt.IV.Length);
            crypt.Padding = PaddingMode.PKCS7;

            ICryptoTransform decryptor = crypt.CreateDecryptor();

            return decryptor.TransformFinalBlock(data, crypt.IV.Length, length - crypt.IV.Length);
        }

        internal static ulong RandUInt64(Random rnd)
        {
            byte[] bytes = new byte[8];

            rnd.NextBytes(bytes);

            return BitConverter.ToUInt64(bytes, 0);
        }

        internal static string BytestoHex(byte[] data)
        {
            return Utilities.BytestoHex(data, 0, data.Length, false);
        }

        internal static string BytestoHex(byte[] data, int offset, int size, bool space)
        {
            StringBuilder hex = new StringBuilder();

            for (int i = offset; i < offset + size; i++)
            {
                hex.Append(String.Format("{0:x2}", data[i]));

                if (space)
                    hex.Append(" ");
            }

            return hex.ToString();
        }
    }
}

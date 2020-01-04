using System;
using System.Security.Cryptography;
using System.Text;

namespace Ticketbooth.Scanner.Tests.Extensions
{
    public static class Generate
    {
        private static readonly char[] AlphaNumericCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

        public static string String(int length)
        {
            byte[] data = new byte[4 * length];

            using var crypto = new RNGCryptoServiceProvider();
            crypto.GetBytes(data);

            StringBuilder result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = rnd % AlphaNumericCharacters.Length;

                result.Append(AlphaNumericCharacters[idx]);
            }

            return result.ToString();
        }
    }
}

using System;
using System.Linq;
using System.Text;

namespace Microsoft.ColumnEncryption.Common
{
    public static class Converter
    {
        public static byte[] FromHexString(string hex)
        {
            string hexString = hex.Substring(0, 2).ToLower() == "0x" ? hex.Substring(2, hex.Length - 2) : hex;
            return Enumerable.Range(0, hexString.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
                             .ToArray();
        }

        public static string ToHexString(byte[] bytes)
        {
            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                hex.AppendFormat("{0:x2}", b);
            return "0x" + hex.ToString();
        }
    }
}

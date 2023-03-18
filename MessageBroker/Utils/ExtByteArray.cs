using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MessageBroker.Utils
{
    public static class ExtByteArray
    {
        private static readonly string[] Baths;

        static ExtByteArray()
        {
            Baths = new string[256];
            for (int i = 0; i < 256; i++)
                Baths[i] = $"{i:X2}";
        }

        /// <summary>
        /// Extension, that convert this byte array to hex string
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static string ToHex(this byte[] array)
        {
            StringBuilder builder = new StringBuilder(array.Length * 2);

            for (int i = 0; i < array.Length; i++)
                builder.Append(Baths[array[i]]);

            return builder.ToString();
        }

        public static string ToHex(this string incomingData, Encoding enc = null)
        {
            if (enc == null)
                enc = Encoding.UTF8;

            var arr = enc.GetBytes(incomingData);

            return arr.ToHex();
        }

        public static string ToHex(this long val)
        {
            var arr = BitConverter.GetBytes(val);
            Array.Reverse(arr);
            return arr.ToHex();
        }

        public static string ToHex(this int array)
        {
            StringBuilder stringBuilder = new StringBuilder(8);
            stringBuilder.Append(array.ToString("x8"));
            return stringBuilder.ToString();
        }

        public static string ToHex(this float array)
        {
            var tmp = BitConverter.GetBytes(array);
            Array.Reverse(tmp);
            return tmp.ToHex();
        }

        public static string ToHex(this short val)
        {
            StringBuilder stringBuilder = new StringBuilder(4);
            stringBuilder.Append(val.ToString("x4"));
            return stringBuilder.ToString();
        }

        public static string ToHex(this byte array)
        {
            StringBuilder stringBuilder = new StringBuilder(2);
            stringBuilder.Append(array.ToString("x2"));
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Format byte array to hex text and ansi symbols blocks
        /// </summary>
        /// <param name="data">source array</param>
        /// <param name="bytesPerBlock">bytes per one format block</param>
        /// <param name="blocksPerRow">byte blocks per one row</param>
        /// <returns></returns>
        public static string FormatHex(this byte[] data, int bytesPerBlock = 4, int blocksPerRow = 4)
        {
            StringBuilder builder = new StringBuilder(data.Length * 4);

            int len = data.Length;
            int bytesPerRow = bytesPerBlock * blocksPerRow;

            int lastRowLen = len % bytesPerRow;
            int rows = len / bytesPerRow;

            if (lastRowLen > 0)
                rows++;

            for (int i = 0; i < rows; i++)
            {
                int currentCount = bytesPerRow * i;
                builder.Append("[");
                builder.Append(currentCount);
                builder.Append("]\t");

                int bytesInThisRow = lastRowLen > 0 && rows - 1 == i ? lastRowLen : bytesPerRow;
                for (int k = 0; k < bytesInThisRow; k++)
                {
                    byte res = data[currentCount + k];
                    builder.Append(res.ToString("X2"));

                    if ((k + 1) % bytesPerBlock == 0)
                        builder.Append("  ");
                }

                var diff = bytesPerRow - bytesInThisRow;
                if (diff > 0)
                {

                    var cnt = diff + diff / bytesPerBlock;
                    if (diff % bytesPerBlock > 0)
                        cnt++;

                    for (int k = 0; k < cnt; k++)
                        builder.Append("  ");
                }

                for (int k = 0; k < bytesInThisRow; k++)
                {

                    char res = (char)data[currentCount + k];
                    if (res > 0x1f && res < 0x80)
                        builder.Append(res);
                    else
                        builder.Append(".");
                }

                builder.Append("\n");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Extension, that convert source hex string to byte array
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this string hexString)
        {
            try
            {
                byte[] result = new byte[hexString.Length / 2];

                for (int index = 0; index < result.Length; index++)
                {
                    string byteValue = hexString.Substring(index * 2, 2);
                    result[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }

                return result;
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid hex string: {0}", hexString);
                throw;
            }
        }

        public static BigInteger HexStringToBigInteger(string hex)
        {
            if (hex.Length % 2 > 0)
            {
                hex = "0" + hex;
            }
            var a = hex.ToBytes().Reverse().ToArray();
            var t = a.Concat(new byte[] { 0 }).ToArray();

            return new BigInteger(t);
        }

        public static byte[] LongToByteArrayBigEndian(long val)
        {
            byte[] intBytes = BitConverter.GetBytes(val);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(intBytes);
            return intBytes;
        }


        public static byte[] UIntToByteArrayBigEndian(uint val)
        {
            byte[] intBytes = BitConverter.GetBytes(val);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(intBytes);
            return intBytes;
        }

        public static byte[] IntToByteArrayBigEndian(int val)
        {
            byte[] intBytes = BitConverter.GetBytes(val);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(intBytes);
            return intBytes;
        }

        public static byte[] ShortToByteArrayBigEndian(short val)
        {
            byte[] intBytes = BitConverter.GetBytes(val);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(intBytes);
            return intBytes;
        }


        public static byte[] StringToByteArray(string hex)
        {
            if (hex.Length % 2 > 0)
            {
                hex = "0" + hex;
            }

            var r = Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();

            return r;
        }

        public static string BigIntegerToString(BigInteger bi)
        {
            byte[] bytes = bi.ToByteArray();
            Array.Reverse(bytes);
            string result = BitConverter.ToString(bytes).Replace("-", string.Empty).ToUpper();
            if (result[0] == '0' && result[1] == '0')
                result = result.Substring(2);
            return result;
        }

        public static string RijnaelKeyModify(string key)
        {
            key = key.ToUpper();
            key = key.Replace('A', '1');
            key = key.Replace('B', '2');
            key = key.Replace('C', '3');
            key = key.Replace('D', '4');
            key = key.Replace('E', '5');
            key = key.Replace('F', '6');

            return key;
        }

        public static float SingleFromByteArrayLE(byte[] data, int startIndex)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(data);
            return BitConverter.ToSingle(data, startIndex);
        }

        public static int IntFromByteArrayBE(byte[] data, int startIndex)
        {
            return data[startIndex] << 24
                 | data[startIndex + 1] << 16
                 | data[startIndex + 2] << 8
                 | data[startIndex + 3];
        }

        public static short ShortFromByteArrayBE(byte[] data, int startIndex)
        {
            return (short)(data[startIndex] << 8
                 | data[startIndex + 1]);
        }
    }
}

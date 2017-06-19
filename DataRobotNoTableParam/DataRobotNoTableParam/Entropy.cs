using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataRobotNoTableParam
{
    using System;
    using System.Collections.Generic;

    public class EntropyCalculator
    {
        private static Dictionary<char, int> CharCounter(string input)
        {
            var table = new Dictionary<char, int>();
            foreach (char c in input)
            {
                if (table.ContainsKey(c))
                    table[c]++;
                else
                    table.Add(c, 1);
            }
            return table;
        }
        private static Dictionary<byte, int> ByteCounter(byte[] byteArray)
        {
            var table = new Dictionary<byte, int>();
            foreach (byte c in byteArray)
            {
                if (table.ContainsKey(c))
                    table[c]++;
                else
                    table.Add(c, 1);
            }
            return table;
        }
        private static Dictionary<long, int> LongCounter(long[] longArray)
        {
            var table = new Dictionary<long, int>();
            foreach (long c in longArray)
            {
                if (table.ContainsKey(c))
                    table[c]++;
                else
                    table.Add(c, 1);
            }
            return table;
        }
        public static double Entropy(long[] longArray)
        {
            double infoC = 0, freq;
            var table = LongCounter(longArray);
            foreach (var letter in table)
            {
                freq = (double)letter.Value / longArray.Length;
                infoC += freq * LogTwo(freq);
            }
            infoC *= -1;
            return infoC;
        }
        private static double LogTwo(double num)
        {
            return Math.Log(num) / Math.Log(2);
        }
        public static double Entropy(string input)
        {
            double infoC = 0, freq;
            var table = CharCounter(input);
            foreach (var letter in table)
            {
                freq = (double)letter.Value / input.Length;
                infoC += freq * LogTwo(freq);
            }
            infoC *= -1;
            return infoC;
        }
        public static double Entropy(byte[] byteArray)
        {
            double infoC = 0, freq;
            var table = ByteCounter(byteArray);
            foreach (var letter in table)
            {
                freq = (double)letter.Value / byteArray.Length;
                infoC += freq * LogTwo(freq);
            }
            infoC *= -1;
            return infoC;
        }
        private static double minEncryptedFileEntropy = 7990;
        private static double minZippedFileEntropy = 7600;

        public static void DetectFile(double entropy)
        {
            double ent = entropy * 1000;
            if (ent > minEncryptedFileEntropy)
                Console.WriteLine("I guess the file is Encrypted.");
            else if (ent > minZippedFileEntropy)
                Console.WriteLine("I guess the file is Zipped.");
            else Console.WriteLine("I guess the file is Regular.");
        }
    }
}

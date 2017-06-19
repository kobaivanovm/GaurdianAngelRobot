using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataRobotNoTableParam
{
    public static class EntropyValue
    {
        public static readonly double MaxPossibleEntropyValue = 8;
        public static readonly double GeneralEncryptedEntropyValue = 7.99;
        public static readonly double CompressedEntropyValue = 7.6;
        public static readonly double RegularFileEntropy = 0;
        public static bool IsFileEncrypted(double entropy)
        {
            return (entropy >= GeneralEncryptedEntropyValue);
        }
    }
}

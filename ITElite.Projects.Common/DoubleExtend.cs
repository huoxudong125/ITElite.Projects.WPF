using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITElite.Projects.Common
{
    public static class DoubleExtend
    {
        // Return a string describing the value as a file size.
        // For example, 1.23 MB.
        public static string ToFileSize(this double value)
        {
            string[] suffixes = { "bytes", "KB", "MB", "GB",
        "TB", "PB", "EB", "ZB", "YB"};
            for (int i = 0; i < suffixes.Length; i++)
            {
                if (value <= (Math.Pow(1024, i + 1)))
                {
                    return ThreeNonZeroDigits(value /
                        Math.Pow(1024, i)) +
                        " " + suffixes[i];
                }
            }

            return ThreeNonZeroDigits(value /
                Math.Pow(1024, suffixes.Length - 1)) +
                " " + suffixes[suffixes.Length - 1];
        }

        // Return a string describing the value as a resolution thi value unit is m.
        // For example, 1.23 nm.
        public static string ToLengthSize(this double value)
        {
            string[] suffixes = { "nm", "μm", "mm", "m", "km" };
            for (int i = 0; i < suffixes.Length; i++)
            {
                if (value <= (Math.Pow(1000, i - 2)))
                {
                    return ThreeNonZeroDigits(value /
                        Math.Pow(1000, i - 3)) +
                        " " + suffixes[i];
                }
            }

            return ThreeNonZeroDigits(value /
                Math.Pow(1000, 1)) +
                " " + suffixes[suffixes.Length - 1];//output km
        }

        // Return the value formatted to include at most three
        // non-zero digits and at most two digits after the
        // decimal point. Examples:
        //         1
        //       123
        //        12.3
        //         1.23
        //         0.12
        private static string ThreeNonZeroDigits(double value)
        {
            if (value >= 100)
            {
                // No digits after the decimal.
                return value.ToString("0,0");
            }
            else if (value >= 10)
            {
                // One digit after the decimal.
                return value.ToString("0.0");
            }
            else
            {
                // Two digits after the decimal.
                return value.ToString("0.00");
            }
        }
    }
}

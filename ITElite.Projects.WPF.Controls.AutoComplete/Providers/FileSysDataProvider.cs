using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace ITElite.Projects.WPF.Controls.AutoComplete.Providers
{
    public class FileSysDataProvider : IAutoCompleteDataProvider, IAutoAppendDataProvider
    {
        private const short INVALID_HANDLE_VALUE = -1;
        //
        public bool IncludeFiles { get; set; }

        public string GetAppendText(string textPattern, string firstMatch)
        {
            if (textPattern.EndsWith("\\"))
            {
                return null;
            }
            return firstMatch.Substring(textPattern.Length);
        }

        public IEnumerable<object> GetItems(string textPattern, int maxResults)
        {
            if (textPattern.Length < 2 || textPattern[1] != ':')
            {
                yield break;
            }
            var lastSlashPos = textPattern.LastIndexOf('\\');
            if (lastSlashPos == -1)
            {
                yield break;
            }
            var fileNamePatternLength = textPattern.Length - lastSlashPos - 1;
            var baseFolder = textPattern.Substring(0, lastSlashPos + 1);
            WIN32_FIND_DATA fd;
            var hFind = FindFirstFile(textPattern + "*", out fd);
            if (hFind.ToInt32() == INVALID_HANDLE_VALUE)
            {
                yield break;
            }
            do
            {
                if (fd.cFileName[0] == '.')
                {
                    continue;
                }
                if ((fd.dwFileAttributes & FileAttributes.Hidden) != 0)
                {
                    continue;
                }
                if ((fd.dwFileAttributes & FileAttributes.Directory) == 0 && !IncludeFiles)
                {
                    continue;
                }
                if (fileNamePatternLength > fd.cFileName.Length)
                {
                    continue;
                }
                yield return baseFolder + fd.cFileName;
            } while (FindNextFile(hFind, out fd));
            FindClose(hFind);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll")]
        public static extern bool FindClose(IntPtr hFindFile);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct WIN32_FIND_DATA
        {
            public readonly FileAttributes dwFileAttributes;
            public readonly FILETIME ftCreationTime;
            public readonly FILETIME ftLastAccessTime;
            public readonly FILETIME ftLastWriteTime;
            public readonly uint nFileSizeHigh;
            public readonly uint nFileSizeLow;
            public readonly uint dwReserved0;
            public readonly uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public readonly string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] public readonly string cAlternateFileName;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ITElite.Projects.WPF.Controls.AutoComplete.Providers
{
    public class UrlHistoryDataProvider : IAutoCompleteDataProvider, IAutoAppendDataProvider
    {
        private const int ERROR_NO_MORE_ITEMS = 0x103;
        private const int ERROR_INSUFFICIENT_BUFFER = 122;
        private List<string> _historyUrls;

        public string GetAppendText(string textPattern, string firstMatch)
        {
            if (!textPattern.StartsWith("http://"))
            {
                textPattern = "http://" + textPattern;
            }
            if (firstMatch.IndexOf(textPattern, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return null;
            }
            var result = firstMatch.Substring(textPattern.Length);
            var slashPos = result.IndexOf('/');
            if (slashPos != -1)
            {
                result = result.Substring(0, slashPos + 1);
            }
            return result;
        }

        public IEnumerable<object> GetItems(string textPattern, int maxResults)
        {
            if (_historyUrls == null)
            {
                lock (this)
                {
                    _historyUrls = LoadHistoryUrls();
                }
            }

            if ("http://".IndexOf(textPattern) == 0)
            {
                return _historyUrls;
            }
            var pattern = textPattern;

            if (!pattern.StartsWith("http://"))
            {
                pattern = "http://" + textPattern;
            }

            var result = new List<object>();

            foreach (var url in _historyUrls)
            {
                if (url.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(url);
                }
            }
            if (!textPattern.StartsWith("http://") && !textPattern.StartsWith("www."))
            {
                pattern = "http://www." + textPattern;
                result.AddRange(GetItems(pattern, maxResults));
            }
            return result;
        }

        //
        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr FindFirstUrlCacheEntry(string lpszUrlSearchPattern,
            IntPtr lpFirstCacheEntryInfo,
            ref int lpdwFirstCacheEntryInfoBufferSize);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern bool FindNextUrlCacheEntry(IntPtr hEnumHandle,
            IntPtr lpNextCacheEntryInfo,
            ref int lpdwNextCacheEntryInfoBufferSize);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern long FindCloseUrlCache(IntPtr hEnumHandle);

        public List<string> LoadHistoryUrls()
        {
            var result = new List<string>();
            var cb = 0;
            const string pattern = "visited:";
            FindFirstUrlCacheEntry(pattern, IntPtr.Zero, ref cb);

            var buf = Marshal.AllocHGlobal(cb);
            try
            {
                var hFind = FindFirstUrlCacheEntry(pattern, buf, ref cb);

                while (true)
                {
                    var pSourceUrl = Marshal.ReadIntPtr(buf, 4);
                    var url = Marshal.PtrToStringAnsi(pSourceUrl);
                    var atPos = url.IndexOf("@");
                    if (atPos != -1)
                    {
                        url = url.Substring(atPos + 1);
                    }
                    if (url.StartsWith("http://"))
                    {
                        result.Add(url);
                    }

                    var retval = FindNextUrlCacheEntry(hFind, buf, ref cb);
                    if (!retval)
                    {
                        var win32Err = Marshal.GetLastWin32Error();
                        if (win32Err == ERROR_NO_MORE_ITEMS)
                        {
                            break;
                        }
                        if (win32Err == ERROR_INSUFFICIENT_BUFFER)
                        {
                            buf = Marshal.ReAllocHGlobal(buf, new IntPtr(cb));
                            FindNextUrlCacheEntry(hFind, buf, ref cb);
                        }
                    }
                }
                FindCloseUrlCache(hFind);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
            return result;
        }
    }
}
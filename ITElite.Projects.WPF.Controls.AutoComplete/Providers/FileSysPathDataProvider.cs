using System.Collections.Generic;
using System.IO;

namespace ITElite.Projects.WPF.Controls.AutoComplete.Providers
{
    public class FileSysPathDataProvider : IAutoCompleteDataProvider
    {
        public IEnumerable<object> GetItems(string textPattern, int maxResults)
        {
            if (textPattern.Length < 2 || textPattern[1] != ':')
            {
                return null;
            }
            var lastSlashPos = textPattern.LastIndexOf('\\');
            var baseFolder = textPattern;
            string partialMatch = null;
            if (lastSlashPos != -1)
            {
                baseFolder = textPattern.Substring(0, lastSlashPos);
                partialMatch = textPattern.Substring(lastSlashPos + 1);
            }
            try
            {
                return Directory.GetDirectories(baseFolder + '\\', partialMatch + "*");
            }
            catch
            {
                return null;
            }
        }
    }
}
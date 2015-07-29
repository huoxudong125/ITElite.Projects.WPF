using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace ITElite.Projects.WPF.Controls.AutoComplete.Providers
{
    public class RegistryDataProvider : IAutoCompleteDataProvider
    {
        private readonly List<RegistryKey> _rootKeys = new List<RegistryKey>
        {
            Registry.ClassesRoot,
            Registry.CurrentUser,
            Registry.LocalMachine,
            Registry.Users,
            Registry.CurrentConfig
        };

        public IEnumerable<object> GetItems(string textPattern, int maxResults)
        {
            var result = new List<string>();
            var parts = textPattern.Split('\\');
            var rootStr = parts[0];

            if (parts.Length == 1)
            {
                foreach (var key in _rootKeys)
                {
                    if (key.Name.StartsWith(rootStr, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(key.Name);
                    }
                }
                return result;
            }
            RegistryKey rootKey = null;
            foreach (var key in _rootKeys)
            {
                if (key.Name.Equals(rootStr, StringComparison.OrdinalIgnoreCase))
                {
                    rootKey = key;
                    break;
                }
            }
            if (rootKey == null)
            {
                return null;
            }
            var subKey = rootKey;
            var sb = new StringBuilder();
            for (var n = 1; n < parts.Length - 1; n++)
            {
                sb.Append(parts[n]).Append('\\');
            }
            var middlePath = sb.ToString();
            if (middlePath.Length != 0)
            {
                try
                {
                    subKey = subKey.OpenSubKey(middlePath);
                }
                catch
                {
                    // SecurityException may be thrown
                    return result;
                }
            }
            var lastStr = parts[parts.Length - 1];
            var subKeyNames = subKey.GetSubKeyNames();

            foreach (var subKeyName in subKeyNames)
            {
                if (subKeyName.StartsWith(lastStr, StringComparison.OrdinalIgnoreCase))
                {
                    sb.Length = 0;
                    sb.Append(rootKey.Name).Append('\\').Append(middlePath).Append(subKeyName);
                    result.Add(sb.ToString());
                }
            }
            subKey.Close();
            return result;
        }
    }
}
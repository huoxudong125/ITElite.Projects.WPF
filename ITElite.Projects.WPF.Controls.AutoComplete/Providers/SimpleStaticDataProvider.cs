using System;
using System.Collections.Generic;

namespace ITElite.Projects.WPF.Controls.AutoComplete.Providers
{
    public class SimpleStaticDataProvider : IAutoCompleteDataProvider
    {
        private readonly IEnumerable<string> _source;

        public SimpleStaticDataProvider(IEnumerable<string> source)
        {
            _source = source;
        }

        public IEnumerable<object> GetItems(string textPattern, int maxResults)
        {
            foreach (var item in _source)
            {
                if (item.StartsWith(textPattern, StringComparison.OrdinalIgnoreCase))
                {
                    yield return item;
                }
            }
        }
    }
}
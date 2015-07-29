using System.Collections.Generic;

namespace ITElite.Projects.WPF.Controls.AutoComplete.Providers
{
    public interface IAutoCompleteDataProvider
    {
        IEnumerable<object> GetItems(string textPattern, int maxResults);
    }
}
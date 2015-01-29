namespace ITElite.Projects.WPF.Controls.AutoComplete.Providers
{
    public interface IAutoAppendDataProvider
    {
        string GetAppendText(string textPattern, string firstMatch);
    }
}

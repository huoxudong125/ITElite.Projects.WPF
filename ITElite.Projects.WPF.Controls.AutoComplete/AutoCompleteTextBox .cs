using System.Windows;
using System.Windows.Controls;
using ITElite.Projects.WPF.Controls.AutoComplete.Core;
using ITElite.Projects.WPF.Controls.AutoComplete.Providers;

namespace ITElite.Projects.WPF.Controls.AutoComplete
{
    /// <summary>
    ///     Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///     Step 1a) Using this custom control in a XAML file that exists in the current project.
    ///     Add this XmlNamespace attribute to the root element of the markup file where it is
    ///     to be used:
    ///     xmlns:MyNamespace="clr-namespace:ITElite.Projects.WPF.Controls.AutoComplete"
    ///     Step 1b) Using this custom control in a XAML file that exists in a different project.
    ///     Add this XmlNamespace attribute to the root element of the markup file where it is
    ///     to be used:
    ///     xmlns:MyNamespace="clr-namespace:ITElite.Projects.WPF.Controls.AutoComplete;assembly=ITElite.Projects.WPF.Controls.AutoComplete"
    ///     You will also need to add a project reference from the project where the XAML file lives
    ///     to this project and Rebuild to avoid compilation errors:
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Select this project]
    ///     Step 2)
    ///     Go ahead and use your control in the XAML file.
    ///     <MyNamespace:CustomControl1 />
    /// </summary>
    public class AutoCompleteTextBox : TextBox
    {
        public AutoCompleteTextBox()
        {
            AutoCompleteManager = new AutoCompleteManager();
            AutoCompleteManager.DataProvider = new FileSysDataProvider();
            Loaded += AutoCompleteTextBox_Loaded;
        }

        public AutoCompleteManager AutoCompleteManager { get; private set; }

        private void AutoCompleteTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            AutoCompleteManager.AttachTextBox(this);
        }
    }
}
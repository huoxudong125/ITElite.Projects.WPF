using System;

namespace ITElite.Projects.WPF.Controls.AutoComplete.Core
{
    /// <summary>
    ///     This class is used to interop with Windows Forms. Skip this if you're writnig pure WPF app.
    ///     For example, you have a WPF UserControl hosted in a Widnows Form (via ElementHost).
    ///     In this case, you will need to intercept that form's WndProc and call this method in AutoCompleteManager.
    /// </summary>
    public interface IWndProcHandler
    {
        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);
    }
}
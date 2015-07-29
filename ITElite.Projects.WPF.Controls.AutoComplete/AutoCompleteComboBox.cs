using System;
using System.Windows.Controls;
using System.Windows.Input;
using ITElite.Projects.WPF.Controls.AutoComplete.Core;

namespace ITElite.Projects.WPF.Controls.AutoComplete
{
    public class AutoCompleteComboBox : ComboBox
    {
        private int _oldSelLength;
        private int _oldSelStart;
        private string _oldText;
        private TextBox _textBox;

        public AutoCompleteComboBox()
        {
            IsEditable = true;
            IsTextSearchEnabled = false;
            GotMouseCapture += AutoCompleteComboBox_GotMouseCapture;

            AutoCompleteManager = new AutoCompleteManager();
        }

        public AutoCompleteManager AutoCompleteManager { get; private set; }

        private void AutoCompleteComboBox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            _oldSelStart = _textBox.SelectionStart;
            _oldSelLength = _textBox.SelectionLength;
            _oldText = _textBox.Text;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (AutoCompleteManager.AutoCompleting)
            {
                return;
            }
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                SelectedValue = Text;
            }
            base.OnPreviewKeyDown(e);
        }

        protected override void OnDropDownOpened(EventArgs e)
        {
            AutoCompleteManager.Disabled = true;
            IsTextSearchEnabled = true;
            SelectedValue = Text;

            base.OnDropDownOpened(e);

            if (SelectedValue == null)
            {
                Text = _oldText;
                _textBox.SelectionStart = _oldSelStart;
                _textBox.SelectionLength = _oldSelLength;
            }
        }

        protected override void OnDropDownClosed(EventArgs e)
        {
            base.OnDropDownClosed(e);
            AutoCompleteManager.Disabled = false;
            IsTextSearchEnabled = false;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _textBox = GetTemplateChild("PART_EditableTextBox") as TextBox;
            AutoCompleteManager.AttachTextBox(_textBox);
        }
    }
}
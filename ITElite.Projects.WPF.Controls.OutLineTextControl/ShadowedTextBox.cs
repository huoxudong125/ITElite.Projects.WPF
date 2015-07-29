using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ITElite.Projects.WPF.Controls.TextControl
{
    /// <summary>
    ///     Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///     Step 1a) Using this custom control in a XAML file that exists in the current project.
    ///     Add this XmlNamespace attribute to the root element of the markup file where it is
    ///     to be used:
    ///     xmlns:MyNamespace="clr-namespace:ITElite.Projects.WPF.Controls.TextControl"
    ///     Step 1b) Using this custom control in a XAML file that exists in a different project.
    ///     Add this XmlNamespace attribute to the root element of the markup file where it is
    ///     to be used:
    ///     xmlns:MyNamespace="clr-namespace:ITElite.Projects.WPF.Controls.TextControl;assembly=ITElite.Projects.WPF.Controls.TextControl"
    ///     You will also need to add a project reference from the project where the XAML file lives
    ///     to this project and Rebuild to avoid compilation errors:
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///     Step 2)
    ///     Go ahead and use your control in the XAML file.
    ///     <MyNamespace:ShadowedTextBox />
    /// </summary>
    public class ShadowedTextBox : TextBox
    {
        private AdornerLabel myAdornerLabel;
        private AdornerLayer myAdornerLayer;

        static ShadowedTextBox()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(ShadowedTextBox), new FrameworkPropertyMetadata(typeof(ShadowedTextBox)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            myAdornerLayer = AdornerLayer.GetAdornerLayer(this);
            myAdornerLabel = new AdornerLabel(this, Label, LabelStyle);
            UpdateAdorner(this);

            var focusProp = DependencyPropertyDescriptor.FromProperty(IsFocusedProperty, typeof (FrameworkElement));
            if (focusProp != null)
            {
                focusProp.AddValueChanged(this, delegate { UpdateAdorner(this); });
            }

            var containsTextProp = DependencyPropertyDescriptor.FromProperty(HasTextProperty, typeof (ShadowedTextBox));
            if (containsTextProp != null)
            {
                containsTextProp.AddValueChanged(this, delegate { UpdateAdorner(this); });
            }
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            HasText = Text != "";

            base.OnTextChanged(e);
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            myAdornerLayer.RemoveAdorners<AdornerLabel>(this); // requires AdornerExtensions.cs

            base.OnDragEnter(e);
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            UpdateAdorner(this);

            base.OnDragLeave(e);
        }

        private void UpdateAdorner(FrameworkElement elem)
        {
            if (((ShadowedTextBox) elem).HasText || elem.IsFocused)
            {
                // Hide the Shadowed Label
                ToolTip = Label;
                myAdornerLayer.RemoveAdorners<AdornerLabel>(elem); // requires AdornerExtensions.cs
            }
            else
            {
                // Show the Shadowed Label
                ToolTip = null;
                if (!myAdornerLayer.Contains<AdornerLabel>(elem)) // requires AdornerExtensions.cs
                    myAdornerLayer.Add(myAdornerLabel);
            }
        }

        #region Properties

        public string Label
        {
            get { return (string) GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Label.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof (string), typeof (ShadowedTextBox),
                new UIPropertyMetadata("Label"));

        public Style LabelStyle
        {
            get { return (Style) GetValue(LabelStyleProperty); }
            set { SetValue(LabelStyleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LabelStyle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LabelStyleProperty =
            DependencyProperty.Register("LabelStyle", typeof (Style), typeof (ShadowedTextBox),
                new UIPropertyMetadata(null));


        public bool HasText
        {
            get { return (bool) GetValue(HasTextProperty); }
            private set { SetValue(HasTextPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey HasTextPropertyKey =
            DependencyProperty.RegisterReadOnly("HasText", typeof (bool), typeof (ShadowedTextBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty HasTextProperty = HasTextPropertyKey.DependencyProperty;

        #endregion
    }
}
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ITElite.Projects.WPF.Controls.TextControl
{
    // Adorners must subclass the abstract base class Adorner.
    public class AdornerLabel : Adorner
    {
        private readonly TextBlock _textBlock;
        // Be sure to call the base class constructor.
        public AdornerLabel(UIElement adornedElement, string label, Style labelStyle)
            : base(adornedElement)
        {
            _textBlock = new TextBlock();
            _textBlock.Style = labelStyle;
            _textBlock.Text = label;
        }

        //return the count of the visuals
        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        //make sure that the layout system knows of the element
        protected override Size MeasureOverride(Size constraint)
        {
            _textBlock.Measure(constraint);
            return constraint;
        }

        //make sure that the layout system knows of the element
        protected override Size ArrangeOverride(Size finalSize)
        {
            _textBlock.Arrange(new Rect(finalSize));
            return finalSize;
        }

        //return the visual that we want to display
        protected override Visual GetVisualChild(int index)
        {
            return _textBlock;
        }
    }
}
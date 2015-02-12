using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ITElite.Projects.WPF.Controls.TextControl
{
    // Adorners must subclass the abstract base class Adorner.
    public class AdornerLabel : Adorner
    {
        private TextBlock _textBlock;

        // Be sure to call the base class constructor.
        public AdornerLabel(UIElement adornedElement, string label, Style labelStyle)
            : base(adornedElement)
        {
            _textBlock = new TextBlock();
            _textBlock.Style = labelStyle;
            _textBlock.Text = label;
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
        protected override System.Windows.Media.Visual GetVisualChild(int index)
        {
            return _textBlock;
        }

        //return the count of the visuals
        protected override int VisualChildrenCount
        {
            get { return 1; }
        }
    }

}

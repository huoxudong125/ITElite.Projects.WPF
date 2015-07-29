using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ITElite.Projects.WPF.Controls.DeepZoom.OverLays
{
    public class OverViewerAdorner : Adorner
    {
        private Control _child;

        public OverViewerAdorner(UIElement adornerElement, Control childControl)
            : base(adornerElement)
        {
            Child = childControl;
            HorizontalAlignment = HorizontalAlignment.Right;
            VerticalAlignment = VerticalAlignment.Bottom;
        }

        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        public Control Child
        {
            get { return _child; }
            set
            {
                if (_child != null)
                {
                    RemoveVisualChild(_child);
                }
                _child = value;
                if (_child != null)
                {
                    AddVisualChild(_child);
                }
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException();
            return _child;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _child.Measure(constraint);
            return _child.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _child.Arrange(new Rect(new Point(0,
                ((FrameworkElement) AdornedElement).ActualHeight - finalSize.Height), finalSize));

            return new Size(_child.ActualWidth, _child.ActualHeight);
        }
    }
}
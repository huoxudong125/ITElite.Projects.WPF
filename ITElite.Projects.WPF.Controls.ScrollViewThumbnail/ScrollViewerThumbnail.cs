using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ITElite.Projects.WPF.Controls.ScrollViewThumbnail
{
    /// <summary>
    ///     Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///     Step 1a) Using this custom control in a XAML file that exists in the current project.
    ///     Add this XmlNamespace attribute to the root element of the markup file where it is
    ///     to be used:
    ///     xmlns:MyNamespace="clr-namespace:ITElite.Projects.WPF.Controls.ScrollViewThumbnail"
    ///     Step 1b) Using this custom control in a XAML file that exists in a different project.
    ///     Add this XmlNamespace attribute to the root element of the markup file where it is
    ///     to be used:
    ///     xmlns:MyNamespace="clr-namespace:ITElite.Projects.WPF.Controls.ScrollViewThumbnail;assembly=ITElite.Projects.WPF.Controls.ScrollViewThumbnail"
    ///     You will also need to add a project reference from the project where the XAML file lives
    ///     to this project and Rebuild to avoid compilation errors:
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Select this project]
    ///     Step 2)
    ///     Go ahead and use your control in the XAML file.
    ///     <MyNamespace:CustomControl1 />
    /// </summary>
    public class ScrollViewerThumbnail : Control
    {
        private const string PART_Highlight = "PART_Highlight";
        // Using a DependencyProperty as the backing store for ScrollViewer. This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScrollViewerProperty =
            DependencyProperty.Register("ScrollViewer", typeof (ScrollViewer), typeof (ScrollViewerThumbnail),
                new UIPropertyMetadata(null));

        public static readonly DependencyProperty HighlightFillProperty =
            DependencyProperty.Register("HighlightFill",
                typeof (Brush),
                typeof (ScrollViewerThumbnail),
                new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(128, 255, 255, 0))));

        static ScrollViewerThumbnail()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (ScrollViewerThumbnail),
                new FrameworkPropertyMetadata(typeof (ScrollViewerThumbnail)));
        }

        public ScrollViewer ScrollViewer
        {
            get { return (ScrollViewer) GetValue(ScrollViewerProperty); }
            set { SetValue(ScrollViewerProperty, value); }
        }

        public Brush HighlightFill
        {
            get { return (Brush) GetValue(HighlightFillProperty); }
            set { SetValue(HighlightFillProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var partHighlight = (Thumb) Template.FindName(PART_Highlight, this);
            partHighlight.DragDelta += partHighlight_DragDelta;
        }

        private void partHighlight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset + e.VerticalChange);
            ScrollViewer.ScrollToHorizontalOffset(ScrollViewer.HorizontalOffset + e.HorizontalChange);
        }
    }
}
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ITElite.Projects.WPF.Controls.DeepZoom.Controls;

namespace ITElite.Projects.WPF.Controls.DeepZoom.OverLays
{
    public class OverViewer : Control
    {
        #region .octr

        static OverViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (OverViewer)
                , new FrameworkPropertyMetadata(typeof (OverViewer)));
        }

        public OverViewer(UIElement deepZoom)
        {
            MultiScaleImage = (MultiScaleImage) deepZoom;
            MultiScaleImage.ViewChangeOnFrame += MultiScaleImage_ViewChangeOnFrame;
            this.Width = 150;
            this.Height = 150;
            this.Margin = new Thickness(10, 0, 0, 30);
        }

        private void MultiScaleImage_ViewChangeOnFrame(object sender, double e)
        {
            //TODO:
        }

        #endregion .octr

        #region public Property

        // Using a DependencyProperty as the backing store for ScrollViewer. This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MultiScaleImageProperty =
            DependencyProperty.Register("MultiScaleImage", typeof (MultiScaleImage), typeof (OverViewer),
                new UIPropertyMetadata(null));

        public MultiScaleImage MultiScaleImage
        {
            get { return (MultiScaleImage) GetValue(MultiScaleImageProperty); }
            set { SetValue(MultiScaleImageProperty, value); }
        }

        public Brush HighlightFill
        {
            get { return (Brush) GetValue(HighlightFillProperty); }
            set { SetValue(HighlightFillProperty, value); }
        }

        public static readonly DependencyProperty HighlightFillProperty =
            DependencyProperty.Register("HighlightFill",
                typeof (Brush),
                typeof (OverViewer),
                new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(128, 255, 255, 0))));

        #endregion public Property

        #region protected override

        private const string PART_Highlight = "PART_Highlight";

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var partHighlight = (Thumb) this.Template.FindName(PART_Highlight, this);
            partHighlight.DragDelta += partHighlight_DragDelta;
        }

        private void partHighlight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            MultiScaleImage.ZoomableCanvas.Offset =
                new Point(
                    MultiScaleImage.ZoomableCanvas.Offset.X + MultiScaleImage.ZoomableCanvas.Scale*e.HorizontalChange
                    , MultiScaleImage.ZoomableCanvas.Offset.Y + MultiScaleImage.ZoomableCanvas.Scale*e.VerticalChange);
        }

        #endregion protected override
    }
}
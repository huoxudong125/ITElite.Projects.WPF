using System;
using System.Threading;
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
            this.Height = 100; 
            this.Width = Height*MultiScaleImage.AspectRatio;
            this.Margin = new Thickness(100, 0, 0, 0);
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

        #region IsShowOverViewer

        public static readonly DependencyProperty IsShowOverViewerProperty =
          DependencyProperty.Register("IsShowOverViewer", typeof(bool), typeof(OverViewer), new PropertyMetadata(false));

        public bool IsShowOverViewer
        {
            get { return (bool)GetValue(IsShowOverViewerProperty); }
            set { SetValue(IsShowOverViewerProperty, value); }
        }

        #endregion IsShowOverViewer




        public Brush HighlightFill
        {
            get { return (Brush) GetValue(HighlightFillProperty); }
            set { SetValue(HighlightFillProperty, value); }
        }

        public static readonly DependencyProperty HighlightFillProperty =
            DependencyProperty.Register("HighlightFill",
                typeof (Brush),
                typeof (OverViewer),
                new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(128, 255, 0, 0))));

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
            Console.WriteLine("Begin MultiScaleImage.ZoomableCanvas:{0},{1}", MultiScaleImage.ZoomableCanvas.ActualViewbox.X
                , MultiScaleImage.ZoomableCanvas.ActualViewbox.Y);

            MultiScaleImage.ZoomableCanvas.Offset =
                new Point(
                    MultiScaleImage.ZoomableCanvas.Offset.X + e.HorizontalChange
                    , MultiScaleImage.ZoomableCanvas.Offset.Y + e.VerticalChange);

            Thread.Sleep(500);
            Console.WriteLine("End MultiScaleImage.ZoomableCanvas:{0},{1}", MultiScaleImage.ZoomableCanvas.ActualViewbox.X
                , MultiScaleImage.ZoomableCanvas.ActualViewbox.Y);
        }

        #endregion protected override
    }
}
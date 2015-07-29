using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace ITElite.Projects.WPF.Controls.DeepZoom.Controls
{
    /// <summary>
    ///     Simple FrameworkElement that draws and animates an image in the screen with the lowest possible overhead.
    /// </summary>
    public class TileHost : FrameworkElement
    {
        // Create a collection of child visual objects.

        private static readonly AnimationTimeline _opacityAnimation =
            new DoubleAnimation(1, TimeSpan.FromMilliseconds(500)) {EasingFunction = new ExponentialEase()};

        private DrawingVisual _visual;

        public TileHost()
        {
            IsHitTestVisible = false;
        }

        public TileHost(ImageSource source, double scale)
            : this()
        {
            Source = source;
            Scale = scale;
        }

        #region Dependency Properties

        #region Source

        /// <summary>
        ///     Source Dependency Property
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof (ImageSource), typeof (TileHost),
                new FrameworkPropertyMetadata(null,
                    RefreshTile));

        /// <summary>
        ///     Gets or sets the Source property. This dependency property
        ///     indicates the source of the image to be displayed.
        /// </summary>
        public ImageSource Source
        {
            get { return (ImageSource) GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        #endregion Source

        #region Scale

        /// <summary>
        ///     Scale Dependency Property
        /// </summary>
        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register("Scale", typeof (double), typeof (TileHost),
                new FrameworkPropertyMetadata(1.0,
                    RefreshTile));

        /// <summary>
        ///     Gets or sets the Scale property. This dependency property
        ///     indicates the scaling to be applied to this tile.
        /// </summary>
        public double Scale
        {
            get { return (double) GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }

        #endregion Scale

        #endregion Dependency Properties

        #region Private methods

        /// <summary>
        ///     Called when the tile should be refreshed (Scale or Source changed)
        /// </summary>
        private static void RefreshTile(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tileHost = d as TileHost;
            if (tileHost != null && tileHost.Source != null && tileHost.Scale > 0)
                tileHost.RenderTile();
        }

        private void RenderTile()
        {
            _visual = new DrawingVisual();
            var bitmapSource = Source as BitmapSource;
            if (bitmapSource != null)
            {
                Width = bitmapSource.PixelWidth*Scale;
                Height = bitmapSource.PixelHeight*Scale;
            }
            else
            {
                Width = Source.Width*Scale;
                Height = Source.Height*Scale;
            }

            var dc = _visual.RenderOpen();
            dc.DrawImage(Source, new Rect(0, 0, Width, Height));
            dc.Close();

            CacheMode = new BitmapCache(1/Scale);

            // Animate opacity
            Opacity = 0;
            BeginAnimation(OpacityProperty, _opacityAnimation);
        }

        #endregion Private methods

        #region FrameworkElement overrides

        // Provide a required override for the VisualChildrenCount property.
        protected override int VisualChildrenCount
        {
            get { return _visual == null ? 0 : 1; }
        }

        // Provide a required override for the GetVisualChild method.
        protected override Visual GetVisualChild(int index)
        {
            return _visual;
        }

        #endregion FrameworkElement overrides
    }
}
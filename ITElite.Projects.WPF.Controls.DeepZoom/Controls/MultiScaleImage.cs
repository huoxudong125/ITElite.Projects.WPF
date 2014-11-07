using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ITElite.Projects.WPF.Controls.DeepZoom.Core;
using ITElite.Projects.WPF.Controls.DeepZoom.OverLays;
using ITElite.Projects.WPF.Controls.DeepZoom.Touch;

namespace ITElite.Projects.WPF.Controls.DeepZoom.Controls
{
    /// <summary>
    /// Enables users to open a multi-resolution image, which can be zoomed in on and panned across.
    /// </summary>
    [TemplatePart(Name = "PART_ItemsControl", Type = typeof(ItemsControl))]
    public class MultiScaleImage : Control
    {
        private const int ScaleAnimationRelativeDuration = 400;
        private const double MinScaleRelativeToMinSize = 0.8;
        private const int ThrottleIntervalMilliseconds = 200;

        private ItemsControl _itemsControl;
        private ZoomableCanvas _zoomableCanvas;
        private MultiScaleImageSpatialItemsSource _spatialSource;
        private MultiValueScalebarAdorner _multiValueScalebarAdorner;
        private double _originalScale;
        private int _desiredLevel;
        private readonly DispatcherTimer _levelChangeThrottle;

        // for apply the template
        static MultiScaleImage()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiScaleImage),
                new FrameworkPropertyMetadata(typeof(MultiScaleImage)));
        }

        public MultiScaleImage()
        {
            MouseTouchDevice.RegisterEvents(this);
            _levelChangeThrottle = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ThrottleIntervalMilliseconds),
                IsEnabled = false
            };
            _levelChangeThrottle.Tick += (s, e) =>
            {
                _spatialSource.CurrentLevel = _desiredLevel;
                _levelChangeThrottle.IsEnabled = false;
            };
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            IsManipulationEnabled = true;
            _itemsControl = GetTemplateChild("PART_ItemsControl") as ItemsControl;
            if (_itemsControl == null) return;

            _itemsControl.ApplyTemplate();

            var factoryPanel = new FrameworkElementFactory(typeof(ZoomableCanvas));
            factoryPanel.AddHandler(LoadedEvent, new RoutedEventHandler(ZoomableCanvasLoaded));
            _itemsControl.ItemsPanel = new ItemsPanelTemplate(factoryPanel);

            if (_spatialSource != null)
                _itemsControl.ItemsSource = _spatialSource;
        }

        private void ZoomableCanvasLoaded(object sender, RoutedEventArgs e)
        {
            _zoomableCanvas = sender as ZoomableCanvas;
            if (_zoomableCanvas != null)
            {
                _zoomableCanvas.RealizationPriority = DispatcherPriority.Background;
                _zoomableCanvas.RealizationRate = 10;
                InitializeCanvas();

                var adornerLayer = AdornerLayer.GetAdornerLayer(this);
                if (_multiValueScalebarAdorner != null)
                {
                    adornerLayer.Remove(_multiValueScalebarAdorner);
                }
                //var scaleBar = new Button()
                //{
                //    HorizontalAlignment = HorizontalAlignment.Right,
                //    Content = "X",
                //};
                var scaleBar = new MultiValueScaleBar(this);
                _multiValueScalebarAdorner = new MultiValueScalebarAdorner(this, scaleBar);
                adornerLayer.Add(_multiValueScalebarAdorner);
            }
        }

        private static AdornerLayer GetAdornerLayer(FrameworkElement subject)
        {
            AdornerLayer layer = null;
            do
            {
                if ((layer = AdornerLayer.GetAdornerLayer(subject)) != null)
                    break;
            } while ((subject = subject.Parent as FrameworkElement) != null);
            return layer;
        }

        #region Public methods

        /// <summary>
        /// Enables a user to zoom in on a point of the MultiScaleImage.
        /// </summary>
        /// <param name="zoomIncrementFactor">Specifies the zoom. This number is greater than 0. A value of 1 specifies that the image fit the allotted page size exactly. A number greater than 1 specifies to zoom in. If a value of 0 or less is used, failure is returned and no zoom changes are applied. </param>
        /// <param name="zoomCenterLogicalX">X coordinate for the point on the MultiScaleImage that is zoomed in on. This is a logical point (between 0 and 1). </param>
        /// <param name="zoomCenterLogicalY">Y coordinate for the point on the MultiScaleImage that is zoomed in on. This is a logical point (between 0 and 1).</param>
        public void ZoomAboutLogicalPoint(double zoomIncrementFactor, double zoomCenterLogicalX,
            double zoomCenterLogicalY)
        {
            var logicalPoint = new Point(zoomCenterLogicalX, zoomCenterLogicalY);
            ScaleCanvas(zoomIncrementFactor, LogicalToElementPoint(logicalPoint), true);
        }

        /// <summary>
        /// Gets a point with logical coordinates (values between 0 and 1) from a point of the MultiScaleImage.
        /// </summary>
        /// <param name="elementPoint">The point on the MultiScaleImage to translate into a point with logical coordinates (values between 0 and 1).</param>
        /// <returns>The logical point translated from the elementPoint.</returns>
        public Point ElementToLogicalPoint(Point elementPoint)
        {
            var absoluteCanvasPoint = _zoomableCanvas.GetCanvasPoint(elementPoint);
            return new Point(absoluteCanvasPoint.X / _zoomableCanvas.Extent.Width,
                absoluteCanvasPoint.Y / _zoomableCanvas.Extent.Height);
        }

        /// <summary>
        /// Gets a point with pixel coordinates relative to the MultiScaleImage from a logical point (values between 0 and 1).
        /// </summary>
        /// <param name="logicalPoint">The logical point to translate into pixel coordinates relative to the MultiScaleImage.</param>
        /// <returns>A point with pixel coordinates relative to the MultiScaleImage translated from logicalPoint.</returns>
        public Point LogicalToElementPoint(Point logicalPoint)
        {
            var absoluteCanvasPoint = new Point(
                logicalPoint.X * _zoomableCanvas.Extent.Width,
                logicalPoint.Y * _zoomableCanvas.Extent.Height
                );
            return _zoomableCanvas.GetVisualPoint(absoluteCanvasPoint);
        }

        #endregion Public methods

        #region Dependency Properties

        #region Source

        /// <summary>
        /// Source Dependency Property
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(MultiScaleTileSource), typeof(MultiScaleImage),
                new FrameworkPropertyMetadata(null,
                    new PropertyChangedCallback(OnSourceChanged)));

        public static readonly DependencyProperty ResolutionProperty = DependencyProperty.Register("Resolution"
            , typeof(double), typeof(MultiScaleImage),
            new PropertyMetadata(default(double), new PropertyChangedCallback(OnResolutionChanged)));

        public static readonly DependencyProperty UnitProperty = DependencyProperty.Register("Unit",
            typeof(Units), typeof(MultiScaleImage),
            new PropertyMetadata(default(Units), new PropertyChangedCallback(OnUnitChanged)));

        private static void OnUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //TODO:
        }

        private static void OnResolutionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //TODO:
        }

        public double Resolution
        {
            get { return (double)GetValue(ResolutionProperty); }
            set { SetValue(ResolutionProperty, value); }
        }

        public Units Unit
        {
            get { return (Units)GetValue(UnitProperty); }
            set { SetValue(UnitProperty, value); }
        }

        public double Scale
        {
            get { return _zoomableCanvas.Scale; }
        }

        /// <summary>
        /// Gets or sets the Source property. This dependency property
        /// indicates the tile source for this MultiScaleImage.
        /// </summary>
        public MultiScaleTileSource Source
        {
            get { return (MultiScaleTileSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// Handles changes to the Source property.
        /// </summary>
        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MultiScaleImage target = (MultiScaleImage)d;
            MultiScaleTileSource oldSource = (MultiScaleTileSource)e.OldValue;
            MultiScaleTileSource newSource = target.Source;
            target.OnSourceChanged(oldSource, newSource);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the Source property.
        /// </summary>
        protected virtual void OnSourceChanged(MultiScaleTileSource oldSource, MultiScaleTileSource newSource)
        {
            if (newSource == null)
            {
                _spatialSource = null;
                return;
            }

            _spatialSource = new MultiScaleImageSpatialItemsSource(newSource);

            if (_itemsControl != null)
                _itemsControl.ItemsSource = _spatialSource;

            if (_zoomableCanvas != null)
                InitializeCanvas();
        }

        #endregion Source

        #region AspectRatio

        /// <summary>
        /// AspectRatio Read-Only Dependency Property
        /// </summary>
        private static readonly DependencyPropertyKey AspectRatioPropertyKey
            = DependencyProperty.RegisterReadOnly("AspectRatio", typeof(double), typeof(MultiScaleImage),
                new FrameworkPropertyMetadata(1.0));

        public static readonly DependencyProperty AspectRatioProperty
            = AspectRatioPropertyKey.DependencyProperty;

        private double MaxScaleRelativeToMaxSize;

        /// <summary>
        /// Gets the aspect ratio of the image used as the source of the MultiScaleImage.
        /// The aspect ratio is the width of the image divided by its height.
        /// </summary>
        public double AspectRatio
        {
            get { return (double)GetValue(AspectRatioProperty); }
        }

        /// <summary>
        /// Provides a secure method for setting the AspectRatio property.
        /// The aspect ratio is the width of the image divided by its height.
        /// </summary>
        /// <param name="value">The new value for the property.</param>
        protected void SetAspectRatio(double value)
        {
            SetValue(AspectRatioPropertyKey, value);
        }

        #endregion AspectRatio

        #endregion Dependency Properties

        #region Overriden Input Event Handlers

        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            base.OnManipulationDelta(e);

            var oldScale = _zoomableCanvas.Scale;
            _zoomableCanvas.ApplyAnimationClock(ZoomableCanvas.ScaleProperty, null);
            _zoomableCanvas.Scale = oldScale;

            var oldOffset = _zoomableCanvas.Offset;
            _zoomableCanvas.ApplyAnimationClock(ZoomableCanvas.OffsetProperty, null);
            _zoomableCanvas.Offset = oldOffset;

            var scale = e.DeltaManipulation.Scale.X;
            ScaleCanvas(scale, e.ManipulationOrigin);

            _zoomableCanvas.Offset -= e.DeltaManipulation.Translation;
            e.Handled = true;
        }

        protected override void OnManipulationInertiaStarting(ManipulationInertiaStartingEventArgs e)
        {
            base.OnManipulationInertiaStarting(e);
            e.TranslationBehavior = new InertiaTranslationBehavior { DesiredDeceleration = 0.0096 };
            e.ExpansionBehavior = new InertiaExpansionBehavior { DesiredDeceleration = 0.000096 };
            e.Handled = true;
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            var relativeScale = Math.Pow(2, (double)e.Delta / Mouse.MouseWheelDeltaForOneLine);
            var position = e.GetPosition(_itemsControl);

            ScaleCanvas(relativeScale, position, true);

            e.Handled = true;
        }

        #endregion Overriden Input Event Handlers

        #region Private helpers

        private void InitializeCanvas()
        {
            var level = Source.GetLevel(_zoomableCanvas.ActualWidth, _zoomableCanvas.ActualHeight);
            _spatialSource.CurrentLevel = level;

            var imageSize = Source.ImageSize;
            var relativeScale = Math.Min(_itemsControl.ActualWidth / imageSize.Width,
                _itemsControl.ActualHeight / imageSize.Height);

            _originalScale = relativeScale;

            _zoomableCanvas.Scale = _originalScale;
            _zoomableCanvas.Offset =
                new Point(imageSize.Width * 0.5 * relativeScale - _zoomableCanvas.ActualWidth * 0.5,
                    imageSize.Height * 0.5 * relativeScale - _zoomableCanvas.ActualHeight * 0.5);
            _zoomableCanvas.Clip = new RectangleGeometry(
                new Rect(0, 0,
                    imageSize.Width,
                    imageSize.Height));

            SetAspectRatio(_spatialSource.Extent.Width / _spatialSource.Extent.Height);

            _spatialSource.InvalidateSource();
        }

        private void ScaleCanvas(double relativeScale, Point center, bool animate = false)
        {
            var scale = _zoomableCanvas.Scale;

            if (scale <= 0) return;

            // minimum size = 80% of size where the whole image is visible
            // maximum size = Max(120% of full resolution of the image, 120% of original scale)

            MaxScaleRelativeToMaxSize = 1.2;
            relativeScale = relativeScale.Clamp(
                MinScaleRelativeToMinSize * _originalScale / scale,
                Math.Max(MaxScaleRelativeToMaxSize, MaxScaleRelativeToMaxSize * _originalScale) / scale);

            var targetScale = scale * relativeScale;

            var newLevel = Source.GetLevel(targetScale);
            var level = _spatialSource.CurrentLevel;
            if (newLevel != level)
            {
                // If it's zooming in, throttle
                if (newLevel > level)
                    ThrottleChangeLevel(newLevel);
                else
                    _spatialSource.CurrentLevel = newLevel;
            }

            if (targetScale != scale)
            {
                var position = (Vector)center;
                var targetOffset = (Point)((Vector)(_zoomableCanvas.Offset + position) * relativeScale - position);

                if (animate)
                {
                    if (relativeScale < 1)
                        relativeScale = 1 / relativeScale;
                    var duration = TimeSpan.FromMilliseconds(relativeScale * ScaleAnimationRelativeDuration);
                    var easing = new CubicEase();
                    _zoomableCanvas.BeginAnimation(ZoomableCanvas.ScaleProperty,
                        new DoubleAnimation(targetScale, duration) { EasingFunction = easing }, HandoffBehavior.Compose);
                    _zoomableCanvas.BeginAnimation(ZoomableCanvas.OffsetProperty,
                        new PointAnimation(targetOffset, duration) { EasingFunction = easing }, HandoffBehavior.Compose);
                }
                else
                {
                    _zoomableCanvas.Scale = targetScale;
                    _zoomableCanvas.Offset = targetOffset;
                }

                if (ViewChangeOnFrame != null)
                    ViewChangeOnFrame(this, targetScale);
            }
        }

        private void ThrottleChangeLevel(int newLevel)
        {
            _desiredLevel = newLevel;

            if (_levelChangeThrottle.IsEnabled)
                _levelChangeThrottle.Stop();

            _levelChangeThrottle.Start();
        }

        #endregion Private helpers

        public System.Windows.Visibility ScaleVisibility { get; set; }

        public event EventHandler<double> ViewChangeOnFrame;
    }
}
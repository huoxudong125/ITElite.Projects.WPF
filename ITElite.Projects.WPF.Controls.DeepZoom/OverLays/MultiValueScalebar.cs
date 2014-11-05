using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ITElite.Projects.WPF.Controls.DeepZoom.Core;

namespace ITElite.Projects.WPF.Controls.DeepZoom.OverLays
{
    public class MultiValueScaleBar : Control, INotifyPropertyChanged
    {
        #region Private Properties

        private VisualCollection _Visuals;
        private ContentPresenter _ContentPresenter;


        private Rectangle _metricScaleBar;

        private TextBlock _metricScaleValue;

        private double _resolution;
        private Units _unit;

        //A set of values in which to round the scale bars values off to.
        private double[] scaleMultipliers = new double[] {1000, 500, 250, 200, 100, 50, 25, 10, 5, 2, 1, 0.5};

        #endregion Private Properties

        #region Property

        public double Resolution
        {
            get { return _resolution; }
            set
            {
                _resolution = value;
                UpdateScalebar();
            }
        }

        public Units Unit
        {
            get { return _unit; }
            set
            {
                _unit = value;
                UpdateScalebar();
            }
        }

        #endregion Property

        #region Constructor

        public MultiValueScaleBar(UIElement map)
        {
            _Visuals = new VisualCollection(this);
            _ContentPresenter = new ContentPresenter();
            _ContentPresenter.HorizontalAlignment = HorizontalAlignment.Right;
            _ContentPresenter.VerticalAlignment = VerticalAlignment.Bottom;
            _Visuals.Add(_ContentPresenter);

            var grid = new Grid();
            //Set initial size and position information for scale bar panel.
            grid.Width = 250;
            grid.Height = 30;
            grid.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            grid.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            grid.Margin = new System.Windows.Thickness(0, 0, 10, 30);

            //Create the metric scalebar and label.
            _metricScaleValue = new TextBlock()
            {
                Text = "1000 km",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            grid.Children.Add(_metricScaleValue);


            _metricScaleBar = new Rectangle()
            {
                Fill = new SolidColorBrush(Colors.DodgerBlue),
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 1,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Width = 200,
                Height = 60,
                Margin = new System.Windows.Thickness(0, 20, 0, 0)
            };

            grid.Children.Add(_metricScaleBar);


            //map.ViewChangeOnFrame += (s, e) =>
            //{
            //    UpdateScalebar();
            //};

            ////Add this scalebar to the map.
            //map.Children.Add(this);

            //Update the scale bar for the current map view.
            UpdateScalebar();

            Content = grid;
        }

        #endregion Constructor

        #region Private Methods

        private void UpdateScalebar()
        {
            //Calculate the ground resolution in km/pixel based on the center of the map and current zoom level.
            var metricResolution = Resolution;
            // GroundResolution(_map.Center.Latitude, (int)Math.Round(_map.ZoomLevel));
            var imperialResolution = metricResolution*0.62137119; //KM to miles

            double maxScaleBarWidth = 100;

            string metricUnitName = "km";
            double metricDistance = maxScaleBarWidth*metricResolution;

            if (metricDistance < 1)
            {
                metricUnitName = "m";
                metricDistance *= 1000;
                metricResolution *= 1000;
            }

            for (var i = 0; i < scaleMultipliers.Length; i++)
            {
                if (metricDistance/scaleMultipliers[i] > 1)
                {
                    var scaleValue = metricDistance - metricDistance%scaleMultipliers[i];
                    _metricScaleValue.Text = string.Format("{0:F0} {1}", scaleValue, metricUnitName);
                    _metricScaleBar.Width = scaleValue/metricResolution;
                    break;
                }
            }
        }

        #endregion Private Methods

        protected override Size MeasureOverride(Size constraint)
        {
            _ContentPresenter.Measure(constraint);
            return _ContentPresenter.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _ContentPresenter.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            return _ContentPresenter.RenderSize;
        }

        protected override Visual GetVisualChild(int index)
        {
            return _Visuals[index];
        }

        protected override int VisualChildrenCount
        {
            get { return _Visuals.Count; }
        }

        public object Content
        {
            get { return _ContentPresenter.Content; }
            set { _ContentPresenter.Content = value; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(name));
        }
    }
}
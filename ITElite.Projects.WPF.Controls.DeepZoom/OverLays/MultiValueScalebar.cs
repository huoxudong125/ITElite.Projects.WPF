using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using ITElite.Projects.WPF.Controls.DeepZoom.Controls;
using ITElite.Projects.WPF.Controls.DeepZoom.Core;
using ITElite.Projects.WPF.Controls.TextControl;

namespace ITElite.Projects.WPF.Controls.DeepZoom.OverLays
{
    public class MultiValueScaleBar : Control
    {
        #region Private Properties

        private const string PART_MetricScaleValue = "PART_MetricScaleValue";
        private const string PART_MetricScaleBar = "PART_MetricScaleBar";
        private readonly double[] _scaleMultipliers = {1000, 500, 250, 200, 100, 50, 25, 10, 5, 2, 1, 0.5};
        private readonly MultiScaleImage deepZoom;

        private Rectangle _metricScaleBar;
        private OutlineTextControl _metricScaleValue;

        //A set of values in which to round the scale bars values off to.

        #endregion Private Properties

        #region Property

        public double Resolution { get; set; }

        public Units Unit { get; set; }

        #endregion Property

        #region Constructor

        static MultiValueScaleBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (MultiValueScaleBar)
                , new FrameworkPropertyMetadata(typeof (MultiValueScaleBar)));
        }

        public MultiValueScaleBar(UIElement multiScaleImage)
        {
            deepZoom = multiScaleImage as MultiScaleImage;
            if (deepZoom != null)
            {
                deepZoom.ViewChangeOnFrame +=
                    (s, e) => UpdateScalebar(((MultiScaleImage) s).Resolution/((MultiScaleImage) s).Scale);
                DependencyPropertyDescriptor dpd =
                    DependencyPropertyDescriptor.FromProperty(MultiScaleImage.ResolutionProperty,
                        typeof (MultiScaleImage));

                if (dpd != null)
                {
                    dpd.AddValueChanged(deepZoom, OnResolutionChanged);
                }
            }
        }


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _metricScaleBar = (Rectangle) Template.FindName(PART_MetricScaleBar, this);
            _metricScaleValue = (OutlineTextControl) Template.FindName(PART_MetricScaleValue, this);

            //Update the scale bar for the current map view.
            UpdateScalebar(deepZoom.Resolution/deepZoom.Scale);
        }

        #endregion Constructor

        #region Private Methods

        private void UpdateScalebar(double resolution)
        {
            //Calculate the ground resolution in km/pixel based on the center of the map and current zoom level.
            double metricResolution = resolution;
            // GroundResolution(_map.Center.Latitude, (int)Math.Round(_map.ZoomLevel));
            //var imperialResolution = metricResolution * 0.62137119; //KM to miles

            double maxScaleBarWidth = 100;

            string metricUnitName = "m";
            double metricDistance = maxScaleBarWidth*metricResolution;

            if (metricDistance < 1e-6)
            {
                metricUnitName = "nm";
                metricDistance *= 1e9;
                metricResolution *= 1e9;
            }
            else if (metricDistance < 1e-3)
            {
                metricUnitName = "μm";
                metricDistance *= 1e6;
                metricResolution *= 1e6;
            }
            else if (metricDistance < 1)
            {
                metricUnitName = "mm";
                metricDistance *= 1000;
                metricResolution *= 1000;
            }

            for (int i = 0; i < _scaleMultipliers.Length; i++)
            {
                if (metricDistance/_scaleMultipliers[i] > 1)
                {
                    double scaleValue = metricDistance - metricDistance%_scaleMultipliers[i];
                    _metricScaleValue.Text = string.Format("{0:F0} {1}", scaleValue, metricUnitName);
                    _metricScaleBar.Width = scaleValue/metricResolution;
                    break;
                }
            }
        }

        private void OnResolutionChanged(object sender, EventArgs e)
        {
            //Update the scale bar for the current map view.
            UpdateScalebar(deepZoom.Resolution/deepZoom.Scale);
        }

        #endregion Private Methods
    }
}
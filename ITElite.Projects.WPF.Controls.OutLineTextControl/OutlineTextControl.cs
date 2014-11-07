using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ITElite.Projects.WPF.Controls.TextControl
{
    /// <summary>
    /// OutlineText custom control class derives layout, event, data binding, and rendering from derived FrameworkElement class.
    /// </summary>
    public class OutlineTextControl : FrameworkElement
    {
        static OutlineTextControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OutlineTextControl), new FrameworkPropertyMetadata(typeof(OutlineTextControl)));
        }

        #region Private Fields

        private Geometry _textGeometry;
        private Geometry _textHighLightGeometry;

        #endregion

        #region Private Methods

        /// <summary>
        /// Invoked when a dependency property has changed. Generate a new FormattedText object to display.
        /// </summary>
        /// <param name="d">OutlineText object whose property was updated.</param>
        /// <param name="e">Event arguments for the dependency property.</param>
        private static void OnOutlineTextInvalidated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((OutlineTextControl)d).CreateText();
        }

        #endregion

        #region FrameworkElement Overrides

        /// <summary>
        /// OnRender override draws the geometry of the text and optional highlight.
        /// </summary>
        /// <param name="drawingContext">Drawing context of the OutlineText control.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            // Draw the outline based on the properties that are set.
            drawingContext.DrawGeometry(Fill, new Pen(Stroke, StrokeThickness), _textGeometry);

            // Draw the text highlight based on the properties that are set.
            if (Highlight == true)
            {
                drawingContext.DrawGeometry(null, new Pen(Stroke, StrokeThickness), _textHighLightGeometry);
            }
        }

        /// <summary>
        /// Create the outline geometry based on the formatted text.
        /// </summary>
        public void CreateText()
        {
            FontStyle fontStyle = FontStyles.Normal;
            FontWeight fontWeight = FontWeights.Medium;

            if (Bold == true) fontWeight = FontWeights.Bold;
            if (Italic == true) fontStyle = FontStyles.Italic;

            // Create the formatted text based on the properties set.
            FormattedText formattedText = new FormattedText(
                Text,
                CultureInfo.DefaultThreadCurrentUICulture ?? CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight,
                new Typeface(
                    Font,
                    fontStyle,
                    fontWeight,
                    FontStretches.Normal),
                FontSize,
                Brushes.Black // This brush does not matter since we use the geometry of the text. 
                );

            // Build the geometry object that represents the text.
            _textGeometry = formattedText.BuildGeometry(new Point(0, 0));

            // Build the geometry object that represents the text hightlight.
            if (Highlight == true)
            {
                _textHighLightGeometry = formattedText.BuildHighlightGeometry(new Point(0, 0));
            }
        }


        protected override Size MeasureOverride(Size availableSize)
        {
            return _textHighLightGeometry == null ? _textGeometry.Bounds.Size : _textHighLightGeometry.Bounds.Size;
        }

        #endregion

        #region DependencyProperties

        /// <summary>
        /// Specifies whether the font should display Bold font weight.
        /// </summary>
        public bool Bold
        {
            get
            {
                return (bool)GetValue(BoldProperty);
            }

            set
            {
                SetValue(BoldProperty, value);
            }
        }

        /// <summary>
        /// Identifies the Bold dependency property.
        /// </summary>
        public static readonly DependencyProperty BoldProperty = DependencyProperty.Register(
            "Bold",
            typeof(bool),
            typeof(OutlineTextControl),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.AffectsRender,
                new PropertyChangedCallback(OnOutlineTextInvalidated),
                null
                )
            );

        /// <summary>
        /// Specifies the brush to use for the fill of the formatted text.
        /// </summary>
        public Brush Fill
        {
            get
            {
                return (Brush)GetValue(FillProperty);
            }

            set
            {
                SetValue(FillProperty, value);
            }
        }

        /// <summary>
        /// Identifies the Fill dependency property.
        /// </summary>
        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
            "Fill",
            typeof(Brush),
            typeof(OutlineTextControl),
            new FrameworkPropertyMetadata(
                new SolidColorBrush(Colors.LightSteelBlue),
                FrameworkPropertyMetadataOptions.AffectsRender,
                new PropertyChangedCallback(OnOutlineTextInvalidated),
                null
                )
            );

        /// <summary>
        /// The font to use for the displayed formatted text.
        /// </summary>
        public FontFamily Font
        {
            get
            {
                return (FontFamily)GetValue(FontProperty);
            }

            set
            {
                SetValue(FontProperty, value);
            }
        }

        /// <summary>
        /// Identifies the Font dependency property.
        /// </summary>
        public static readonly DependencyProperty FontProperty = DependencyProperty.Register(
            "Font",
            typeof(FontFamily),
            typeof(OutlineTextControl),
            new FrameworkPropertyMetadata(
                new FontFamily("Arial"),
                FrameworkPropertyMetadataOptions.AffectsRender,
                new PropertyChangedCallback(OnOutlineTextInvalidated),
                null
                )
            );

        /// <summary>
        /// The current font size.
        /// </summary>
        public double FontSize
        {
            get
            {
                return (double)GetValue(FontSizeProperty);
            }

            set
            {
                SetValue(FontSizeProperty, value);
            }
        }

        /// <summary>
        /// Identifies the FontSize dependency property.
        /// </summary>
        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
            "FontSize",
            typeof(double),
            typeof(OutlineTextControl),
            new FrameworkPropertyMetadata(
                 (double)48.0,
                 FrameworkPropertyMetadataOptions.AffectsRender,
                 new PropertyChangedCallback(OnOutlineTextInvalidated),
                 null
                 )
            );

        /// <summary>
        /// Specifies whether to show the text highlight.
        /// </summary>
        public bool Highlight
        {
            get
            {
                return (bool)GetValue(HighlightProperty);
            }

            set
            {
                SetValue(HighlightProperty, value);
            }
        }

        /// <summary>
        /// Identifies the Hightlight dependency property.
        /// </summary>
        public static readonly DependencyProperty HighlightProperty = DependencyProperty.Register(
            "Highlight",
            typeof(bool),
            typeof(OutlineTextControl),
            new FrameworkPropertyMetadata(
                 false,
                 FrameworkPropertyMetadataOptions.AffectsRender,
                 new PropertyChangedCallback(OnOutlineTextInvalidated),
                 null
                 )
            );

        /// <summary>
        /// Specifies whether the font should display Italic font style.
        /// </summary>
        public bool Italic
        {
            get
            {
                return (bool)GetValue(ItalicProperty);
            }

            set
            {
                SetValue(ItalicProperty, value);
            }
        }

        /// <summary>
        /// Identifies the Italic dependency property.
        /// </summary>
        public static readonly DependencyProperty ItalicProperty = DependencyProperty.Register(
            "Italic",
            typeof(bool),
            typeof(OutlineTextControl),
            new FrameworkPropertyMetadata(
                 false,
                 FrameworkPropertyMetadataOptions.AffectsRender,
                 new PropertyChangedCallback(OnOutlineTextInvalidated),
                 null
                 )
            );

        /// <summary>
        /// Specifies the brush to use for the stroke and optional hightlight of the formatted text.
        /// </summary>
        public Brush Stroke
        {
            get
            {
                return (Brush)GetValue(StrokeProperty);
            }

            set
            {
                SetValue(StrokeProperty, value);
            }
        }

        /// <summary>
        /// Identifies the Stroke dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            "Stroke",
            typeof(Brush),
            typeof(OutlineTextControl),
            new FrameworkPropertyMetadata(
                 new SolidColorBrush(Colors.Teal),
                 FrameworkPropertyMetadataOptions.AffectsRender,
                 new PropertyChangedCallback(OnOutlineTextInvalidated),
                 null
                 )
            );

        /// <summary>
        ///     The stroke thickness of the font.
        /// </summary>
        public ushort StrokeThickness
        {
            get
            {
                return (ushort)GetValue(StrokeThicknessProperty);
            }

            set
            {
                SetValue(StrokeThicknessProperty, value);
            }
        }

        /// <summary>
        /// Identifies the StrokeThickness dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
            "StrokeThickness",
            typeof(ushort),
            typeof(OutlineTextControl),
            new FrameworkPropertyMetadata(
                 (ushort)0,
                 FrameworkPropertyMetadataOptions.AffectsRender,
                 new PropertyChangedCallback(OnOutlineTextInvalidated),
                 null
                 )
            );

        /// <summary>
        /// Specifies the text string to display.
        /// </summary>
        public string Text
        {
            get
            {
                return (string)GetValue(TextProperty);
            }

            set
            {
                SetValue(TextProperty, value);
            }
        }

        /// <summary>
        /// Identifies the Text dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text",
            typeof(string),
            typeof(OutlineTextControl),
            new FrameworkPropertyMetadata(
                 "",
                 FrameworkPropertyMetadataOptions.AffectsRender,
                 new PropertyChangedCallback(OnOutlineTextInvalidated),
                 null
                 )
            );

        #endregion
    }
}

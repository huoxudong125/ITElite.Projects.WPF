using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace ITElite.Projects.WPF.Controls.DeepZoom.Core
{
    public class HDImageTileSource : MultiScaleTileSource
    {
        private IList<DisplayRect> _displayRects;
        private string _imageExtension;
        private string imagePathTemplate;

        public HDImageTileSource(Uri sourceUri)
        {
            HdImagesSourrceUri = sourceUri;
        }

        #region Dependency Properties

        #region UriSource

        /// <summary>
        ///     UriSource Dependency Property
        /// </summary>
        public static readonly DependencyProperty HdImagesSourrceUriProperty =
            DependencyProperty.Register("HdImagesSourrceUri", typeof (Uri), typeof (HDImageTileSource),
                new FrameworkPropertyMetadata(null,
                    OnUriSourceChanged));

        /// <summary>
        ///     Gets or sets the source Uri of the DeepZoomImageTileSource.
        /// </summary>
        public Uri HdImagesSourrceUri
        {
            get { return (Uri) GetValue(HdImagesSourrceUriProperty); }
            set { SetValue(HdImagesSourrceUriProperty, value); }
        }

        /// <summary>
        ///     Handles changes to the UriSource property.
        /// </summary>
        private static void OnUriSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (HDImageTileSource) d;
            var oldUriSource = (Uri) e.OldValue;
            Uri newUriSource = target.HdImagesSourrceUri;
            target.OnUriSourceChanged(oldUriSource, newUriSource);
        }

        /// <summary>
        ///     Provides derived classes an opportunity to handle changes to the UriSource property.
        /// </summary>
        protected virtual void OnUriSourceChanged(Uri oldUriSource, Uri newUriSource)
        {
            LoadHdImageXml();

            InitializeTileSource();

            if (UriSourceChanged != null)
                UriSourceChanged(this, EventArgs.Empty);
        }

        public event EventHandler UriSourceChanged;

        #endregion UriSource

        #endregion Dependency Properties

        #region Overriden methods

        protected override int GetMaximumLevel(double width, double height)
        {
            return base.GetMaximumLevel(width, height) - (int) Math.Log(TileSize, ImagesGenerateStep) + 1;
        }

        protected internal override object GetTileLayers(int tileLevel, int tilePositionX, int tilePositionY)
        {
            if (!TileExists(tileLevel, tilePositionX, tilePositionY))
                return null;

            string source = HdImagesSourrceUri.OriginalString;

            string url = source.Substring(0,
                source.LastIndexOf("pyramid.xml", StringComparison.CurrentCultureIgnoreCase))
                         + imagePathTemplate.Replace("{l}", tileLevel.ToString())
                             .Replace("{c}", tilePositionX.ToString()).Replace("{r}", tilePositionY.ToString());

            var uriResult=new Uri(url, HdImagesSourrceUri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative);
         
            return uriResult;
        }

        #endregion Overriden methods

        #region Helper methods

        private void LoadHdImageXml()
        {
            XElement imageElement = XElement.Load(HdImagesSourrceUri.OriginalString);

            if (imageElement == null)
                throw new FileFormatException("Invalid XML file.");

            XNamespace xmlns = imageElement.GetDefaultNamespace();

            XElement imageSetElement = imageElement.Element(xmlns + "imageset");
            ;
            if (imageSetElement == null)
                throw new FileFormatException("Invalid XML file.");

            imagePathTemplate = imageSetElement.Attribute("url").Value;
            TileSize = (int) imageSetElement.Attribute("tileWidth");
            TileOverlap = (int) imageSetElement.Attribute("tileOverlap");
            _imageExtension = "tif"; //(string)imageElement.Attribute("Format");

            ImageSize = new Size(
                (int) imageSetElement.Attribute("width"),
                (int) imageSetElement.Attribute("height")
                );

            ImagesGenerateStep = (int) imageSetElement.Attribute("step");
            ZoomStep = (int) imageSetElement.Attribute("maxZoom");

            string displayRectsString = imageSetElement.Attribute("subRect").Value;
            if (!string.IsNullOrEmpty(displayRectsString))
            {
                string[] rectDetails = displayRectsString.Split(new[] {' '});
                _displayRects = new List<DisplayRect>
                {
                    new DisplayRect
                    {
                        MaxLevel = ((int) imageSetElement.Attribute("levels")),
                        MinLevel = 0,
                        Rect = new Rect(int.Parse(rectDetails[0]),
                            int.Parse(rectDetails[1])
                            , int.Parse(rectDetails[2]),
                            int.Parse(rectDetails[3]))
                    }
                };
            }
        }

        private bool TileExists(int level, int column, int row)
        {
            if (_displayRects == null) return true;

            double scale = ScaleAtLevel(level);

            foreach (DisplayRect dRect in _displayRects.Where(r => level >= r.MinLevel && level <= r.MaxLevel))
            {
                double minColumn = dRect.Rect.X*scale;
                double minRow = dRect.Rect.Y*scale;
                double maxColumn = minColumn + dRect.Rect.Width*scale;
                double maxRow = minRow + dRect.Rect.Height*scale;

                minColumn = Math.Floor(minColumn/TileSize);
                minRow = Math.Floor(minRow/TileSize);
                maxColumn = Math.Ceiling(maxColumn/TileSize);
                maxRow = Math.Ceiling(maxRow/TileSize);

                if (minColumn <= column && column < maxColumn &&
                    minRow <= row && row < maxRow)
                    return true;
            }

            return false;
        }

        #endregion Helper methods
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace ITElite.Projects.WPF.Controls.DeepZoom.Core
{
    /// <summary>
    /// Used to specify the source of a MultiScaleImage.
    /// </summary>
    public class DeepZoomImageTileSource : MultiScaleTileSource
    {
        private string _imageExtension;
        private IList<DisplayRect> _displayRects;

        public DeepZoomImageTileSource(Uri sourceUri)
        {
            UriSource = sourceUri;
        }

        #region Dependency Properties

        #region UriSource

        /// <summary>
        /// UriSource Dependency Property
        /// </summary>
        public static readonly DependencyProperty UriSourceProperty =
            DependencyProperty.Register("UriSource", typeof(Uri), typeof(DeepZoomImageTileSource),
                new FrameworkPropertyMetadata(null,
                    new PropertyChangedCallback(OnUriSourceChanged)));

        /// <summary>
        /// Gets or sets the source Uri of the DeepZoomImageTileSource.
        /// </summary>
        public Uri UriSource
        {
            get { return (Uri)GetValue(UriSourceProperty); }
            set { SetValue(UriSourceProperty, value); }
        }

        /// <summary>
        /// Handles changes to the UriSource property.
        /// </summary>
        private static void OnUriSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DeepZoomImageTileSource target = (DeepZoomImageTileSource)d;
            Uri oldUriSource = (Uri)e.OldValue;
            Uri newUriSource = target.UriSource;
            target.OnUriSourceChanged(oldUriSource, newUriSource);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the UriSource property.
        /// </summary>
        protected virtual void OnUriSourceChanged(Uri oldUriSource, Uri newUriSource)
        {
            LoadDeepZoomXml();

            InitializeTileSource();

            if (UriSourceChanged != null)
                UriSourceChanged(this, EventArgs.Empty);
        }

        public event EventHandler UriSourceChanged;

        #endregion UriSource

        #endregion Dependency Properties

        #region Overriden methods

        protected internal override object GetTileLayers(int tileLevel, int tilePositionX, int tilePositionY)
        {
            if (!TileExists(tileLevel, tilePositionX, tilePositionY))
                return null;

            var source = UriSource.OriginalString;
            var url = source.Substring(0, source.Length - 4) + "_files/"
                + tileLevel + "/" + tilePositionX + "_"
                + tilePositionY + "." + _imageExtension;
            return new Uri(url, UriSource.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative);
        }

        #endregion Overriden methods

        #region Helper methods

        private void LoadDeepZoomXml()
        {
            var imageElement = XElement.Load(UriSource.OriginalString);

            if (imageElement == null)
                throw new FileFormatException("Invalid XML file.");

            var xmlns = imageElement.GetDefaultNamespace();

            TileSize = (int)imageElement.Attribute("TileSize");
            TileOverlap = (int)imageElement.Attribute("Overlap");
            _imageExtension = (string)imageElement.Attribute("Format");

            var sizeElement = imageElement.Element(xmlns + "Size");
            if (sizeElement == null)
                throw new FileFormatException("Invalid XML file.");

            ImageSize = new Size(
                (int)sizeElement.Attribute("Width"),
                (int)sizeElement.Attribute("Height")
            );

            var displayRectsElement = imageElement.Element(xmlns + "DisplayRects");
            if (displayRectsElement != null)
            {
                _displayRects = displayRectsElement
                                    .Elements(xmlns + "DisplayRect")
                                    .Select(el =>
                                    {
                                        var rectElement = el.Element(xmlns + "Rect");
                                        var x = (double)rectElement.Attribute("X");
                                        var y = (double)rectElement.Attribute("Y");
                                        var width = (double)rectElement.Attribute("Width");
                                        var height = (double)rectElement.Attribute("Height");
                                        var minLevel = (int)el.Attribute("MinLevel");
                                        var maxLevel = (int)el.Attribute("MaxLevel");

                                        return new DisplayRect(x, y, width, height, minLevel, maxLevel);
                                    }).ToList();
            }
        }

        private bool TileExists(int level, int column, int row)
        {
            if (_displayRects == null) return true;

            var scale = ScaleAtLevel(level);

            foreach (var dRect in _displayRects.Where(r => level >= r.MinLevel && level <= r.MaxLevel))
            {
                var minColumn = dRect.Rect.X * scale;
                var minRow = dRect.Rect.Y * scale;
                var maxColumn = minColumn + dRect.Rect.Width * scale;
                var maxRow = minRow + dRect.Rect.Height * scale;

                minColumn = Math.Floor(minColumn / TileSize);
                minRow = Math.Floor(minRow / TileSize);
                maxColumn = Math.Ceiling(maxColumn / TileSize);
                maxRow = Math.Ceiling(maxRow / TileSize);

                if (minColumn <= column && column < maxColumn &&
                    minRow <= row && row < maxRow)
                    return true;
            }

            return false;
        }

        #endregion Helper methods
    }
}
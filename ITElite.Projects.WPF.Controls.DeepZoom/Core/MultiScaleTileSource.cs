using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace ITElite.Projects.WPF.Controls.DeepZoom.Core
{
    /// <summary>
    ///     Used to specify the source of Deep Zoom images.
    /// </summary>
    //[TypeConverter(typeof(HDImageTileSourceConverter))]
    [TypeConverter(typeof (DeepZoomImageTileSourceConverter))]
    public abstract class MultiScaleTileSource : DependencyObject
    {
        #region Abstract methods

        /// <summary>
        ///     Gets a Uri or Stream to be used as source for a given tile.
        /// </summary>
        /// <param name="tileLevel">Level of the tile.</param>
        /// <param name="tilePositionX">X-coordinate position of the tile.</param>
        /// <param name="tilePositionY">Y-coordinate position of the tile.</param>
        /// <returns>An Uri or Stream object that can be used as source for an image tile.</returns>
        /// <remarks>If this method returns a Uri, it will be used as UriSo</remarks>
        protected internal abstract object GetTileLayers(int tileLevel, int tilePositionX, int tilePositionY);

        #endregion Abstract methods

        #region Constructors

        /// <summary>
        ///     Exclusive for DeepZoomImageTileSource
        /// </summary>
        internal MultiScaleTileSource()
        {
            ZoomStep = 2;
            ImagesGenerateStep = 2;
        }

        public MultiScaleTileSource(long imageWidth, long imageHeight, int tileSize, int tileOverlap) : this()
        {
            ImageSize = new Size(imageWidth, imageHeight);
            TileSize = tileSize;
            TileOverlap = tileOverlap;

            InitializeTileSource();
        }

        public MultiScaleTileSource(int imageWidth, int imageHeight, int tileSize, int tileOverlap) :
            this(imageWidth, (long) imageHeight, tileSize, tileOverlap)
        {
        }

        internal void InitializeTileSource()
        {
            _maxLevel = GetMaximumLevel(ImageSize.Width, ImageSize.Height);
            _zoomLimitLevel = _maxLevel;
            CalculateLevelOffsets();
        }

        #endregion Constructors

        #region Internal methods

        /// <summary>
        ///     Returns the visible tiles inside a rectangle starting from a chosen
        ///     level and moving upwards until there are no missing tiles.
        /// </summary>
        /// <param name="rectangle">The rectangle in which tiles will be searched.</param>
        /// <param name="startingLevel">
        ///     The level where the search will start. If there are missing tiles in this level,
        ///     the next level will be rendered, until a level with no missing tiles is found.
        /// </param>
        /// <returns>The visible tiles inside the rectangle at the requested level.</returns>
        internal IEnumerable<Tile> VisibleTilesUntilFill(Rect rectangle, int startingLevel)
        {
            var visibleTiles = Enumerable.Empty<Tile>();

            /*
            // Algorithm : start on "closest" layer, go down until there are no more layers to paint
            // Advantage - optimizes loading, loads less images
            // Disadvantage - user will see holes while tiles are being downloaded, no transitions

            var level = startingLevel;
            var isVisibleAreaFullyPainted = false;
            while (level >= 0 && !isVisibleAreaFullyPainted)
            {
                var levelScale = ScaleAtLevel(level);
                var scaledBounds = new Rect(rectangle.X * levelScale, rectangle.Y * levelScale, rectangle.Width * levelScale, rectangle.Height * levelScale);
                var tilesInLevel = VisibleTiles(scaledBounds, level);
                isVisibleAreaFullyPainted = !tilesInLevel.Any(t => GetTileLayers(t.Level, t.Column, t.Row) == null); // If there are no missing tiles, area is fully painted
                visibleTiles = visibleTiles.Concat(tilesInLevel.Where(t => t != null));
                level--;
            }
            */

            // Algorithm : start on layer 0, go up - this is the algorithm used by Seadragon
            // Advantage - smoother loading, user doesn't see holes
            // Disadvantage - downloads up to 33% more data

            var levels = Enumerable.Range(0, startingLevel + 1);
            visibleTiles = levels.SelectMany(level =>
            {
                var levelScale = ScaleAtLevel(level);
                var scaledBounds = new Rect(rectangle.X*levelScale,
                    rectangle.Y*levelScale,
                    rectangle.Width*levelScale,
                    rectangle.Height*levelScale);
                return VisibleTiles(scaledBounds, level);
            });
            return visibleTiles;
        }

        internal Point GetTilePosition(int column, int row)
        {
            var offsetX = column == 0 ? 0 : TileOverlap;
            var offsetY = row == 0 ? 0 : TileOverlap;

            return new Point(column*TileSize - offsetX, row*TileSize - offsetY);
        }

        internal int GetLevel(double scaleRatio)
        {
            var level = _maxLevel + (int) Math.Log(scaleRatio, ImagesGenerateStep);
                // _maxLevel + (int)Math.Log(scaleRatio, 2);

            return level.Clamp(0, _zoomLimitLevel);
        }

        internal int GetLevel(double viewportWidth, double viewportHeight)
        {
            var originalAspectRatio = ImageSize.Width/ImageSize.Height;
            var viewportAspectRatio = viewportWidth/viewportHeight;

            var currentLevel = 0;
            if (viewportAspectRatio > originalAspectRatio)
            {
                while (ImageSizeAtLevel(currentLevel).Height < viewportHeight && currentLevel < _zoomLimitLevel)
                    currentLevel++;
            }
            else
            {
                while (ImageSizeAtLevel(currentLevel).Width < viewportWidth && currentLevel < _zoomLimitLevel)
                    currentLevel++;
            }
            return currentLevel;
        }

        internal double ScaleAtLevel(int level)
        {
            if (_levelScales.Count > level)
                return _levelScales[level];
            return Math.Pow((1.0/ImagesGenerateStep), _maxLevel - level); //Math.Pow(0.5, _maxLevel - level)
        }

        internal Size ImageSizeAtLevel(int level)
        {
            var scale = ScaleAtLevel(level);
            return new Size(
                Math.Ceiling(ImageSize.Width*scale),
                Math.Ceiling(ImageSize.Height*scale)
                );
        }

        internal int LevelOffset(int level)
        {
            return _levelOffsets[level];
        }

        internal int LevelFromOffset(long tileId)
        {
            var level = _levelOffsets.Count - 1;
            while (level > 0 && _levelOffsets[level] > tileId)
                level--;

            return level;
        }

        internal int ColumnsAtLevel(int level)
        {
            if (_columnCounts.Count > level)
                return _columnCounts[level];
            return (int) Math.Ceiling(ImageSizeAtLevel(level).Width/TileSize);
        }

        internal int RowsAtLevel(int level)
        {
            if (_rowCounts.Count > level)
                return _rowCounts[level];
            return (int) Math.Ceiling(ImageSizeAtLevel(level).Height/TileSize);
        }

        // Note: This uses an int because ZoomableCanvas uses ints to determine visible tiles.
        // Even though, you can store an image with more than 45000*45000 tiles on the last level.
        // It's possible to overflow this limit with huge sparse images that are close to the
        // 4 billion pixel limit from DeepZoom Composer.
        internal int GetTileIndex(Tile tile)
        {
            var rowsAtLevel = RowsAtLevel(tile.Level);
            var columnsAtLevel = ColumnsAtLevel(tile.Level);

            var levelOffset = LevelOffset(tile.Level);

            if (columnsAtLevel > rowsAtLevel)
                return levelOffset + columnsAtLevel*tile.Row + tile.Column;
            return levelOffset + rowsAtLevel*tile.Column + tile.Row;
        }

        internal Tile TileFromIndex(int index)
        {
            var level = LevelFromOffset(index);
            var levelOffset = LevelOffset(level);

            var rowsAtLevel = RowsAtLevel(level);
            var columnsAtLevel = ColumnsAtLevel(level);

            index -= levelOffset;

            var row = default(int);
            var column = default(int);

            if (columnsAtLevel > rowsAtLevel)
            {
                row = index/columnsAtLevel;
                column = index - row*columnsAtLevel;
            }
            else
            {
                column = index/rowsAtLevel;
                row = index - column*rowsAtLevel;
            }

            return new Tile(level, column, row);
        }

        #endregion Internal methods

        #region Protected helpers

        protected void CalculateLevelOffsets()
        {
            var offset = 0;
            for (var level = 0; level <= _maxLevel; level++)
            {
                _levelOffsets.Add(offset);
                _levelScales.Add(ScaleAtLevel(level));
                _rowCounts.Add(RowsAtLevel(level));
                _columnCounts.Add(ColumnsAtLevel(level));
                try
                {
                    checked
                    {
                        offset += TilesAtLevel(level); //TODO:Why to accumulative the offset
                    }
                }
                catch (OverflowException)
                {
                    // offset > MaxInt
                    _zoomLimitLevel = level - 1;
                    break;
                }
            }
        }

        protected virtual int GetMaximumLevel(double width, double height)
        {
            return (int) Math.Ceiling(Math.Log(Math.Max(width, height), ImagesGenerateStep));
            //(int)Math.Ceiling(Math.Log(Math.Max(width, height), 2))
        }

        protected int TilesAtLevel(int level)
        {
            checked
            {
                return ColumnsAtLevel(level)*RowsAtLevel(level);
            }
        }

        #endregion Protected helpers

        #region Private helpers

        // Returns the visible tiles inside a rectangle on any level
        private IEnumerable<Tile> VisibleTiles(Rect rectangle, int level)
        {
            rectangle.Intersect(new Rect(ImageSize));

            var top = Math.Floor(rectangle.Top/TileSize);
            var left = Math.Floor(rectangle.Left/TileSize);
            var right = Math.Ceiling(rectangle.Right/TileSize);
            var bottom = Math.Ceiling(rectangle.Bottom/TileSize);

            right = right.AtMost(ColumnsAtLevel(level));
            bottom = bottom.AtMost(RowsAtLevel(level));

            var width = (right - left).AtLeast(0);
            var height = (bottom - top).AtLeast(0);

            if (top == 0.0 && left == 0.0 && width == 1.0 && height == 1.0) // This level only has one tile
                yield return new Tile(level, 0, 0);
            else
            {
                foreach (var pt in Quadivide(new Rect(left, top, width, height)))
                    yield return new Tile(level, (int) pt.X, (int) pt.Y);
            }
        }

        private static IEnumerable<Point> Quadivide(Rect area)
        {
            if (area.Width > 0 && area.Height > 0)
            {
                var center = area.GetCenter();

                var x = Math.Floor(center.X);
                var y = Math.Floor(center.Y);

                yield return new Point(x, y);

                var quad1 = new Rect(area.TopLeft, new Point(x, y + 1));
                var quad2 = new Rect(area.TopRight, new Point(x, y));
                var quad3 = new Rect(area.BottomLeft, new Point(x + 1, y + 1));
                var quad4 = new Rect(area.BottomRight, new Point(x + 1, y));

                var quads = new Queue<IEnumerator<Point>>();
                quads.Enqueue(Quadivide(quad1).GetEnumerator());
                quads.Enqueue(Quadivide(quad2).GetEnumerator());
                quads.Enqueue(Quadivide(quad3).GetEnumerator());
                quads.Enqueue(Quadivide(quad4).GetEnumerator());
                while (quads.Count > 0)
                {
                    var quad = quads.Dequeue();
                    if (quad.MoveNext())
                    {
                        yield return quad.Current;
                        quads.Enqueue(quad);
                    }
                }
            }
        }

        #endregion Private helpers

        #region Private Fields

        private readonly IList<int> _columnCounts = new List<int>();
        private readonly IList<int> _levelOffsets = new List<int>();
        private readonly IList<double> _levelScales = new List<double>();
        private readonly IList<int> _rowCounts = new List<int>();
        private int _maxLevel;
        private int _zoomLimitLevel;

        #endregion Private Fields

        #region Protected Properties

        public Size ImageSize { get; protected set; }

        protected internal int TileSize { get; set; }

        protected internal int TileOverlap { get; set; }

        protected internal int ZoomStep { get; set; }

        protected internal int ImagesGenerateStep { get; set; }

        #endregion Protected Properties
    }
}
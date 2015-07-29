using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ITElite.Projects.WPF.Controls.DeepZoom.Core
{
    /// <summary>
    ///     A spatial items source that is able to find and cache visible image
    ///     tiles in a the screen. Used in conjuction with ZoomableCanvas.
    /// </summary>
    internal class MultiScaleImageSpatialItemsSource :
        IList,
        ZoomableCanvas.ISpatialItemsSource
    {
        private const int CacheCapacity = 300; // limit cache to 300 tiles
        private static readonly object CacheLock = new object();
        private readonly Queue<string> _cachedTiles = new Queue<string>(CacheCapacity);
        private readonly Dictionary<string, BitmapSource> _tileCache = new Dictionary<string, BitmapSource>();
        private readonly MultiScaleTileSource _tileSource;
        private CancellationTokenSource _currentCancellationTokenSource = new CancellationTokenSource();
        private int _currentLevel;

        public MultiScaleImageSpatialItemsSource(MultiScaleTileSource tileSource)
        {
            _tileSource = tileSource;
        }

        public int CurrentLevel
        {
            get { return _currentLevel; }
            set
            {
                if (value == _currentLevel) return;

                // Cancel all download tasks
                _currentCancellationTokenSource.Cancel();
                _currentCancellationTokenSource = new CancellationTokenSource();

                _currentLevel = value;
            }
        }

        public int ZoomStep
        {
            get { return _tileSource.ZoomStep; }
        }

        public object this[int i]
        {
            get
            {
                var tile = _tileSource.TileFromIndex(i);
                var tileId = tile.ToString();

                if (_tileCache.ContainsKey(tileId))
                    return new VisualTile(tile, _tileSource, _tileCache[tileId]);

                var tileVm = new VisualTile(tile, _tileSource);

                var imageSource = _tileSource.GetTileLayers(tile.Level, tile.Column, tile.Row);

                var uri = imageSource as Uri;
                if (uri != null)
                {
                    // Capture closure
                    var token = _currentCancellationTokenSource.Token;
                    Task.Factory
                        .StartNew(() =>
                        {
                            var source = ImageLoader.LoadImage(uri);
                            if (source != null)
                                source = CacheTile(tileId, source);
                            return source;
                        }, token, TaskCreationOptions.None, TaskScheduler.Default) //TODO: change to Task.Run
                        .ContinueWith(t =>
                        {
                            if (t.Result != null)
                            {
                                tileVm.Source = t.Result;
                            }
                        }, TaskContinuationOptions.OnlyOnRanToCompletion);
                }
                else
                {
                    var stream = imageSource as Stream;
                    if (stream != null)
                    {
                        var source = new BitmapImage();
                        source.BeginInit();
                        source.CacheOption = BitmapCacheOption.OnLoad;
                        source.StreamSource = stream;
                        source.EndInit();

                        var src = CacheTile(tileId, source);
                        tileVm.Source = src;
                    }
                    else return null;
                }

                return tileVm;
            }
            set { throw new NotSupportedException(); }
        }

        public void InvalidateSource()
        {
            if (ExtentChanged != null)
                ExtentChanged(this, EventArgs.Empty);
            if (QueryInvalidated != null)
                QueryInvalidated(this, EventArgs.Empty);
        }

        private BitmapSource CacheTile(string tileId, BitmapSource source)
        {
            lock (CacheLock)
            {
                if (_tileCache.ContainsKey(tileId))
                    return _tileCache[tileId];


                if (_cachedTiles.Count >= CacheCapacity)
                {
                    _tileCache.Remove(_cachedTiles.Dequeue());
                }
                _cachedTiles.Enqueue(tileId);
                _tileCache.Add(tileId, source);
            }
            return source;
        }

        #region  ISpatialItems Source members

        public Rect Extent
        {
            get { return new Rect(_tileSource.ImageSize); }
        }

        public IEnumerable<int> Query(Rect rectangle)
        {
            return _tileSource.VisibleTilesUntilFill(rectangle, CurrentLevel)
                .Select(t => _tileSource.GetTileIndex(t));
        }

        public event EventHandler ExtentChanged;

        public event EventHandler QueryInvalidated;

        #endregion ISpatialItemsSource members

        #region Irrelevant IList Members

        int IList.Add(object value)
        {
            return 0;
        }

        void IList.Clear()
        {
        }

        bool IList.Contains(object value)
        {
            return false;
        }

        int IList.IndexOf(object value)
        {
            return 0;
        }

        void IList.Insert(int index, object value)
        {
        }

        void IList.Remove(object value)
        {
        }

        void IList.RemoveAt(int index)
        {
        }

        void ICollection.CopyTo(Array array, int index)
        {
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        bool IList.IsReadOnly
        {
            get { return true; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return null; }
        }

        int ICollection.Count
        {
            get { return int.MaxValue; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield break;
        }

        #endregion Irrelevant IList Members
    }
}
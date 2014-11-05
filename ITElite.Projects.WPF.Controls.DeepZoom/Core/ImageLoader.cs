using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ITElite.Projects.WPF.Controls.DeepZoom.Core
{
    public static class ImageLoader
    {
        /// <summary>
        /// Loads an image from a given Uri, synchronously.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static BitmapSource LoadImage(Uri uri)
        {
            try
            {
                var bi = new BitmapImage();
                MemoryStream mem;
                using (var client = new WebClient())
                {
                    var buffer = client.DownloadData(uri);
                    mem = new MemoryStream(buffer);
                }
                bi.BeginInit();
                bi.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bi.CacheOption = BitmapCacheOption.None;
                bi.StreamSource = mem;
                bi.EndInit();
                bi.Freeze();

                return bi;
            }
            catch (WebException)
            {
                // Server error or image not found, do nothing
            }
            catch (FileNotFoundException)
            {
                // Local file not found, do nothing
            }
            catch (FileFormatException)
            {
                // Corrupted image, do nothing
            }
            return null;
        }
    }
}

using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace ITElite.Projects.WPF.IO.DeepZoom
{
    public enum ImageType
    {
        Png,
        Jpeg
    }

    public class DeepZoomCreator
    {
        /// <summary>
        ///     Specifies the output filetype
        /// </summary>
        private ImageType imageType = ImageType.Jpeg;

        /// <summary>
        ///     Create a deep zoom image from a single source image
        /// </summary>
        /// <param name="sourceImage">Source image path</param>
        /// <param name="destinationImage">Destination path (must be .dzi or .xml)</param>
        public void CreateSingleComposition(string sourceImage, string destinationImage, ImageType type)
        {
            imageType = type;
            var source = sourceImage;
            var destDirectory = Path.GetDirectoryName(destinationImage);
            var leafname = Path.GetFileNameWithoutExtension(destinationImage);
            var root = Path.Combine(destDirectory, leafname);
            ;
            var filesdir = root + "_files";

            Directory.CreateDirectory(filesdir);
            var img = new BitmapImage(new Uri(source));
            double dWidth = img.PixelWidth;
            double dHeight = img.PixelHeight;
            var AspectRatio = dWidth/dHeight;

            // The Maximum level for the pyramid of images is
            // Log2(maxdimension)

            var maxdimension = Math.Max(dWidth, dHeight);
            var logvalue = Math.Log(maxdimension, 2);
            var MaxLevel = (int) Math.Ceiling(logvalue);
            var topleveldir = Path.Combine(filesdir, MaxLevel.ToString());

            // Create the directory for the top level tiles
            Directory.CreateDirectory(topleveldir);

            // Calculate how many tiles across and down
            var maxcols = img.PixelWidth/256;
            var maxrows = img.PixelHeight/256;

            // Get the bounding rectangle of the source image, for clipping
            var MainRect = new Rect(0, 0, img.PixelWidth, img.PixelHeight);
            for (var j = 0; j <= maxrows; j++)
            {
                for (var i = 0; i <= maxcols; i++)
                {
                    // Calculate the bounds of the tile
                    // including a 1 pixel overlap each side
                    var smallrect = new Rect((double) (i*256) - 1, (double) (j*256) - 1, 258.0, 258.0);

                    // Adjust for the rectangles at the edges by intersecting
                    smallrect.Intersect(MainRect);

                    // We want a RenderTargetBitmap to render this tile into
                    // Create one with the dimensions of this tile
                    var outbmp = new RenderTargetBitmap((int) smallrect.Width, (int) smallrect.Height, 96, 96,
                        PixelFormats.Pbgra32);
                    var visual = new DrawingVisual();
                    var context = visual.RenderOpen();

                    // Set the offset of the source image into the destination bitmap
                    // and render it
                    var rect = new Rect(-smallrect.Left, -smallrect.Top, img.PixelWidth, img.PixelHeight);
                    context.DrawImage(img, rect);
                    context.Close();
                    outbmp.Render(visual);

                    // Save the bitmap tile
                    var destination = Path.Combine(topleveldir, string.Format("{0}_{1}", i, j));
                    EncodeBitmap(outbmp, destination);

                    // null out everything we've used so the Garbage Collector
                    // knows they're free. This could easily be voodoo since they'll go
                    // out of scope, but it can't hurt.
                    outbmp = null;
                    context = null;
                    visual = null;
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            // clear the source image since we don't need it anymore
            img = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Now render the lower levels by rendering the tiles from the level
            // above to the next level down
            for (var level = MaxLevel - 1; level >= 0; level--)
            {
                RenderSubtiles(filesdir, dWidth, dHeight, MaxLevel, level);
            }

            // Now generate the .dzi file

            var format = "png";
            if (imageType == ImageType.Jpeg)
            {
                format = "jpg";
            }

            var dzi = new XElement("Image",
                new XAttribute("TileSize", 256),
                new XAttribute("Overlap", 1),
                new XAttribute("Format", format), // xmlns="http://schemas.microsoft.com/deepzoom/2008">
                new XElement("Size",
                    new XAttribute("Width", dWidth),
                    new XAttribute("Height", dHeight)),
                new XElement("DisplayRects",
                    new XElement("DisplayRect",
                        new XAttribute("MinLevel", 1),
                        new XAttribute("MaxLevel", MaxLevel),
                        new XElement("Rect",
                            new XAttribute("X", 0),
                            new XAttribute("Y", 0),
                            new XAttribute("Width", dWidth),
                            new XAttribute("Height", dHeight)))));
            dzi.Save(destinationImage);
        }

        /// <summary>
        ///     Save the output bitmap as either Png or Jpeg
        /// </summary>
        /// <param name="outbmp">Bitmap to save</param>
        /// <param name="destination">Path to save to, without the file extension</param>
        private void EncodeBitmap(RenderTargetBitmap outbmp, string destination)
        {
            if (imageType == ImageType.Png)
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(outbmp));
                var fs = new FileStream(destination + ".png", FileMode.Create);
                encoder.Save(fs);
                fs.Close();
            }
            else
            {
                var encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = 95;
                encoder.Frames.Add(BitmapFrame.Create(outbmp));
                var fs = new FileStream(destination + ".jpg", FileMode.Create);
                encoder.Save(fs);
                fs.Close();
            }
        }

        /// <summary>
        ///     Render the subtiles given a fully rendered top-level
        /// </summary>
        /// <param name="subfiles">Path to the xxx_files directory</param>
        /// <param name="imageWidth">Width of the source image</param>
        /// <param name="imageHeight">Height of the source image</param>
        /// <param name="maxlevel">Top level of the tileset</param>
        /// <param name="desiredlevel">
        ///     Level we want to render. Note it requires
        ///     that the level above this has already been rendered.
        /// </param>
        private void RenderSubtiles(string subfiles, double imageWidth, double imageHeight, int maxlevel,
            int desiredlevel)
        {
            var formatextension = ".png";
            if (imageType == ImageType.Jpeg)
            {
                formatextension = ".jpg";
            }
            var uponelevel = desiredlevel + 1;
            var desiredfactor = Math.Pow(2, maxlevel - desiredlevel);
            var higherfactor = Math.Pow(2, maxlevel - (desiredlevel + 1));
            var renderlevel = Path.Combine(subfiles, desiredlevel.ToString());
            Directory.CreateDirectory(renderlevel);
            var upperlevel = Path.Combine(subfiles, (desiredlevel + 1).ToString());

            // Calculate the tiles we want to translate down
            var MainBounds = new Rect(0, 0, imageWidth, imageHeight);
            var OriginalRect = new Rect(0, 0, imageWidth, imageHeight);

            // Scale down this rectangle to the scale factor of the level we want
            MainBounds.X = Math.Ceiling(MainBounds.X/desiredfactor);
            MainBounds.Y = Math.Ceiling(MainBounds.Y/desiredfactor);
            MainBounds.Width = Math.Ceiling(MainBounds.Width/desiredfactor);
            MainBounds.Height = Math.Ceiling(MainBounds.Height/desiredfactor);

            var lowx = (int) Math.Floor(MainBounds.X/256);
            var lowy = (int) Math.Floor(MainBounds.Y/256);
            var highx = (int) Math.Floor(MainBounds.Right/256);
            var highy = (int) Math.Floor(MainBounds.Bottom/256);

            for (var x = lowx; x <= highx; x++)
            {
                for (var y = lowy; y <= highy; y++)
                {
                    var smallrect = new Rect((double) (x*256) - 1, (double) (y*256) - 1, 258.0, 258.0);
                    smallrect.Intersect(MainBounds);
                    var outbmp = new RenderTargetBitmap((int) smallrect.Width, (int) smallrect.Height, 96, 96,
                        PixelFormats.Pbgra32);
                    var visual = new DrawingVisual();
                    var context = visual.RenderOpen();

                    // Calculate the bounds of this tile

                    var rect = smallrect;
                    // This is the rect of this tile. Now render any appropriate tiles onto it
                    // The upper level tiles are twice as big, so they have to be shrunk down

                    var scaledRect = new Rect(rect.X*2, rect.Y*2, rect.Width*2, rect.Height*2);
                    for (var tx = lowx*2; tx <= highx*2 + 1; tx++)
                    {
                        for (var ty = lowy*2; ty <= highy*2 + 1; ty++)
                        {
                            // See if this tile overlaps
                            var subrect = GetTileRectangle(tx, ty);
                            if (scaledRect.IntersectsWith(subrect))
                            {
                                subrect.X -= scaledRect.X;
                                subrect.Y -= scaledRect.Y;
                                RenderTile(context, Path.Combine(upperlevel, tx + "_" + ty + formatextension), subrect);
                            }
                        }
                    }
                    context.Close();
                    outbmp.Render(visual);

                    // Render the completed tile and clear all resources used
                    var destination = Path.Combine(renderlevel, string.Format(@"{0}_{1}", x, y));
                    EncodeBitmap(outbmp, destination);
                    outbmp = null;
                    visual = null;
                    context = null;
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        ///     Get the bounds of the given tile rectangle
        /// </summary>
        /// <param name="x">x index of the tile</param>
        /// <param name="y">y index of the tile</param>
        /// <returns>Bounding rectangle for the tile at the given indices</returns>
        private static Rect GetTileRectangle(int x, int y)
        {
            var rect = new Rect(256*x - 1, 256*y - 1, 258, 258);
            if (x == 0)
            {
                rect.X = 0;
                rect.Width = rect.Width - 1;
            }
            if (y == 0)
            {
                rect.Y = 0;
                rect.Width = rect.Width - 1;
            }

            return rect;
        }

        /// <summary>
        ///     Render the given tile rectangle, shrunk down by half to fit the next
        ///     lower level
        /// </summary>
        /// <param name="context">DrawingContext for the DrawingVisual to render into</param>
        /// <param name="path">path to the tile we're rendering</param>
        /// <param name="rect">Rectangle to render this tile.</param>
        private void RenderTile(DrawingContext context, string path, Rect rect)
        {
            if (File.Exists(path))
            {
                var img = new BitmapImage(new Uri(path));
                rect = new Rect(rect.X/2.0, rect.Y/2.0, img.PixelWidth/2.0, img.PixelHeight/2.0);
                context.DrawImage(img, rect);
            }
        }
    }
}
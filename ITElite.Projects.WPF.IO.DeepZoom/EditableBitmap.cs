using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ITElite.Projects.WPF.IO.DeepZoom
{
    /// <summary>
    ///     Editable bitmap class created by Justin Dunlap, published by the author on www.codeproject.com:
    ///     http://www.codeproject.com/KB/graphics/fastimagedrawing.aspx
    ///     The code is fully Justin's, except for an addition to one of the constructors:
    ///     I have added bool bSmoothScaling as parameter to the constructor that uses a rescaled
    ///     source image, to allow producing slightly nicer looking scaled bitmaps.
    /// </summary>
    public class EditableBitmap : IDisposable
    {
        private readonly SharedPinnedByteArray byteArray;

        /// <summary>
        ///     Creates a new EditableBitmap with the specified pixel format,
        ///     and copies the bitmap passed in onto the buffer.
        /// </summary>
        /// <param name="source">The bitmap to copy from.</param>
        /// <param name="format">The PixelFormat for the new bitmap.</param>
        public EditableBitmap(Bitmap source, PixelFormat format)
            : this(source.Width, source.Height, format)
        {
            //NOTE: This ONLY preserves the first frame of the image.
            //It does NOT copy EXIF properties, multiple frames, etc.
            //In places where preserving them is necessary, it must 
            //be done manually.
            var g = Graphics.FromImage(Bitmap);
            g.DrawImageUnscaledAndClipped(source, new Rectangle(0, 0, source.Width, source.Height));
            g.Dispose();
        }

        /// <summary>
        ///     Creates a new EditableBitmap with the specified pixel format and size,
        ///     and copies the bitmap passed in onto the buffer. The source bitmap is stretched to
        ///     fit the new size.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="format"></param>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        /// <param name="bSmoothScaling"></param>
        public EditableBitmap(Bitmap source, PixelFormat format, int newWidth, int newHeight, bool bSmoothScaling)
            : this(newWidth, newHeight, format)
        {
            //NOTE: This ONLY preserves the first frame of the image.
            //It does NOT copy EXIF properties, multiple frames, etc.
            //In places where preserving them is necessary, it must 
            //be done manually.
            using (var g = Graphics.FromImage(Bitmap))
            {
                if (bSmoothScaling)
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(source, 0, 0, newWidth, newHeight);
                g.Dispose();
            }
        }

        /// <summary>
        ///     Creates a new EditableBitmap containing a copy of the specified source bitmap.
        /// </summary>
        /// <param name="source"></param>
        public EditableBitmap(Bitmap source)
            : this(source, source.PixelFormat)
        {
        }

        /// <summary>
        ///     Creates a new, blank EditableBitmap with the specified width, height, and pixel format.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="format"></param>
        public EditableBitmap(int width, int height, PixelFormat format)
        {
            PixelFormatSize = Image.GetPixelFormatSize(format)/8;
            Stride = width*PixelFormatSize;
            var padding = (Stride%4);
            Stride += padding == 0 ? 0 : 4 - padding; //pad out to multiple of 4
            byteArray = new SharedPinnedByteArray(Stride*height);
            Bitmap = new Bitmap(width, height, Stride, format, byteArray.bitPtr);
        }

        /// <summary>
        ///     Gets the pixel format size in bytes (not bits, as with Image.GetPixelFormatSize()).
        /// </summary>
        public int PixelFormatSize { get; private set; }

        /// <summary>
        ///     Gets the stride of the bitmap.
        /// </summary>
        public int Stride { get; private set; }

        /// <summary>
        ///     Gets the underlying <see cref="System.Drawing.Bitmap" />
        ///     that this EditableBitmap wraps.
        /// </summary>
        public Bitmap Bitmap { get; set; }

        /// <summary>
        ///     Gets an array that contains the bitmap bit buffer.
        /// </summary>
        public byte[] Bits
        {
            get { return byteArray.bits; }
        }

        /// <summary>
        ///     The <see cref="EditableBitmap" /> that this <see cref="EditableBitmap" /> is a view on.
        ///     This property's value will be null if this EditableBitmap is not a view on another
        ///     <see cref="EditableBitmap" />.
        /// </summary>
        public EditableBitmap Owner { get; private set; }

        /// <summary>
        ///     Gets a safe pointer to the buffer containing the bitmap bits.
        /// </summary>
        public IntPtr BitPtr
        {
            get { return byteArray.bitPtr; }
        }

        public bool Disposed { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        protected void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            Bitmap.Dispose();
            byteArray.ReleaseReference();
            Disposed = true;

            //Set managed object refs to null if explicitly disposing, so that they can be cleaned up by the GC.
            if (disposing)
            {
                Owner = null;
                Bitmap = null;
            }
        }

        ~EditableBitmap()
        {
            Dispose(false);
        }

        #region View Support

        /// <summary>
        ///     Creates an <see cref="EditableBitmap" /> as a view on a section of an existing <see cref="EditableBitmap" />.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="viewArea"></param>
        protected EditableBitmap(EditableBitmap source, Rectangle viewArea)
        {
            Owner = source;
            PixelFormatSize = source.PixelFormatSize;
            byteArray = source.byteArray;
            byteArray.AddReference();
            Stride = source.Stride;

            try
            {
                StartOffset = source.StartOffset + (Stride*viewArea.Y) + (viewArea.X*PixelFormatSize);
                Bitmap = new Bitmap(viewArea.Width, viewArea.Height, Stride, source.Bitmap.PixelFormat,
                    (IntPtr) (((int) byteArray.bitPtr) + StartOffset));
            }
            finally
            {
                if (Bitmap == null)
                    byteArray.ReleaseReference();
            }
        }

        /// <summary>
        ///     Creates an <see cref="EditableBitmap" /> as a view on a section of an existing <see cref="EditableBitmap" />.
        /// </summary>
        /// <param name="viewArea">The area that should form the bounds of the view.</param>
        public EditableBitmap CreateView(Rectangle viewArea)
        {
            if (Disposed)
                throw new ObjectDisposedException("this");
            return new EditableBitmap(this, viewArea);
        }

        /// <summary>
        ///     If this <see cref="EditableBitmap" /> is a view on another <see cref="EditableBitmap" /> instance,
        ///     this property gets the index where the pixels that are within the view's pixel area start.
        /// </summary>
        public int StartOffset { get; private set; }

        #endregion
    }

    internal class SharedPinnedByteArray
    {
        internal IntPtr bitPtr;
        internal byte[] bits;
        private bool destroyed;
        internal GCHandle handle;
        private int refCount;

        public SharedPinnedByteArray(int length)
        {
            bits = new byte[length];
            // if not pinned the GC can move around the array
            handle = GCHandle.Alloc(bits, GCHandleType.Pinned);
            bitPtr = Marshal.UnsafeAddrOfPinnedArrayElement(bits, 0);
            refCount++;
        }

        internal void AddReference()
        {
            refCount++;
        }

        internal void ReleaseReference()
        {
            refCount--;
            if (refCount <= 0)
                Destroy();
        }

        private void Destroy()
        {
            if (!destroyed)
            {
                handle.Free();
                bits = null;
                destroyed = true;
            }
        }

        ~SharedPinnedByteArray()
        {
            Destroy();
        }
    }
}
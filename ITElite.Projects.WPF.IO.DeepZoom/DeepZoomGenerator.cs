// This code has been changed by J�rg Lang (lang.joerg@gmail.com) from a class 
// called Decos.DeepZoom that was made 2008 by Berend Engelbrecht, b.engelbrecht@gmail.com
// The original code can be found at www.codeproject.com
//
// The changes made are mainly taking out all stuff that seemed to be product
// specific to a product by the original creator of the code and that was not
// needed to create tiles for a single image.
// The main change is of course, that the images get stored in a database.
//
// This source code is licensed for commercial and non-commercial use under the 
// Code Project Open License (CPOL) 1.02  http://www.codeproject.com/info/cpol10.aspx
//  

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;

namespace ITElite.Projects.WPF.IO.DeepZoom
{
    /// <summary>
    ///     GenerateDeepZoom encapsulates code used to generate a DeepZoom collection from a
    ///     set of scan pages. This class does not contain any Windows Forms related or user
    ///     interface code. The class can be compiled in dotnet 2.0 and higher.
    ///     The main entry point of this class is the GenerateFromScanFile method. This
    ///     method is an instance function, because we need an instance of the class to
    ///     support progress events.
    ///     General methods that are needed both in GenerateDeepZoom and in the PageBitmap
    ///     class (see further below) are all implemented as static functions, so that
    ///     a PageBitmap object does not need a reference to the GenerateDeepZoom object
    ///     that contains it.
    /// </summary>
    public class GenerateDeepZoom
    {
        /// <summary>
        ///     Delegate for GenerateDeepZoomProgress event.
        /// </summary>
        /// <param name="sStatus">Current status</param>
        /// <param name="iProgressPercentage">Progress percentage (0 - 100)</param>
        public delegate void GenerateDeepZoomProgress(string sStatus, int iProgressPercentage);

        /// <summary>Size in pixels of DeepZoom image tiles</summary>
        internal const int TILESIZE = 256;

        /// <summary>Overlap in pixels for DeepZoom image tiles</summary>
        internal const int TILEOVERLAP = 1;

        internal const int PROGRESSPERIMAGE = 32;
        private static int m_iJpegQuality = 90;
        private static PixelFormat m_PixelFormat = PixelFormat.Format16bppRgb555;
        private static ImageCodecInfo m_JpegCodec;
        private int m_iProgressEndPercentage;
        private int m_iProgressPercentage = -1;
        private int m_iProgressStartPercentage;
        private int m_iProgressSteps;
        // progress event support
        private string m_sProgressStatus = string.Empty;

        /// <summary>JPEG quality used for jpg image tiles, must be between 1 and 100</summary>
        public static int JpegQuality
        {
            get { return m_iJpegQuality; }
            set { m_iJpegQuality = value; }
        }

        /// <summary>PixelFormat used in memory bitmaps</summary>
        public static PixelFormat PixelFormat
        {
            get { return m_PixelFormat; }
            set { m_PixelFormat = value; }
        }

        /// <summary>
        ///     Add your handler to OnGenerateDeepZoomProgress to be informed of DeepZoom
        ///     generation progress.
        /// </summary>
        public event GenerateDeepZoomProgress OnGenerateDeepZoomProgress;

        /// <summary>
        ///     Generates a deepzoom collection from a "scan file" (either a collection of single page
        ///     image files or one multipage tiff file).
        ///     A Decos DSI scan file collection [name].dsi is assumed to point to single page images
        ///     [name].000, [name].001, ... [name].nnn. GenerateFromScanFile will simply iterate file
        ///     names until the first one that doesn't exist. The page image files can be anything
        ///     that System.Drawing.Bitmap allows in its constructor, usually tiff or jpg.
        /// </summary>
        /// <param name="sScanPath">Full path to the scan file</param>
        /// <param name="sDeepZoomPath">Deepzoom output path, should point to the GeneratedImages subdirectory</param>
        /// <returns>true on success</returns>
        public bool GenerateFromScanFile(string sScanPath, string sDeepZoomPath)
        {
            int iPage = 0, iPageCount = 999;
            var fMaxWidth = 0.0F;
            var fTotalHeight = 0.0F;
            var alBitmaps = new ArrayList();
            string sPage;
            var bUseJpeg = false;
            var bSuccess = false;

            SetStatus("Reading image file(s)", 0, 1, 1);
            var sExt = Path.GetExtension(sScanPath);
            var bIsDecosScan = (string.Compare(sExt, ".DSI", true) == 0);
            Bitmap bmPages;
            if (bIsDecosScan)
            {
                bmPages = null; // For a decos scan we retrieve single page bitmaps one by one
                sScanPath = sScanPath.Substring(0, sScanPath.Length - 4);
            }
            else
            {
                bmPages = new Bitmap(sScanPath);
                iPageCount = bmPages.GetFrameCount(FrameDimension.Page);
            }

            try
            {
                PageBitmap pbm;
                for (iPage = 0; iPage < iPageCount; iPage++)
                {
                    if (bIsDecosScan)
                    {
                        sPage = sScanPath + "." + iPage.ToString("000");
                        if (File.Exists(sPage))
                            pbm = new PageBitmap(sPage);
                        else
                            break; // no more pages
                    }
                    else
                    {
                        pbm = new PageBitmap(sScanPath, bmPages, iPage, iPageCount);
                    }
                    if (fMaxWidth < pbm.PhysWidth) fMaxWidth = pbm.PhysWidth;
                    pbm.PhysTop = fTotalHeight;
                    fTotalHeight += (pbm.PhysHeight*1.05F); // use 5% of page height as gap between pages
                    alBitmaps.Add(pbm);
                    if ((pbm.bm.PixelFormat != PixelFormat.Format1bppIndexed) && (pbm.ImageFormat != ImageFormat.Png))
                        bUseJpeg = true;
                    pbm.OnPageBitmapProgress += pbm_OnPageBitmapProgress;
                }
            }
            catch (Exception ex)
            {
                Trace.Write(ex);
            }
            if ((bmPages != null) && (iPageCount > 1))
            {
                // we cloned the separate pages, so we cna get rid of the original now
                bmPages.Dispose();
                bmPages = null;
            }
            SetProgress(1);

            // After reading in the file(s) we should have at least one valid bitmap
            // to be able to continue.
            if (alBitmaps.Count > 0)
            {
                // Generate full deepzoom image sets for each page. We keep the highest level
                // that allows to show every page in one single tile, this will be used to generate
                // collection thumbnails.
                var iFullPageLevel = int.MaxValue;
                var iStartpercentage = 1;
                var iEndPercentage = 1;
                for (var iPbm = 0; iPbm < alBitmaps.Count; iPbm++)
                {
                    iStartpercentage = iEndPercentage;
                    iEndPercentage = 1 + (90*(iPbm + 1))/alBitmaps.Count;
                    SetStatus("Creating DeepZoom image " + (iPbm + 1) + " of " + alBitmaps.Count,
                        iStartpercentage, iEndPercentage, PROGRESSPERIMAGE);
                    var pbm = (PageBitmap) alBitmaps[iPbm];
                    pbm.CreateDeepZoomImage(sDeepZoomPath, bUseJpeg);
                    if (pbm.FullPageLevel < iFullPageLevel)
                        iFullPageLevel = pbm.FullPageLevel;
                }

                // Create collection thumbnails. How that should be done is explained here:
                // http://msdn.microsoft.com/en-us/library/cc645077(VS.95).aspx#Collections
                SetStatus("Creating collection thumbnails", 91, 99, PROGRESSPERIMAGE + 1);
                CreateCollectionThumbnails(alBitmaps, iFullPageLevel, bUseJpeg, sDeepZoomPath);

                // Write metadata xml files. I simply imitated the lay-out of actual files created 
                // by the current (june 2008) version of DeepZoom composer. The xml file format of 
                // dzc_output.xml does *not* match the current version of the documented xsd schema, 
                // that can be downloaded from:
                // http://msdn.microsoft.com/en-us/library/cc645033(VS.95).aspx
                SetStatus("Writing metadata", 99, 100, 1);
                CreateSceneAndMetadataXml(alBitmaps, fMaxWidth, fTotalHeight, bUseJpeg, iFullPageLevel, sDeepZoomPath);
                SetProgress(m_iProgressSteps);
                alBitmaps.Clear();
                bSuccess = true;
            }
            if (bmPages != null)
                bmPages.Dispose();
            SetStatus(null, 0, 0, 0);
            return bSuccess;
        }

        /// <summary>
        ///     This event is raised by a page bitmap when generating tiles.
        /// </summary>
        /// <param name="iProgressPercentage">progress percentage for this page</param>
        private void pbm_OnPageBitmapProgress(int iProgressPercentage)
        {
            SetProgress((PROGRESSPERIMAGE*iProgressPercentage)/100);
        }

        /// <summary>
        ///     Creates the collection thumbnails after individual deepzoom images for all pages
        ///     have been completed.
        /// </summary>
        /// <param name="alBitmaps">Array of page bitmaps</param>
        /// <param name="iFullPageLevel">Highest zoom level that allows each apge to be viewed in a single tile</param>
        /// <param name="bUseJpeg">true if color (jpg) tiles have been created, false for grayscale (png) tiles</param>
        /// <param name="sDeepZoomPath">Deepzoom output path</param>
        private void CreateCollectionThumbnails(ArrayList alBitmaps, int iFullPageLevel, bool bUseJpeg,
            string sDeepZoomPath)
        {
            try
            {
                var sRootPath = Path.Combine(sDeepZoomPath, "dzc_output_files");
                int iMortonWidth, iMortonHeight;

                GetMortonDimensions(alBitmaps.Count, out iMortonWidth, out iMortonHeight);

                using (var bmTile = new Bitmap(TILESIZE, TILESIZE, PixelFormat))
                using (var gfx = Graphics.FromImage(bmTile))
                {
                    SetProgress(1);
                    var iProgress = PROGRESSPERIMAGE;
                    var iThumbsPerTile = 1;
                    for (var iLevel = iFullPageLevel; iLevel >= 0; iLevel--)
                    {
                        var sOutputPath = Path.Combine(sRootPath, iLevel.ToString());
                        if (RecreatePath(sOutputPath))
                        {
                            for (int iTileX = 0, iFirstThumbX = 0;
                                iTileX < iMortonWidth;
                                iTileX++, iFirstThumbX += iThumbsPerTile)
                            {
                                for (int iTileY = 0, iFirstThumbY = 0;
                                    iTileY < iMortonHeight;
                                    iTileY++, iFirstThumbY += iThumbsPerTile)
                                {
                                    gfx.FillRectangle(Brushes.Black, 0, 0, bmTile.Width, bmTile.Height);
                                    for (var iThumbX = 0; iThumbX < iThumbsPerTile; iThumbX++)
                                        for (var iThumbY = 0; iThumbY < iThumbsPerTile; iThumbY++)
                                        {
                                            var iPbm = GetMortonIndex(iThumbX + iFirstThumbX, iThumbY + iFirstThumbY);
                                            if (iPbm < alBitmaps.Count)
                                            {
                                                var pbm = (PageBitmap) alBitmaps[iPbm];
                                                var sThumbnailPath = pbm.GetTilePath(iLevel, 0, 0);
                                                if (File.Exists(sThumbnailPath))
                                                {
                                                    using (var bmThumb = new Bitmap(sThumbnailPath))
                                                    {
                                                        var iX = (iThumbX*TILESIZE)/iThumbsPerTile;
                                                        var iY = (iThumbY*TILESIZE)/iThumbsPerTile;
                                                        if ((iX + bmThumb.Width <= TILESIZE) &&
                                                            (iY + bmThumb.Height <= TILESIZE))
                                                            gfx.DrawImage(bmThumb, iX, iY);
                                                    }
                                                }
                                            }
                                        }
                                    var sOutputFile = iTileX + "_" + iTileY + (bUseJpeg ? ".jpg" : ".png");
                                    SaveTile(bmTile, Path.Combine(sOutputPath, sOutputFile), JpegQuality, bUseJpeg);
                                }
                            }
                        }
                        iMortonWidth = Math.Max((iMortonWidth + 1) >> 1, 1);
                        iMortonHeight = Math.Max((iMortonHeight + 1) >> 1, 1);
                        iThumbsPerTile <<= 1;
                        iProgress = iProgress/2;
                        SetProgress(1 + (PROGRESSPERIMAGE - iProgress));
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.Write(ex);
            }
        }

        /// <summary>
        ///     Creates the required xml files in the root of the deepzoom collection:
        ///     SparseImageSceneGraph.xml, Metadata.xml and dzc_output.xml.
        /// </summary>
        /// <param name="alBitmaps">Array of page bitmaps</param>
        /// <param name="fMaxWidth">Width of entire scene in physical coordinates (inches)</param>
        /// <param name="fTotalHeight">Height of entire scene in physical coordinates (inches)</param>
        /// <param name="bUseJpeg">true if color tiles (jpg) were used</param>
        /// <param name="iMaxCollectionLevel">Maximum level for collection thumbnails</param>
        /// <param name="sDeepZoomPath">DeepZoom collection output directory</param>
        private void CreateSceneAndMetadataXml(ArrayList alBitmaps, float fMaxWidth, float fTotalHeight, bool bUseJpeg,
            int iMaxCollectionLevel, string sDeepZoomPath)
        {
            // Scene and Metadata XML contain mostly identical data:
            // - Coordinates of scene are normalized to total width = 1.0 and total height = 1.0 for
            //   complete scene. Origin is in upper left corner, lower right corner is (1,1)
            // - Aspect ratio: physical width of scene / physical height of scene
            // - ZOrder = (index of image + 1) by default
            //
            // Note: as filename is used as unique reference in scene and metadata xml files, 
            // but not in the dzc_output_images directory; not surprisingly the viewer fully ignores 
            // the scene data as specified in those two xml files!
            // Instead of that, the similar (but not identical!) data in dzc_output.xml is used, which
            // incidentally contains some properties that are absent from the published xsd schema.
            //
            // dzc_output.xml uses Viewport (width, X, Y) in combination with width and height in
            // pixels to build the scene. The algebra behind it seems very peculiar (and Deepzoom
            // Composer beta 2 gets it wrong...). For images with square pixels, the following seems
            // to lead to correct results in the viewer:
            //   viewport width = (physical width of scene) / (physical width of image)
            //   viewport x = -(physical left of image) / (physical width of image)
            //   viewport y = -(physical top of image) / (physical width of image)
            //
            // Deepzoom Composer somewhere confuses pixel dimensions with physical dimensions, as it
            // incorrectly positions images with different dpi value.
            //
            // Actually, I would have preferred if the SparseImageSceneGraph.xml would really
            // have been used to position collection images, as its coordinate system is more
            // standard Silverlight and thus easier to understand.
            //
            var sSceneXml = Path.Combine(sDeepZoomPath, "SparseImageSceneGraph.xml");
            var sMetadataXml = Path.Combine(sDeepZoomPath, "Metadata.xml");
            var sDzcOutputXml = Path.Combine(sDeepZoomPath, "dzc_output.xml");
            using (var swScene = new StreamWriter(sSceneXml, false))
            using (var swMeta = new StreamWriter(sMetadataXml, false))
            using (var swDzc = new StreamWriter(sDzcOutputXml, false))
            {
                // Write header info for scene and metadata xml files.
                var fAspectRatio = fMaxWidth/fTotalHeight;
                var sLine = "<?xml version=\"1.0\" ?>\r\n<SceneGraph version=\"1\">\r\n<AspectRatio>" +
                            fAspectRatio.ToString(CultureInfo.InvariantCulture) + "</AspectRatio>";
                swScene.WriteLine(sLine);
                swMeta.WriteLine(sLine.Replace("<SceneGraph ", "<Metadata "));

                // Write header info for dzc output xml file.
                swDzc.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                swDzc.WriteLine("<Collection MaxLevel=\"" + iMaxCollectionLevel +
                                "\" TileSize=\"" + TILESIZE + "\" Format=\"" + (bUseJpeg ? "jpg" : "png") +
                                "\" Quality=\"" + (JpegQuality/100.0).ToString(CultureInfo.InvariantCulture) +
                                "\" NextItemId=\"" + alBitmaps.Count +
                                "\" xmlns=\"http://schemas.microsoft.com/deepzoom/2008\">\r\n<Items>");

                // write data for each page
                for (var iPbm = 0; iPbm < alBitmaps.Count; iPbm++)
                {
                    var pbm = (PageBitmap) alBitmaps[iPbm];

                    // Write file identification for scene and metadata xml files.
                    // Filename is actually pretty meaningless, as the souce files are not present in the
                    // DeepZoom site and the page xml data does not contain a file name reference. 
                    // (the unique ID from the collection would have been more sensible)
                    sLine = "<SceneNode>\r\n<FileName>" + pbm.FileRef + "</FileName>";
                    swScene.WriteLine(sLine);
                    swMeta.WriteLine(sLine.Replace("SceneNode>", "Image>"));

                    // Write file identification for dzc output xml file.
                    swDzc.WriteLine("<I Id=\"" + iPbm + "\" N=\"" + iPbm +
                                    "\" IsPath=\"1\" Source=\"dzc_output_images/" + pbm.ShortName + ".xml\">");

                    // Calculate postion of image in physical coordinates
                    pbm.PhysLeft = (fMaxWidth - pbm.PhysWidth)/2; // center bitmap horizontally
                    // pbm.PhysTop has already been set when images were added, so that total height
                    // could be calculated.

                    // Calculate position of image within scene in normalized coordinates
                    var fX = pbm.PhysLeft/fMaxWidth;
                    var fY = pbm.PhysTop/fTotalHeight;
                    var fWidth = pbm.PhysWidth/fMaxWidth;
                    var fHeight = pbm.PhysHeight/fTotalHeight;

                    // Write normalized image coordinates to scene and metadata xml files.
                    sLine = "<x>" + fX.ToString(CultureInfo.InvariantCulture) +
                            "</x>\r\n<y>" + fY.ToString(CultureInfo.InvariantCulture) +
                            "</y>\r\n<Width>" + fWidth.ToString(CultureInfo.InvariantCulture) +
                            "</Width>\r\n<Height>" + fHeight.ToString(CultureInfo.InvariantCulture) +
                            "</Height>\r\n<ZOrder>" + (iPbm + 1) + "</ZOrder>";
                    swScene.WriteLine(sLine);
                    swMeta.WriteLine(sLine);

                    // Calculate viewport coordinates for dzc output
                    var fViewportWidth = fMaxWidth/pbm.PhysWidth;
                    var fViewportX = -pbm.PhysLeft/pbm.PhysWidth;
                    var fViewportY = -pbm.PhysTop/pbm.PhysWidth;

                    // Write pixel size and viewport coordinates to dzc output xml file.
                    swDzc.WriteLine("<Size Width=\"" + pbm.bm.Width +
                                    "\" Height=\"" + pbm.bm.Height + "\" />");
                    swDzc.WriteLine("<Viewport Width=\"" +
                                    fViewportWidth.ToString(CultureInfo.InvariantCulture) +
                                    "\" X=\"" + fViewportX.ToString(CultureInfo.InvariantCulture) +
                                    "\" Y=\"" + fViewportY.ToString(CultureInfo.InvariantCulture) +
                                    "\" />");

                    // Write image closing tags
                    swScene.WriteLine("</SceneNode>");
                    swMeta.WriteLine("<Tag />\r\n</Image>");
                    swDzc.WriteLine("</I>");
                }

                // Write file closing tags and close files
                swScene.WriteLine("</SceneGraph>");
                swMeta.WriteLine("</Metadata>");
                swDzc.WriteLine("</Items>\r\n</Collection>");
                swScene.Close();
                swMeta.Close();
                swDzc.Close();
            }
        }

        /// <summary>
        ///     Helper function used to generate progress events.
        /// </summary>
        /// <param name="sStatus">Status text to be shown</param>
        /// <param name="iStartPercentage">Start percentage in overall progress for sub process that is about to start</param>
        /// <param name="iEndPercentage">End percentage in overall progress for sub process</param>
        /// <param name="iSteps">Number of steps that will be counted in sub process</param>
        private void SetStatus(string sStatus, int iStartPercentage, int iEndPercentage, int iSteps)
        {
            m_sProgressStatus = sStatus;
            m_iProgressStartPercentage = iStartPercentage;
            m_iProgressEndPercentage = iEndPercentage;
            m_iProgressSteps = iSteps;
            m_iProgressPercentage = -2;
            SetProgress(0);
        }

        /// <summary>
        ///     Helper function used to generate progress events.
        /// </summary>
        /// <param name="iCurrentStep">Step in current sub process</param>
        private void SetProgress(int iCurrentStep)
        {
            var iPercentage = -1;
            if (m_iProgressSteps > 0)
            {
                iPercentage = m_iProgressStartPercentage +
                              ((m_iProgressEndPercentage - m_iProgressStartPercentage)*iCurrentStep)/m_iProgressSteps;
                if (iPercentage < 0)
                    iPercentage = 0;
                if (iPercentage > 100)
                    iPercentage = 100;
            }
            if (iPercentage != m_iProgressPercentage)
            {
                m_iProgressPercentage = iPercentage;
                if (OnGenerateDeepZoomProgress != null)
                    OnGenerateDeepZoomProgress(m_sProgressStatus, iPercentage);
            }
        }

        /// <summary>
        ///     Creates a tile set for the specified bitmap and level. The caller should calculate
        ///     and pass on the zoom width and height for for the level.
        /// </summary>
        /// <param name="bm">Original bitmap</param>
        /// <param name="sOutputPath">Root output path for all tile sets (a subdirectory will be created for the level)</param>
        /// <param name="iLevel">Level</param>
        /// <param name="iWidth">overall width of the image to be used for specified zoom level</param>
        /// <param name="iHeight">overall height of the image to be used for specified zoom level</param>
        /// <param name="bUseJpeg">true if color image (should generate jpg)</param>
        /// <param name="bUseOverlap">
        ///     true to generate overlapped tiles (deepzoom images), false for fixed 256x256 tiles
        ///     (collection thumbnails)
        /// </param>
        /// <returns>Count of generated tiles</returns>
        internal static int CreateTiles(Bitmap bm, string sOutputPath, int iLevel, int iWidth, int iHeight,
            bool bUseJpeg, bool bUseOverlap)
        {
            var iTiles = 0;

            if (iWidth < 1) iWidth = 1;
            if (iHeight < 1) iHeight = 1;
            sOutputPath = Path.Combine(sOutputPath, iLevel.ToString());
            if (RecreatePath(sOutputPath))
            {
                var bSmoothScaling = bUseOverlap && ((iWidth < bm.Width) || (iHeight < bm.Height));
                using (var bmScaled = new EditableBitmap(bm, PixelFormat, iWidth, iHeight, bSmoothScaling))
                {
                    for (int x = 0, iX = 0; x < iWidth; x += TILESIZE, iX++)
                    {
                        int iLeft;
                        var iTileWidth = GetTileSize(x, iWidth, out iLeft, bUseOverlap);
                        for (int y = 0, iY = 0; y < iHeight; y += TILESIZE, iY++)
                        {
                            int iTop;
                            var iTileHeight = GetTileSize(y, iHeight, out iTop, bUseOverlap);
                            var rectTile = new Rectangle(iLeft, iTop, iTileWidth, iTileHeight);
                            var sOutputFile = iX + "_" + iY + (bUseJpeg ? ".jpg" : ".png");
                            using (var bmTile = bmScaled.CreateView(rectTile))
                            {
                                if (!bUseOverlap && ((iTileWidth < TILESIZE) || (iTileHeight < TILESIZE)))
                                {
                                    // Collection thumbnail tiles are always 256x256, even if the image content
                                    // is much smaller. Draw a smaller image on top of a black 256x256 image.
                                    using (var bmExtended = new Bitmap(TILESIZE, TILESIZE, bmTile.Bitmap.PixelFormat))
                                    {
                                        using (var gfx = Graphics.FromImage(bmExtended))
                                        {
                                            gfx.FillRectangle(Brushes.Black, 0, 0, TILESIZE, TILESIZE);
                                            gfx.DrawImage(bmTile.Bitmap, 0, 0);
                                        }
                                        SaveTile(bmExtended, Path.Combine(sOutputPath, sOutputFile), JpegQuality,
                                            bUseJpeg);
                                    }
                                }
                                else
                                    SaveTile(bmTile.Bitmap, Path.Combine(sOutputPath, sOutputFile), JpegQuality,
                                        bUseJpeg);
                            }
                            iTiles++;
                        }
                    }
                }
            }
            return iTiles;
        }

        /// <summary>
        ///     Deletes and recreates the specified directory (as a quick way to empty it)
        /// </summary>
        /// <param name="sOutputPath">Directory to be recreated</param>
        /// <returns>true on success</returns>
        internal static bool RecreatePath(string sOutputPath)
        {
            try
            {
                if (Directory.Exists(sOutputPath))
                    Directory.Delete(sOutputPath, true);
                Directory.CreateDirectory(sOutputPath);
            }
            catch
            {
            }
            return Directory.Exists(sOutputPath);
        }

        /// <summary>
        ///     Returns the maximum tile level for the given image dimensions.
        /// </summary>
        /// <param name="iWidth">Image width in pixels</param>
        /// <param name="iHeight">Image height in pixels</param>
        /// <returns>Maximum DeepZoom tile level for the image</returns>
        internal static int CalcMaxLevel(int iWidth, int iHeight)
        {
            var iDimension = Math.Max(iWidth, iHeight);
            return Convert.ToInt32(Math.Ceiling(Math.Log(iDimension)/Math.Log(2)));
        }

        /// <summary>
        ///     Helper function to get tile coordinates. DeepZoom uses tiles that have a net
        ///     size of 256 x 256, but have an overlap of 1 pixel on all sides. Tiles at the
        ///     border of the image are slightly smaller (e.g., 257 x 258) than tiles in the
        ///     middle (258 x 258).
        ///     Collection thumbnails do not use overlap but have tiles that are always exactly
        ///     256 x 256. For the non-overlap case GetTileSize will still truncate to the
        ///     image border, it returns the exact rectangle of the source image to be copied.
        /// </summary>
        /// <param name="iStart">Net start coordinate</param>
        /// <param name="iMax">Maximum coordinate in image</param>
        /// <param name="iActualStart">OUT: Actual start coordinate can be 1 pixel below iStart</param>
        /// <param name="bUseOverlap">true to use overlap</param>
        /// <returns>Actual tile size</returns>
        internal static int GetTileSize(int iStart, int iMax, out int iActualStart, bool bUseOverlap)
        {
            int iTileSize;
            if (bUseOverlap)
            {
                iTileSize = TILESIZE + TILEOVERLAP;
                iActualStart = iStart;
                if (iStart > 0)
                {
                    iTileSize += TILEOVERLAP;
                    iActualStart -= TILEOVERLAP;
                }
            }
            else
            {
                iActualStart = iStart;
                iTileSize = TILESIZE;
            }
            if (iActualStart + iTileSize > iMax)
                iTileSize = (iMax - iActualStart);
            if (iTileSize < 1)
                iTileSize = 1;
            return iTileSize;
        }

        /// <summary>
        ///     Returns Morton coordinates for the specified page bitmap index.
        ///     For explanation of the use of Morton coordinates to determine thumbnail
        ///     locations in a DeepZoom collection see
        ///     http://msdn.microsoft.com/en-us/library/cc645077(VS.95).aspx#Collections
        ///     The pattern used is also known as a Z-order curve, see
        ///     http://en.wikipedia.org/wiki/Z-order_%28curve%29
        /// </summary>
        /// <param name="iPageIndex">zero-based page bitmap index</param>
        /// <param name="iMortonX">OUT: zero-based column index</param>
        /// <param name="iMortonY">OUT: zero-based row index</param>
        private static void GetMortonXY(int iPageIndex, out int iMortonX, out int iMortonY)
        {
            iMortonX = 0;
            iMortonY = 0;
            var iBit = 1;
            while (iPageIndex != 0)
            {
                if ((iPageIndex & 1) != 0) // if lowest odd bit is set
                    iMortonX |= iBit;
                if ((iPageIndex & 2) != 0) // if lowest even bit is set
                    iMortonY |= iBit;
                iPageIndex >>= 2; // right shift 2 bits (divide by 4)
                iBit <<= 1; // left shift 1 bit (multiply by 2)
            }
        }

        /// <summary>
        ///     GetMortonIndex does the reverse transformation of GetMortonXY.
        /// </summary>
        /// <param name="iMortonX">zero-based column index</param>
        /// <param name="iMortonY">zero-based row index</param>
        /// <returns>zero-based page bitmap index</returns>
        private static int GetMortonIndex(int iMortonX, int iMortonY)
        {
            var iPageIndex = 0;

            var iBit = 1;
            while ((iMortonX != 0) || (iMortonY != 0))
            {
                if ((iMortonX & 1) != 0)
                    iPageIndex |= iBit; // Set an odd bit in the Index if the lowest X bit is set
                iMortonX >>= 1;
                iBit <<= 1;
                if ((iMortonY & 1) != 0)
                    iPageIndex |= iBit; // Set an even bit in the Index if the lowest Y bit is set
                iMortonY >>= 1;
                iBit <<= 1;
            }
            return iPageIndex;
        }

        /// <summary>
        ///     Returns number of Morton rows and Morton columns for the given page count.
        /// </summary>
        /// <param name="iPageCount">Page count</param>
        /// <param name="iMortonWidth">OUT: count of Morton columns (highest MortonX + 1)</param>
        /// <param name="iMortonHeight">OUT: count of Morton rows (highest MortonY + 1)</param>
        private static void GetMortonDimensions(int iPageCount, out int iMortonWidth, out int iMortonHeight)
        {
            // To find the bounding box of the Morton coordinates of a given Morton number and all numbers below 
            // that, consider the set of numbers that are exactly the square of a power of 2:
            //  1, 4, 16, 64, 256, 1024, ...
            //
            // For squares of powers of 2, the Morton numbers always form a filled square:
            //
            //  0   1   4   5
            //  2   3   6   7
            //  8   9  12  13
            // 10  11  14  15
            //
            // The vertical dimension and horizontal dimension are both sqrt(16) = 4 for n = 16.
            // If we continue the range, the dimension will first grow along the x-axis
            // and then along the Y-axis. The actual dimension for any in-between number
            // will be hard to predict, but for n=64, the box will be square again:
            //
            //  0   1   4   5  16  17  20  21  40  41
            //  2   3   6   7  ..                  ..
            //  8   9  12  13                      ..
            // 10  11  14  15                      ..
            // 32                                  ..
            // 34                                  ..
            // ..                                  ..
            // ..  ..  ..  ..  ..  ..  ..  ..  ..  63
            //
            // So any number < 64 cannot have a dimension > 8 and we can use that as a
            // starting condition.

            var iStartDimension = 1;
            while ((iStartDimension*iStartDimension) <= iPageCount)
                iStartDimension <<= 1;

            // Subtract 1 because we use zero-based column and row indexes
            var iMaxX = iStartDimension - 1;
            var iMaxY = iMaxX;

            // To find actual dimensions, we use the assumption that dimension of rectangle
            // containing used Morton numbers will always grow first along the x-axis (row 0), 
            // then along the y-axis (column 0) and only after that the "inside" of the 
            // rectangle will be filled. The wikipedia article
            //   http://en.wikipedia.org/wiki/Z-order_%28curve%29
            // illustrates this graphically.
            //
            // So it is enough to iterate along (x,0) and (y,0) until we find an index that is
            // not out of bounds.
            //
            for (var x = iMaxX; x >= 0; x--)
                if (GetMortonIndex(x, 0) < iPageCount) // If index at (x,0) is inside (0 .. iPageCount-1)
                {
                    iMaxX = x;
                    break;
                }

            for (var y = iMaxY; y >= 0; y--)
                if (GetMortonIndex(0, y) < iPageCount) // If index at (0,y) is inside (0 .. iPageCount-1)
                {
                    iMaxY = y;
                    break;
                }

            iMortonWidth = iMaxX + 1;
            iMortonHeight = iMaxY + 1;
        }

        /// <summary>
        ///     SaveTile is used to save a tile bitmap, either as jpg (bUseJpeg == true) or
        ///     as png (bUseJpeg == false).
        /// </summary>
        /// <param name="bm">Bitmap to be saved</param>
        /// <param name="sPath">Full path to output file</param>
        /// <param name="lQuality">Quality (only used for jpg)</param>
        /// <param name="bUseJpeg">true: save jpg format; false: save png format</param>
        internal static void SaveTile(Bitmap bm, string sPath, long lQuality, bool bUseJpeg)
        {
            if (bUseJpeg)
            {
                // Encoder parameter for image quality
                var qualityParam =
                    new EncoderParameter(Encoder.Quality, lQuality);

                // Jpeg image codec
                if (m_JpegCodec == null)
                    m_JpegCodec = getEncoderInfo("image/jpeg");

                if (m_JpegCodec == null)
                    return;

                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = qualityParam;

                bm.Save(sPath, m_JpegCodec, encoderParams);
            }
            else
                bm.Save(sPath, ImageFormat.Png);
        }

        /// <summary>
        ///     Helper function that is used to locate the jpeg codec used in GDI+.
        /// </summary>
        /// <param name="sMimeType">Mime type for which codec must be located</param>
        /// <returns></returns>
        private static ImageCodecInfo getEncoderInfo(string sMimeType)
        {
            // Get image codecs for all image formats
            var codecs = ImageCodecInfo.GetImageEncoders();

            // Find the correct image codec
            for (var i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == sMimeType)
                    return codecs[i];
            return null;
        }
    }

    /// <summary>
    ///     The PageBitmap class encapsulates bitmap and scene data
    ///     for one page bitmap.
    /// </summary>
    internal class PageBitmap
    {
        public delegate void PageBitmapProgress(int iProgressPercentage);

        private readonly Bitmap m_bm;
        private readonly float m_fPhysHeight;
        private readonly float m_fPhysWidth;
        private readonly string m_sFileExt = ".tif";
        private readonly string m_sImagePath;
        private readonly string m_sShortName = string.Empty;
        private bool m_bUseJpeg;
        private string m_sDeepZoomPath = string.Empty;

        public PageBitmap(string sImagePath)
            : this(sImagePath, null, 0, 0)
        {
        }

        public PageBitmap(string sImagePath, Bitmap bmPage, int iPage, int iPageCount)
        {
            FullPageLevel = 0;
            MaxLevel = 0;
            PhysTop = 0.0F;
            PhysLeft = 0.0F;
            if (bmPage == null)
            {
                // If no bitmap reference is passed, the path should contain the full path of a single
                // image file
                m_sImagePath = sImagePath;
                m_bm = new Bitmap(sImagePath, true);
            }
            else
            {
                if (iPageCount > 1)
                {
                    // Clone the specified page of the source image
                    bmPage.SelectActiveFrame(FrameDimension.Page, iPage);
                    m_bm = new Bitmap(bmPage);
                    m_bm.SetResolution(bmPage.HorizontalResolution, bmPage.VerticalResolution);
                }
                else
                {
                    // since we have only one page, just reference the bitmap (faster)
                    m_bm = bmPage;
                }
                m_sImagePath = Path.Combine(Path.GetDirectoryName(sImagePath),
                    Path.GetFileNameWithoutExtension(sImagePath) + "." + iPage.ToString("000"));
                m_sFileExt = Path.GetExtension(sImagePath);
            }
            try
            {
                m_fPhysWidth = m_bm.Width/m_bm.HorizontalResolution;
                m_fPhysHeight = m_bm.Height/m_bm.VerticalResolution;
            }
            catch
            {
                if (m_bm.Width > m_bm.Height)
                {
                    m_fPhysHeight = 8.5F;
                    m_fPhysWidth = m_fPhysHeight*m_bm.Width/m_bm.Height;
                }
                else
                {
                    m_fPhysWidth = 8.5F;
                    m_fPhysHeight = m_fPhysWidth*m_bm.Height/m_bm.Width;
                }
            }
            iPage = -1;
            var sExt = Path.GetExtension(m_sImagePath);
            if ((sExt.Length == 4) && int.TryParse(sExt.Substring(1), out iPage))
                m_sShortName = Path.GetFileName(m_sImagePath); // decos scan has page number as extension
            else
                m_sShortName = Path.GetFileNameWithoutExtension(m_sImagePath); // strip .tif, .jpg, etc from short name
        }

        /// <summary>
        ///     bm is the bitmap object contained in this PageBitmap
        /// </summary>
        public Bitmap bm
        {
            get { return m_bm; }
        }

        /// <summary>
        ///     physical width of image in inches
        /// </summary>
        public float PhysWidth
        {
            get { return m_fPhysWidth; }
        }

        /// <summary>
        ///     physical height of image in inches
        /// </summary>
        public float PhysHeight
        {
            get { return m_fPhysHeight; }
        }

        /// <summary>
        ///     Left coordinate for this image in scene in inches
        /// </summary>
        public float PhysLeft { get; set; }

        /// <summary>
        ///     Top coordinate for this image in scene in inches
        /// </summary>
        public float PhysTop { get; set; }

        /// <summary>
        ///     Max deepzoom tiles level for this image, depends on largest pixel dimension
        /// </summary>
        public int MaxLevel { get; private set; }

        /// <summary>
        ///     Highest level that contains the full page in one tile, used in
        ///     collection processing. Valid after CreateDeepZoomImage has been called.
        /// </summary>
        public int FullPageLevel { get; private set; }

        /// <summary>
        ///     Short name of image used in various places. Assumed to be unique in the collection.
        /// </summary>
        public string ShortName
        {
            get { return m_sShortName; }
        }

        /// <summary>
        ///     Pseudo "full path to input image", assembled from m_sImagePath and short name.
        ///     Actually, a multipage tiff can contain many images in one file, but deepzoom
        ///     incorrectly assumes one  unique filename per image.
        /// </summary>
        public string FileRef
        {
            get { return Path.Combine(Path.GetDirectoryName(m_sImagePath), ShortName + ".tif"); }
        }

        /// <summary>
        ///     Returns the ImageFormat of the image in this page bitmap, if it could be
        ///     determined from the original image file extension.
        /// </summary>
        public ImageFormat ImageFormat
        {
            get
            {
                switch (m_sFileExt.ToLower())
                {
                    case ".jpg":
                    case ".jpeg":
                        return ImageFormat.Jpeg;
                    case ".gif":
                        return ImageFormat.Gif;
                    case ".png":
                        return ImageFormat.Png;
                    case ".tif":
                    case ".tiff":
                        return ImageFormat.Tiff;
                    case ".bmp":
                        return ImageFormat.Bmp;
                }
                return m_bm.RawFormat; // don't know image format
            }
        }

        /// <summary>
        ///     Full path of root folder containing generated deepzoom images.
        ///     Valid after CreateDeepZoomImage has been called.
        /// </summary>
        public string OutputImageRootPath
        {
            get { return Path.Combine(m_sDeepZoomPath, "dzc_output_images"); }
        }

        /// <summary>
        ///     Full path of subdirectory of OutputImageRootPath that contains the
        ///     deepzoom tiles for this page bitmap.
        ///     Valid after CreateDeepZoomImage has been called.
        /// </summary>
        public string OutputImageFilesPath
        {
            get { return Path.Combine(OutputImageRootPath, ShortName + "_files"); }
        }

        public event PageBitmapProgress OnPageBitmapProgress;

        ~PageBitmap()
        {
            if (m_bm != null)
                m_bm.Dispose();
        }

        /// <summary>
        ///     Creates the DeepZoom image tile set for one page.
        /// </summary>
        /// <param name="sDeepZoomPath">Root directory of the DeepZoom collection</param>
        /// <param name="bUseJpeg">true if color images (jpg) should be written</param>
        public void CreateDeepZoomImage(string sDeepZoomPath, bool bUseJpeg)
        {
            m_sDeepZoomPath = sDeepZoomPath;
            m_bUseJpeg = bUseJpeg;
            MaxLevel = GenerateDeepZoom.CalcMaxLevel(bm.Width, bm.Height);
            FullPageLevel = -1;
            var iWidth = bm.Width;
            var iHeight = bm.Height;
            var sImagePath = OutputImageFilesPath;
            var iProgress = 100;
            for (var iLevel = MaxLevel; iLevel >= 0; iLevel--)
            {
                var iTiles = GenerateDeepZoom.CreateTiles(bm, sImagePath, iLevel, iWidth, iHeight, bUseJpeg, true);
                if ((iTiles == 1) && (FullPageLevel < 0))
                    FullPageLevel = iLevel; // keep highest level that has full page tile
                iWidth = iWidth/2;
                iHeight = iHeight/2;
                iProgress = iProgress/2;
                if (OnPageBitmapProgress != null)
                    OnPageBitmapProgress(100 - iProgress);
            }
            WriteImageXml(OutputImageRootPath, bUseJpeg);
            OnPageBitmapProgress(100);
        }

        /// <summary>
        ///     Writes the xml file for a DeepZoom image set that should be present in the
        ///     dzc_output_images directory.
        /// </summary>
        /// <param name="sImageRootPath">Image root path</param>
        /// <param name="bUseJpeg">true if color images (jpg) have been written</param>
        private void WriteImageXml(string sImageRootPath, bool bUseJpeg)
        {
            using (var sw = new StreamWriter(Path.Combine(sImageRootPath, ShortName + ".xml"), false))
            {
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sw.WriteLine("<Image TileSize=\"" + GenerateDeepZoom.TILESIZE +
                             "\" Overlap=\"" + GenerateDeepZoom.TILEOVERLAP +
                             "\" Format=\"" + (bUseJpeg ? "jpg" : "png") +
                             "\" xmlns=\"http://schemas.microsoft.com/deepzoom/2008\">");
                sw.WriteLine("<Size Width=\"" + bm.Width +
                             "\" Height=\"" + bm.Height + "\"/>");
                sw.WriteLine("</Image>");
                sw.Close();
            }
        }

        /// <summary>
        ///     Returns the full path for the specified tile image.
        ///     Valid after CreateDeepZoomImage has been called.
        /// </summary>
        /// <param name="iLevel">deepzoom level</param>
        /// <param name="iX">tile x index</param>
        /// <param name="iY">tile y index</param>
        /// <returns></returns>
        public string GetTilePath(int iLevel, int iX, int iY)
        {
            var sPath = Path.Combine(OutputImageFilesPath, iLevel.ToString());
            var sFileName = iX + "_" + iY + (m_bUseJpeg ? ".jpg" : ".png");
            return Path.Combine(sPath, sFileName);
        }
    }
}
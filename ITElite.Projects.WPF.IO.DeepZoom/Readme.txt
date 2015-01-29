The Code is reference from the URL:
http://jimlynn.wordpress.com/tag/wpf/

A SIMPLE LIBRARY TO BUILD A DEEP ZOOM IMAGE
http://jimlynn.wordpress.com/2008/imple-library-to-build-a-deep-zoom-image/11/12/a-s
PROGRAMMATICALLY CREATE DEEP ZOOM COLLECTIONS
http://jimlynn.wordpress.com/2008/11/28/programmatically-create-deep-zoom-collections/


second way to generate the DeepZoom files
http://www.codeproject.com/Articles/27359/Generate-Silverlight-DeepZoom-Image-Collection-f
DeepZoomGenerator
 1  // This code has been changed by J�rg Lang (lang.joerg@gmail.com) from a class 
  2  // called Decos.DeepZoom that was made 2008 by Berend Engelbrecht, b.engelbrecht@gmail.com
  3  // The original code can be found at www.codeproject.com
  4  //
  5  // The changes made are mainly taking out all stuff that seemed to be product
  6  // specific to a product by the original creator of the code and that was not
  7  // needed to create tiles for a single image.
  8  // The main change is of course, that the images get stored in a database.
  9  //
 10  // This source code is licensed for commercial and non-commercial use under the 
 11  // Code Project Open License (CPOL) 1.02  http://www.codeproject.com/info/cpol10.aspx
 12  //  
 13  using System;
 14  using System.Drawing;
 15  using System.Drawing.Imaging;
 16  using System.IO;
 17  
 18  namespace DzComposer
 19  {
 20      /// <summary>
 21      /// DeepZoom encapsulates code used to generate a DeepZoom image from a
 22      /// single image. This class does not contain any Windows Forms related or user
 23      /// interface code. 
 24      /// </summary>
 25      public class DeepZoomGenerator
 26      {
 27          /// <summary>
 28          /// Occurs when the creation of the deep zoom image progresses.
 29          /// </summary>
 30          //public event EventHandler<DeepZoomCreationProgressEventArgs> CreationProgress;
 31  
 32          /// <summary>Overlap in pixels for DeepZoom image tiles</summary>
 33          internal const int tileOverlap = 1;
 34          internal const int maxThumbnailWidth = 125;
 35  
 36          private ImageCodecInfo jpegCodec;
 37          
 38          /// <summary>
 39          /// Gets or sets the database persister.
 40          /// </summary>
 41          /// <value>The database persister.</value>
 42          public IDzPersistance Persister { get; set; }
 43  
 44          /// <summary>JPEG quality used for jpg image tiles, must be between 1 and 100</summary>
 45          public int JpegQuality { get; set; }
 46  
 47          /// <summary>PixelFormat used in memory bitmaps</summary>
 48          public PixelFormat ColorPixelFormat { get; set; }
 49  
 50          /// <summary>PixelFormat used in memory bitmaps</summary>
 51          public int TileSize { get; set; }
 52  
 53   
 54          /// <summary>
 55          /// Initializes a new instance of the <see cref="DeepZoomGenerator"/> class with 
 56          /// default values for jpeg quality (90), tile size (256) and a color format
 57          /// of 24bppRgb.
 58          /// </summary>
 59          public DeepZoomGenerator(): this(90, 256, System.Drawing.Imaging.PixelFormat.Format24bppRgb)
 60          {
 61          }
 62  
 63          /// <summary>
 64          /// Initializes a new instance of the <see cref="DeepZoomGenerator"/> class.
 65          /// </summary>
 66          /// <param name="jpegQuality">The JPEG quality. Integer values from 0 to 100</param>
 67          /// <param name="tileSize">Size of the tiles.</param>
 68          /// <param name="colorPixelFormat">The pixel format.</param>
 69          public DeepZoomGenerator(int jpegQuality, int tileSize, PixelFormat colorPixelFormat)
 70          {
 71              JpegQuality = jpegQuality;
 72              TileSize = tileSize;
 73              ColorPixelFormat = colorPixelFormat;
 74          }
 75  
 76  
 77          ///// <summary>
 78          ///// Generates a deepzoom image from a file
 79          ///// The image file can be anything
 80          ///// that System.Drawing.Bitmap allows in its constructor, usually tiff or jpg.
 81          ///// </summary>
 82          ///// <param name="sourceFile">Full path to the file</param>
 83          ///// <param name="imageName">Name of the image.</param>
 84          ///// <param name="useJpeg">if set to <c>true</c> [use JPEG].</param>
 85          ///// <param name="useOverlap">if set to <c>true</c> the tiles will be created with a one pixel overlap.</param>
 86          ///// <returns>the id of the image in the database</returns>
 87          //public int GenerateFromFile(string sourceFile, string imageName, bool useJpeg, bool useOverlap)
 88          //{
 89          //    Bitmap sourceImage;
 90          //    if (File.Exists(sourceFile))
 91          //        sourceImage = new Bitmap(sourceFile);
 92          //    else
 93          //        throw new FileNotFoundException("File not found!", sourceFile);
 94  
 95          //    // Generate full deepzoom image 
 96          //    int id = CreateSingleDeepZoomImage(imageName, sourceImage, useJpeg, useOverlap);
 97  
 98          //    sourceImage.Dispose();
 99  
100          //    return id;
101          //}
102  
103          /// <summary>
104          /// Creates the DeepZoom image tile set for one image.
105          /// </summary>
106          /// <param name="imageName">Name of the image.</param>
107          /// <param name="bitmap">The bitmap.</param>
108          /// <param name="useJpeg">true if color images (jpg) should be written</param>
109          /// <param name="useOverlap">if set to <c>true</c> tiles will be created a one pixel overlap.</param>
110          /// <returns>The id of the image in the database</returns>
111          public bool CreateSingleDeepZoomImage(string imageName, Bitmap bitmap, bool useJpeg, bool useOverlap)
112          {
113              bool ok = true;
114              int maxLevel = CalcMaxLevel(bitmap.Width, bitmap.Height);
115              int width = bitmap.Width;
116              int height = bitmap.Height;
117              double progressStep = (double) 100 / maxLevel;
118              double progress = 0;
119              int overlap = useOverlap ? tileOverlap : 0;
120  
121              // Create a thumbnail to store in the db as a preview
122              //Bitmap thumbnail = new Bitmap(bitmap, maxThumbnailWidth, bitmap.Height / (bitmap.Width / maxThumbnailWidth));
123  
124              // Persist the image info in the database
125              Persister.SaveImageInfo(imageName, width, height, TileSize, overlap, GetMimeType(useJpeg) /*, thumbnail*/);
126  
127              for (int level = maxLevel; level >= 0; level--)
128              {
129                  bool outOfMemory;
130                  CreateTiles(bitmap, imageName, level, width, height, useJpeg, useOverlap, out outOfMemory);
131                  
132                  if (ok)
133                      ok = !outOfMemory;
134                  
135                  width = (width / 2);
136                  height = (height / 2);
137                  progress += progressStep;
138  
139                  //OnDeepZoomCreationProgress(new DeepZoomCreationProgressEventArgs((int) progress));
140              }
141  
142              //return imageId;
143              return ok;
144          }
145  
146          ///// <summary>
147          ///// Raises the DeepZoomCreationProgress event.
148          ///// </summary>
149          ///// <param name="e">The <see cref="DbDzComposer.DeepZoomCreationProgressEventArgs"/> instance containing the event data.</param>
150          //private void OnDeepZoomCreationProgress(DeepZoomCreationProgressEventArgs e)
151          //{
152          //    // To prevent race conditions assign it to a variable and raise the event from there
153          //    EventHandler<DeepZoomCreationProgressEventArgs> handler = CreationProgress;
154          //    if (handler != null)
155          //    {
156          //        handler(this, e);
157          //    }
158          //}
159  
160  
161          /// <summary>
162          /// Creates a tile set for the specified bitmap and level. The caller should calculate
163          /// and pass on the zoom width and height for for the level.
164          /// </summary>
165          /// <param name="bitmap">Original bitmap</param>
166          /// <param name="imageId">The image id.</param>
167          /// <param name="level">Level</param>
168          /// <param name="width">overall width of the image to be used for specified zoom level</param>
169          /// <param name="height">overall height of the image to be used for specified zoom level</param>
170          /// <param name="useJpeg">true if color image (should generate jpg)</param>
171          /// <param name="useOverlap">true to generate overlapped tiles (deepzoom images), false for fixed 256x256 tiles (collection thumbnails)</param>
172          /// <returns>Count of generated tiles</returns>
173          internal int CreateTiles(Bitmap bitmap, string imageName, int level, int width, int height, bool useJpeg, bool useOverlap, 
174                                     out bool outOfMemory)
175          {
176              int tilesCount = 0;
177              outOfMemory = false;
178  
179              // Make sure we have valid height and width
180              if (width < 1) width = 1;
181              if (height < 1) height = 1;
182  
183              bool useSmoothScaling = useOverlap && (width < bitmap.Width || height < bitmap.Height);
184              using (var scaledBitmap = new EditableBitmap(bitmap, ColorPixelFormat, width, height, useSmoothScaling))
185              {
186                  outOfMemory = scaledBitmap.OutOfMemory;
187  
188                  for (int x = 0, iX = 0; x < width; x += TileSize, iX++)
189                  {
190                      int left;
191                      int tileWidth = GetTileSize(x, width, out left, useOverlap);
192                      for (int y = 0, iY = 0; y < height; y += TileSize, iY++)
193                      {
194                          int top;
195                          int tileHeight = GetTileSize(y, height, out top, useOverlap);
196                          var rectTile = new Rectangle(left, top, tileWidth, tileHeight);
197                          string outputFile = iX + "_" + iY + (useJpeg ? ".jpg" : ".png");
198                          using (EditableBitmap tileBitmap = scaledBitmap.CreateView(rectTile))
199                          {
200                              if (!useOverlap && (tileWidth < TileSize || tileHeight < TileSize))
201                              {
202                                  // Collection thumbnail tiles are always the tilesize in minimum, even if the image content
203                                  // is much smaller. Draw a smaller image on top of a black TileSize x TileSize image.
204                                  using (var bmExtended = new Bitmap(TileSize, TileSize, tileBitmap.Bitmap.PixelFormat))
205                                  {
206                                      using (Graphics gfx = Graphics.FromImage(bmExtended))
207                                      {
208                                          gfx.FillRectangle(Brushes.Black, 0, 0, TileSize, TileSize);
209                                          gfx.DrawImage(tileBitmap.Bitmap, 0, 0);
210                                      }
211                                      SaveTile(bmExtended, imageName, level, iX, iY, JpegQuality, useJpeg);
212                                  }
213                              }
214                              else
215                                  SaveTile(tileBitmap.Bitmap, imageName, level, iX, iY, JpegQuality, useJpeg);
216                          }
217                          tilesCount++;
218                      }
219                  }
220              }
221  
222              return tilesCount;
223          }
224  
225  
226          /// <summary>
227          /// Returns the maximum tile level for the given image dimensions.
228          /// </summary>
229          /// <param name="width">Image width in pixels</param>
230          /// <param name="height">Image height in pixels</param>
231          /// <returns>Maximum DeepZoom tile level for the image</returns>
232          internal static int CalcMaxLevel(int width, int height)
233          {
234              int iDimension = Math.Max(width, height);
235              return Convert.ToInt32(Math.Ceiling(Math.Log(iDimension) / Math.Log(2)));
236          }
237  
238          /// <summary>
239          /// Helper function to get tile coordinates. DeepZoom uses tiles that have a net
240          /// size of 256 x 256, but have an overlap of 1 pixel on all sides. Tiles at the 
241          /// border of the image are slightly smaller (e.g., 257 x 258) than tiles in the 
242          /// middle (258 x 258).
243          /// 
244          /// Collection thumbnails do not use overlap but have tiles that are always exactly
245          /// 256 x 256. For the non-overlap case Getc_tileSize will still truncate to the 
246          /// image border, it returns the exact rectangle of the source image to be copied.
247          /// </summary>
248          /// <param name="start">Net start coordinate</param>
249          /// <param name="max">Maximum coordinate in image</param>
250          /// <param name="actualStart">OUT: Actual start coordinate can be 1 pixel below iStart</param>
251          /// <param name="useOverlap">true to use overlap</param>
252          /// <returns>Actual tile size</returns>
253          internal int GetTileSize(int start, int max, out int actualStart, bool useOverlap)
254          {
255              int ic_tileSize;
256              if (useOverlap)
257              {
258                  ic_tileSize = TileSize + tileOverlap;
259                  actualStart = start;
260                  if (start > 0)
261                  {
262                      ic_tileSize += tileOverlap;
263                      actualStart -= tileOverlap;
264                  }
265              }
266              else
267              {
268                  actualStart = start;
269                  ic_tileSize = TileSize;
270              }
271              if (actualStart + ic_tileSize > max)
272                  ic_tileSize = (max - actualStart);
273              if (ic_tileSize < 1)
274                  ic_tileSize = 1;
275              return ic_tileSize;
276          }
277  
278          /// <summary>
279          /// SaveTile is used to save a tile bitmap, either as jpg (bUseJpeg == true) or
280          /// as png (bUseJpeg == false).
281          /// </summary>
282          /// <param name="bitmap">Bitmap to be saved</param>
283          /// <param name="level">The level.</param>
284          /// <param name="imageId">The image id.</param>
285          /// <param name="x">The x coordinates of the image</param>
286          /// <param name="y">The y coordinates of the image</param>
287          /// <param name="quality">Quality (only used for jpg)</param>
288          /// <param name="useJpeg">true: save jpg format; false: save png format</param>
289          internal void SaveTile(Bitmap bitmap, string imageName, int level, int x, int y, long quality, bool useJpeg)
290          {           
291              MemoryStream memStream = new MemoryStream();
292              if (useJpeg)
293              {
294                  // Encoder parameter for image quality
295                  var qualityParam = new EncoderParameter(Encoder.Quality, quality);
296  
297                  // Jpeg image codec
298                  if (jpegCodec == null)
299                      jpegCodec = GetEncoderInfo(GetMimeType(true));
300  
301                  if (jpegCodec == null)
302                      return;
303  
304                  var encoderParams = new EncoderParameters(1);
305                  encoderParams.Param[0] = qualityParam;
306  
307                  // Create a new bitmap according to the users quality settings
308                  bitmap.Save(memStream, jpegCodec, encoderParams);
309                  Bitmap bmp = new Bitmap(memStream);
310  
311                  // Save the jpge to the database
312                  Persister.SaveImageTile(imageName, level, x, y, bmp);
313              }
314              else
315              {
316                  bitmap.Save(memStream, ImageFormat.Png);
317                  Bitmap bmp = new Bitmap(memStream);
318  
319                  // Save the png to the database
320                  Persister.SaveImageTile(imageName, level, x, y, bmp);
321              }
322          }
323  
324          /// <summary>
325          /// Helper function that returns the mime type for either jpeg or png
326          /// </summary>
327          /// <param name="useJpeg">if set to <c>true</c> [use JPEG].</param>
328          /// <returns></returns>
329          private string GetMimeType(bool useJpeg)
330          {
331              return useJpeg ? "image/jpeg" : "image/png";
332          }
333  
334          /// <summary>
335          /// Helper function that is used to locate the jpeg codec used in GDI+.
336          /// </summary>
337          /// <param name="mimeType">Mime type for which codec must be located</param>
338          /// <returns></returns>
339          private static ImageCodecInfo GetEncoderInfo(string mimeType)
340          {
341              // Get image codecs for all image formats
342              ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
343  
344              // Find the correct image codec
345              for (int i = 0; i < codecs.Length; i++)
346                  if (codecs[i].MimeType == mimeType)
347                      return codecs[i];
348              return null;
349          }
350      }
351  
352      /// <summary>
353      /// Provides data for the DeepZoomCreationProgress. 
354      /// </summary>
355      public class DeepZoomCreationProgressEventArgs: EventArgs
356      {
357          private readonly int m_creationProgress;
358          public int CreationProgress
359          {
360              get { return m_creationProgress; }
361          }
362  
363          public DeepZoomCreationProgressEventArgs(int percentage)
364          {
365              if (percentage > 100)
366                  percentage = 100;
367              m_creationProgress = percentage;
368          }
369      }
370  }
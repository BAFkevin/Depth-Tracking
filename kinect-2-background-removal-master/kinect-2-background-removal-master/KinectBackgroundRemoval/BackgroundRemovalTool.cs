using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectBackgroundRemoval
{
    /// <summary>
    /// Provides extension methods for removing the background of a Kinect frame.
    /// </summary>
    public class BackgroundRemovalTool
    {
        #region Constants
        /// <summary>
        /// Map depth range to byte range
        /// </summary>
        private const int MapDepthToByte = 8000 / 256;
        /// <summary>
        /// The DPI.
        /// </summary>
        readonly double DPI = 96.0;

        /// <summary>
        /// Default format.
        /// </summary>
        readonly PixelFormat FORMAT = PixelFormats.Bgra32;

        /// <summary>
        /// Bytes per pixel.
        /// </summary>
        readonly int BYTES_PER_PIXEL = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        #endregion

        #region Members

        /// <summary>
        /// The bitmap source.
        /// </summary>
        WriteableBitmap _bitmap = null;
        WriteableBitmap ColorBitmap = null;

        /// <summary>
        /// The depth values.
        /// </summary>
        ushort[] _depthData = null;

        /// <summary>
        /// The body index values.
        /// </summary>
        byte[] _bodyData = null;

        /// <summary>
        /// The RGB pixel values.
        /// </summary>
        byte[] _colorData = null;

        /// <summary>
        /// Pixles for the depth sensing
        /// </summary>
        byte[] _displayPixels = null;
        byte[] _prevDisplayPixels = null;

        // <summary>
        /// pixels for the normal image
        /// </summary>
        byte[] ColorDisplayPixels = null;

        /// <summary>
        /// The color points used for the background removal (green-screen) effect.
        /// </summary>
        ColorSpacePoint[] _colorPoints = null;

        /// <summary>
        /// The coordinate mapper for the background removal (green-screen) effect.
        /// </summary>
        CoordinateMapper _coordinateMapper = null;
        bool FirstFound = false;
        int startPixel;
        int endPixel;
        int prevStartPixel;
        int prevEndPixel;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of BackgroundRemovalTool.
        /// </summary>
        /// <param name="mapper">The coordinate mapper used for the background removal.</param>
        public BackgroundRemovalTool(CoordinateMapper mapper)
        {
            _coordinateMapper = mapper;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Converts a depth frame to the corresponding System.Windows.Media.Imaging.BitmapSource and removes the background (green-screen effect).
        /// </summary>
        /// <param name="depthFrame">The specified depth frame.</param>
        /// <param name="colorFrame">The specified color frame.</param>
        /// <param name="bodyIndexFrame">The specified body index frame.</param>
        /// <returns>The corresponding System.Windows.Media.Imaging.BitmapSource representation of image.</returns>
        public Tuple<BitmapSource, BitmapSource> GreenScreen(ColorFrame colorFrame, DepthFrame depthFrame, BodyIndexFrame bodyIndexFrame, int Resn, int Resm, bool avg)
        {
            int colorWidth = colorFrame.FrameDescription.Width;
            int colorHeight = colorFrame.FrameDescription.Height;

            int depthWidth = depthFrame.FrameDescription.Width;
            int depthHeight = depthFrame.FrameDescription.Height;

            int bodyIndexWidth = bodyIndexFrame.FrameDescription.Width;
            int bodyIndexHeight = bodyIndexFrame.FrameDescription.Height;

            if (_displayPixels == null)
            {
                _depthData = new ushort[depthWidth * depthHeight];
                _bodyData = new byte[depthWidth * depthHeight];
                _colorData = new byte[colorWidth * colorHeight * BYTES_PER_PIXEL];
                _displayPixels = new byte[depthWidth * depthHeight * BYTES_PER_PIXEL];
                ColorDisplayPixels = new byte[colorWidth * colorHeight * BYTES_PER_PIXEL];
                _colorPoints = new ColorSpacePoint[depthWidth * depthHeight];
                _bitmap = new WriteableBitmap(depthWidth, depthHeight, DPI, DPI, FORMAT, null);
                ColorBitmap = new WriteableBitmap(depthWidth, depthHeight, DPI, DPI, FORMAT, null);
            }

            if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                colorFrame.CopyRawFrameDataToArray(ColorDisplayPixels);
            }
            else
            {
                colorFrame.CopyConvertedFrameDataToArray(ColorDisplayPixels, ColorImageFormat.Bgra);
            }
            int stride = colorWidth * FORMAT.BitsPerPixel / 8;


            if (((depthWidth * depthHeight) == _depthData.Length) && ((colorWidth * colorHeight * BYTES_PER_PIXEL) == _colorData.Length) && ((bodyIndexWidth * bodyIndexHeight) == _bodyData.Length))
            {
                depthFrame.CopyFrameDataToArray(_depthData);

                if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                {
                    colorFrame.CopyRawFrameDataToArray(_colorData);
                }
                else
                {
                    colorFrame.CopyConvertedFrameDataToArray(_colorData, ColorImageFormat.Bgra);
                }

                bodyIndexFrame.CopyFrameDataToArray(_bodyData);
                _coordinateMapper.MapDepthFrameToColorSpace(_depthData, _colorPoints);



                Array.Clear(_displayPixels, 0, _displayPixels.Length);

                for (int y = 0; y < depthHeight; ++y)
                {
                    for (int x = 0; x < depthWidth; ++x)
                    {
                        int depthIndex = (y * depthWidth) + x;

                        byte player = _bodyData[depthIndex];
                        ushort maxDepth = ushort.MaxValue;
                        ushort depth = _depthData[depthIndex];
                        ushort minDepth = depthFrame.DepthMinReliableDistance;
                        byte intensity = (byte)(255 - (depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0));
                        int displayIndex = depthIndex * BYTES_PER_PIXEL;

                        if (player == 0 || player == 1 || player == 2 || player == 3 || player == 4 || player == 5)
                        {

                            ColorSpacePoint colorPoint = _colorPoints[depthIndex];

                            int colorX = (int)Math.Floor(colorPoint.X + 0.5);
                            int colorY = (int)Math.Floor(colorPoint.Y + 0.5);

                            if ((colorX >= 0) && (colorX < colorWidth) && (colorY >= 0) && (colorY < colorHeight))
                            {
                                int colorIndex = ((colorY * colorWidth) + colorX) * BYTES_PER_PIXEL;


                                if (player == 0 || player == 3 || player == 4)
                                    _displayPixels[displayIndex + 0] = 0xff;
                                else
                                    _displayPixels[displayIndex + 0] = 0;

                                if (player == 1 || player == 3 || player == 5)
                                    _displayPixels[displayIndex + 1] = 0xff;
                                else
                                    _displayPixels[displayIndex + 1] = 0;

                                if (player == 2 || player == 4 || player == 5)
                                    _displayPixels[displayIndex + 2] = 0xff;
                                else
                                    _displayPixels[displayIndex + 2] = 0;

                                _displayPixels[displayIndex + 3] = intensity;

                                if (displayIndex > 16 && _displayPixels[displayIndex - 9] != 0 && !FirstFound)
                                {
                                    prevStartPixel = startPixel;
                                    startPixel = displayIndex + 3;
                                    //_displayPixels[displayIndex ] = 0xff;
                                    //_displayPixels[displayIndex + 1] = 0xff;
                                    //_displayPixels[displayIndex + 2] = 0xff;
                                    FirstFound = true;
                                    //if (prevStartPixel + 1092 < startPixel && prevStartPixel != 0)
                                    //{
                                    //    //moved left
                                    //}
                                }

                                //if (NotFirst && _prevDisplayPixels[displayIndex+3] == 0 && displayIndex > 51 ) 
                                //{
                                //    if (_prevDisplayPixels[displayIndex - 51] == 0 && _displayPixels[displayIndex-51] !=0)
                                //    {
                                //        //Object Moved to the Left side of the screen
                                //    }


                                //}

                            }
                        }
                        //else if (FirstFound && _displayPixels[displayIndex%2048 - 9] == 0)
                        //{
                        //    prevEndPixel = endPixel;
                        //    endPixel = displayIndex + 3;
                        //    _displayPixels[displayIndex] = 0x00;
                        //    _displayPixels[displayIndex + 1] = 0xff;
                        //    _displayPixels[displayIndex + 2] = 0x00;

                        //}

                    }
                }
                //FirstFound = false;
                if (avg)
                    this.RenderDepthPixels(Resn, Resm);
                else
                    this.RenderDepthPixelsSkip(Resn, Resm);
                //PostionAdjustment();
                _bitmap.Lock();

                Marshal.Copy(_displayPixels, 0, _bitmap.BackBuffer, _displayPixels.Length);
                _bitmap.AddDirtyRect(new Int32Rect(0, 0, depthWidth, depthHeight));

                _bitmap.Unlock();



            }




            return new Tuple<BitmapSource, BitmapSource>(_bitmap, BitmapSource.Create(colorWidth, colorHeight, 96, 96, FORMAT, null, ColorDisplayPixels, stride));
        }

        #endregion
        // private void PostionAdjustment()
        // {
        // }

        private void RenderDepthPixels(int n, int m = 0)
        {
            if (n != 512)
            {
                if (m == 0) m = n;
                int columns = 2048 / n;
                int rows = 424 / m;
                int currentrow, currentcolumn;
                int[] totals0 = new int[n * m + 1];
                int[] totals1 = new int[n * m + 1];
                int[] totals2 = new int[n * m + 1];
                int[] totals3 = new int[n * m + 1];

                int rowsector = 0;
                int columnsector = 0;
                for (int i = 0; i < _displayPixels.Length; i++)
                {

                    currentrow = i / 2048;
                    currentcolumn = i % 2048;
                    rowsector = currentrow / rows;
                    columnsector = currentcolumn / columns;
                    int sector = (rowsector * (n - 1)) + (rowsector + columnsector);
                    if (i % 4 == 0)
                        totals0[sector] += _displayPixels[i];
                    if (i % 4 == 1)
                        totals1[sector] += _displayPixels[i];
                    if (i % 4 == 2)
                        totals2[sector] += _displayPixels[i];
                    if (i % 4 == 3)
                        totals3[sector] += _displayPixels[i];

                }

                rowsector = 0;
                columnsector = 0;
                for (int i = 0; i < _displayPixels.Length; i++)
                {
                    currentrow = i / 2048;
                    currentcolumn = i % 2048;
                    rowsector = currentrow / rows;
                    columnsector = currentcolumn / columns;
                    int sector = (rowsector * (n - 1)) + (rowsector + columnsector);
                    if (i % 4 == 0)
                        _displayPixels[i] = (byte)(totals0[sector] / (rows * columns / 4));
                    if (i % 4 == 1)
                        _displayPixels[i] = (byte)(totals1[sector] / (rows * columns / 4));
                    if (i % 4 == 2)
                        _displayPixels[i] = (byte)(totals2[sector] / (rows * columns / 4));
                    if (i % 4 == 3)
                        _displayPixels[i] = (byte)(totals3[sector] / (rows * columns / 4));

                }
            }
        }

        private void RenderDepthPixelsMiddle(int n, int m = 0)
        {
            if (n != 512)
            {


                if (m == 0) m = n;
                int columns = 2048 / n;
                int rows = 424 / m;
                int currentrow, currentcolumn;
                int[] totals0 = new int[n * m + 1];
                int[] totals1 = new int[n * m + 2];
                int[] totals2 = new int[n * m + 3];
                int[] totals3 = new int[n * m + 4];

                int rowsector = 0;
                int columnsector = 0;
                for (int i = 0; i < _displayPixels.Length; i++)
                {
                    if (i % columns == 0 && i != 868348)
                    {
                        currentrow = i / 2048;
                        currentcolumn = i % 2048;
                        rowsector = currentrow / rows;
                        columnsector = currentcolumn / columns;
                        int sector = (rowsector * (n - 1)) + (rowsector + columnsector);
                        totals0[sector] = _displayPixels[i + columns / 2];
                        totals1[sector] = _displayPixels[i + columns / 2 + 1];
                        totals2[sector] = _displayPixels[i + columns / 2 + 2];
                        totals3[sector] = _displayPixels[i + columns / 2 + 3];
                    }

                }

                rowsector = 0;
                columnsector = 0;
                for (int i = 0; i < _displayPixels.Length; i++)
                {
                    currentrow = i / 2048;
                    currentcolumn = i % 2048;
                    rowsector = currentrow / rows;
                    columnsector = currentcolumn / columns;
                    int sector = (rowsector * (n - 1)) + (rowsector + columnsector);
                    if (i % 4 == 0)
                        _displayPixels[i] = (byte)(totals0[sector]);
                    if (i % 4 == 1)
                        _displayPixels[i] = (byte)(totals1[sector]);
                    if (i % 4 == 2)
                        _displayPixels[i] = (byte)(totals2[sector]);
                    if (i % 4 == 3)
                        _displayPixels[i] = (byte)(totals3[sector]);

                }

            }
        }



        private void RenderDepthPixelsSkip(int n, int m = 0)
        {
            if (n == 512) n = 4;
            if (n == 8) n = 64;
            if (n == 32) n = 16;

            for (int i = 0; i < _displayPixels.Length; i = i + 4 * n)
            {
                for (int j = 1; j < n; j++)
                {
                    _displayPixels[i + 4 * j] = _displayPixels[i];

                    _displayPixels[1 + i + 4 * j] = _displayPixels[i + 1];

                    _displayPixels[2 + i + 4 * j] = _displayPixels[i + 2];

                    _displayPixels[3 + i + 4 * j] = _displayPixels[i + 3];
                }

            }
        }



    }
}

using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KinectBackgroundRemoval
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        int ResN = 8;
        int ResM = 8;
        bool ByAvg = true;
        

        

        // 1) Create a background removal tool.
        BackgroundRemovalTool _backgroundRemovalTool;

        public MainWindow()
        {
            InitializeComponent();
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                // 2) Initialize the background removal tool.
                _backgroundRemovalTool = new BackgroundRemovalTool(_sensor.CoordinateMapper);

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
                
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }
        }
       

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            using (var colorFrame = reference.ColorFrameReference.AcquireFrame())
            using (var depthFrame = reference.DepthFrameReference.AcquireFrame())
            using (var bodyIndexFrame = reference.BodyIndexFrameReference.AcquireFrame())

                if (colorFrame != null && depthFrame != null && bodyIndexFrame != null)
                {
                    // 3) Update the image source.
                     var tuple = _backgroundRemovalTool.GreenScreen(colorFrame, depthFrame, bodyIndexFrame, ResN,ResM,ByAvg);
                    camera.Source = tuple.Item1;
                    PicCam.Source = tuple.Item2;
                }
            

            //try
            //{
            //    colorFrame.Dispose();
            //    depthFrame.Dispose();
            //    bodyIndexFrame.Dispose();
            //}
            //catch
            //{
            //}
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ResN = 32;
            ResM = 53;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ResN = 512;
            ResM = 424;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ResN = 8;
            ResM = 8;
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (ByAvg)
                ByAvg = false;
            else
                ByAvg = true;
        }
    }
}

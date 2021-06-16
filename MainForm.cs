using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Azure.Kinect.Sensor;

namespace K4A_WinForms_Visualize
{
    public partial class MainForm : Form
    {
        private Device _device;
        private Transformation _transformation;
        private int _colourWidth;
        private int _colourHeight;
        Bitmap colorBitmap;
        private BackgroundWorker ImageCaptureWorker;
        public MainForm()
        {
            InitializeComponent();
            ImageCaptureWorker = new BackgroundWorker() { WorkerSupportsCancellation = true };
            ImageCaptureWorker.DoWork += ImageCaptureWorker_DoWork;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if(ImageCaptureWorker.IsBusy)
            {
                ImageCaptureWorker.CancelAsync();
            }
        }

        private void ImageCaptureWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            while (true)
            {
                try
                {
                    using (var capture = _device.GetCapture())
                    {
                        unsafe
                        {
                            //Getting color image of kinect.
                            Microsoft.Azure.Kinect.Sensor.Image colorImage = capture.Color;
                            //Geting the pointer of color image
                            using (MemoryHandle pin = colorImage.Memory.Pin())
                            {
                                //creating bitmap image
                                colorBitmap = new Bitmap(
                                     colorImage.WidthPixels, //width of color image
                                     colorImage.HeightPixels,//height of color image
                                     colorImage.StrideBytes, //data size of a stride (width*4)
                                     PixelFormat.Format32bppArgb,//format (RGBA)
                                     (IntPtr)pin.Pointer); //pointer of each pixel
                                // 缩放到一半
                                pictureBox1.Image = new Bitmap(colorBitmap, new Size(1024, 768));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred {ex.Message}");
                    break;
                }
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    Thread.Sleep(1000);
                    _device.StopCameras();
                    _device.Dispose();
                    break;
                }
            }
        }

        private void Start_Click(object sender, EventArgs e)
        {
            if(Device.GetInstalledCount() == 0)
            {
                MessageBox.Show("检测不到相机，请确认是否已连接");
                return;
            }
            _device = Device.Open();

            var configuration = new DeviceConfiguration
            {
                ColorFormat = Microsoft.Azure.Kinect.Sensor.ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R1536p,
                DepthMode = DepthMode.WFOV_2x2Binned,
                SynchronizedImagesOnly = true,
                CameraFPS = FPS.FPS30
            };

            _device.StartCameras(configuration);
            var calibration = _device.GetCalibration(configuration.DepthMode, configuration.ColorResolution);

            _transformation = calibration.CreateTransformation();
            _colourWidth = calibration.ColorCameraCalibration.ResolutionWidth;
            _colourHeight = calibration.ColorCameraCalibration.ResolutionHeight;
            ImageCaptureWorker.RunWorkerAsync();
        }

        private void Stop_Click(object sender, EventArgs e)
        {
            if (ImageCaptureWorker.IsBusy)
            {
                ImageCaptureWorker.CancelAsync();
            }
        }
    }
}

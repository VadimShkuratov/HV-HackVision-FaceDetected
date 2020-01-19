using System;
using System.IO;
using System.Windows;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;



using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace Home
{
    public partial class MainWindow : Window
    {

        Image<Bgr, Byte> currentFrame;
        Capture grabber;
        HaarCascade face;
        HaarCascade eye;
        MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);
        Image<Gray, byte> result, TrainedFace = null;
        Image<Gray, byte> gray = null;
        List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
        List<string> labels = new List<string>();
        List<string> NamePersons = new List<string>();
        int ContTrain, NumLabels, t;
        string name, names = null;
        


        public MainWindow()
        {
            InitializeComponent();
            /////////TIMER_Date///////
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.IsEnabled = true;
            timer.Tick += (o, t) => { label.Content = DateTime.Now.ToString(); };
            timer.Start();

            /////////TIMER_Date///////

            /////////TIMER__Start_Camera_FrameQuery///////

            System.Windows.Threading.DispatcherTimer timer1 = new System.Windows.Threading.DispatcherTimer();
            timer1.Tick += new EventHandler(timerTick);
            timer1.Interval = new TimeSpan(0, 0, 15);
            timer1.IsEnabled = true;
            timer1.Start();
            /////////TIMER__Start_Camera_FrameQuery///////
        }
        
        


        void timerTick(object sender, EventArgs e)
        {
            image1_MouseDown(sender, e);
            
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContTrain = ContTrain + 1;

                gray = grabber.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

                MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
                face,
                1.2,
                10,
                Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                new System.Drawing.Size(20, 20));

                foreach (MCvAvgComp f in facesDetected[0])
                {
                    TrainedFace = currentFrame.Copy(f.rect).Convert<Gray, byte>();
                    break;
                }

                TrainedFace = result.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                trainingImages.Add(TrainedFace);
                labels.Add(textBox.Text);


                using (System.Drawing.Bitmap source = TrainedFace.Bitmap)
                {
                    IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                    BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        ptr,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());


                    image2.Source = bs;

                }

                File.WriteAllText(System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + "/TrainedFaces/TrainedLabels.txt", trainingImages.ToArray().Length.ToString() + "%");

                for (int i = 1; i < trainingImages.ToArray().Length + 1; i++)
                {
                    trainingImages.ToArray()[i - 1].Save(System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + "/TrainedFaces/face" + i + ".bmp");
                    File.AppendAllText(System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + "/TrainedFaces/TrainedLabels.txt", labels.ToArray()[i - 1] + "%");
                }

                MessageBox.Show(textBox.Text + "´s face detected and added :)", "Training OK");
            }
            catch
            {
                MessageBox.Show("Enable the face detection first", "Training Fail");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ////////LOAD_Face///////////////////////
            face = new HaarCascade("haarcascade_frontalface_default.xml");
            try
            {

                string Labelsinfo = File.ReadAllText(System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + "/TrainedFaces/TrainedLabels.txt");
                string[] Labels = Labelsinfo.Split('%');
                NumLabels = Convert.ToInt16(Labels[0]);
                ContTrain = NumLabels;
                string LoadFaces;

                for (int tf = 1; tf < NumLabels + 1; tf++)
                {
                    LoadFaces = "face" + tf + ".bmp";
                    trainingImages.Add(new Image<Gray, byte>(System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + "/TrainedFaces/" + LoadFaces));
                    labels.Add(Labels[tf]);
                }

            }
            catch (Exception tf)
            {
            }

            
        }
        ////////LOAD_Face///////////////////////





        ////////Frame_Image///////////////////////


        public void FrameGrabber(object sender, EventArgs e)
        {
            label1.Content = "0";
            NamePersons.Add("");
            
            

            currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

            gray = currentFrame.Convert<Gray, Byte>();

            MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(face, 1.3, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new System.Drawing.Size(20, 20));

            foreach (MCvAvgComp f in facesDetected[0])
            {
                t = t + 1;
                result = currentFrame.Copy(f.rect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                currentFrame.Draw(f.rect, new Bgr(Color.Red), 2);


                if (trainingImages.ToArray().Length != 0)
                {

                    MCvTermCriteria termCrit = new MCvTermCriteria(ContTrain, 0.001);

                    EigenObjectRecognizer recognizer = new EigenObjectRecognizer(trainingImages.ToArray(), labels.ToArray(), 3000, ref termCrit);

                    
                    name = recognizer.Recognize(result);

                    currentFrame.Draw(name, ref font, new System.Drawing.Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.LightGreen));

                }
                

                NamePersons[t - 1] = name;
                NamePersons.Add("");

                label1.Content = facesDetected[0].Length.ToString();

            }


            t = 0;

            for (int nnn = 0; nnn < facesDetected[0].Length; nnn++)
            {
                names = names + NamePersons[nnn] + ", ";
            }

            using (System.Drawing.Bitmap source = currentFrame.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());


                image.Source = bs;

            }

            label2.Content = names;
            names = "";

            NamePersons.Clear();


        }

        ////////Frame_Image///////////////////////



        ////////LOAD_Cam_Frame///////////////////////


        private void image1_MouseDown(object sender, EventArgs e)
        {
            if (label.Visibility == Visibility.Hidden)
            {
                grabber = new Capture();
                grabber.QueryFrame();
                System.Windows.Interop.ComponentDispatcher.ThreadIdle += new EventHandler(FrameGrabber);
                label.Visibility = Visibility.Visible;
            }
        }

        ////////LOAD_Cam_Frame///////////////////////



        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            System.Windows.Interop.ComponentDispatcher.ThreadIdle -= new EventHandler(FrameGrabber);
            grabber = null;
        }




        



    }


}

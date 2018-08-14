using System.Windows.Navigation;
using System.Collections.ObjectModel;
using System.Runtime.ConstrainedExecution;
using System.Drawing.Imaging;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using PyFaceRecognition;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
using WPFMediaKit.DirectShow.Controls;
using System.Windows.Media.Animation;
using iFlyDotNet;
using System.Threading;

namespace SmartFactory
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        FaceRecoginition faceRec;

        NAudioHelper nAudioHelper;

        private Queue<string> resultQueue=new Queue<string>();

        private const string adminPwd = "123";

        private readonly string loginParam = "appid = 5a404c2c";

        private readonly string bnfPath = "tengen.bnf";

        public ObservableCollection<string> FaceList { get; } = new ObservableCollection<string>();

        private ICommand deleteFaceCommand;

        public ICommand DeleteFaceCommand
        {
            get
            {
                return deleteFaceCommand ?? (deleteFaceCommand = new SimpleCommand(DeleteFace));
            }
        }


        public MainWindow()
        {
            InitializeComponent();
            txtTip.Text = "";
            faceRec = FaceRecoginition.GetInstance(new Func<Bitmap>(() => GetBitmap()));
            faceRec.ResultEvent += ResultHandler;
            faceRec.ExceptionEvent += (msg) => Console.WriteLine(msg);
            if (loginParam != "")
            {
                IFlyCommon.Login(loginParam);
                IFlyAsr.GetInstance().Init(bnfPath);
                IFlyAsr.GetInstance().GetResult += (asrStr) =>
                {
                    this.ShowModalMessageExternal("识别结果", asrStr);
                    //var ttsStr = Talkback.GetResult(asrStr);
                    //if (ttsStr == null)
                    //    return;
                    //var data = IFlyTTS.GetInstance().GetPcmDataByTTS(ttsStr);
                    //nAudioHelper.Play(data);
                };
                nAudioHelper = new NAudioHelper();
                nAudioHelper.PcmDataAvailable += (data) => IFlyAsr.GetInstance().RunAsrRealTime(data, 60);
            }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (MultimediaUtil.VideoInputDevices.Any())
                videoCaptureElement.VideoCaptureDevice = MultimediaUtil.VideoInputDevices[0];
            faceRec?.Start();
            nAudioHelper?.StartRec();
        }

        private Bitmap GetBitmap()
        {
            Bitmap bitmap = null;
            this.Dispatcher.Invoke(() =>
            {
                var imageSource = videoCaptureElement.CloneSingleFrameImage().Source;
                bitmap = ImageSourceToBitmap(imageSource);
            });
            return bitmap;

            Bitmap ImageSourceToBitmap(ImageSource imageSource)
            {
                int width=0;
                int height=0;
                if (imageSource == null)
                    throw new Exception("ImageSource is null");
                var bitmapSource = imageSource as BitmapSource;
                if (bitmapSource == null)
                {
                    width = (int)imageSource.Width;
                    height = (int)imageSource.Height;
                    var dv = new DrawingVisual();
                    using (var dc = dv.RenderOpen())
                    {
                        dc.DrawImage(imageSource, new Rect(0, 0, width, height));
                    }
                    var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(dv);
                    bitmapSource = BitmapFrame.Create(rtb);
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                    encoder.Save(ms);

                    Bitmap bp = new Bitmap(ms);
                    int startX= (int)(width * 0.26);
                    int startY= (int)(height * 0.18);
                    int iWidth = (int)(width * 0.48);
                    int iHeight = (int)(height * 0.64);
                    Bitmap bmpout = new Bitmap(iWidth, iHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                    Graphics g = Graphics.FromImage(bmpout);
                    g.DrawImage(bp, new Rectangle(0,0, iWidth, iHeight), new Rectangle(startX, startY, iWidth,iHeight), GraphicsUnit.Pixel);
                    g.Dispose();
                    return bmpout;
                }
            }
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            faceRec?.Stop();
            nAudioHelper?.StopRec();
        }

        private void ResultHandler(string result)
        {
            resultQueue.Enqueue(result);
            if (resultQueue.Count < 2)
                return;
            if (resultQueue.Max() == resultQueue.Min())
            {
                this.Dispatcher.Invoke(() =>
                {
                    switch (result)
                    {
                        case "未检测到人脸":
                            if (txtTip.Text != "请将脸放入识别框中")
                            {
                                txtTip.Text = "请将脸放入识别框中";
                                Storyboard story = (Storyboard)this.FindResource("TipStory");
                                story.Begin();
                            }
                            break;
                        case "请先录入人脸":
                            if (txtTip.Text != "请先录入人脸")
                            {
                                txtTip.Text = "请先录入人脸";
                                Storyboard story = (Storyboard)this.FindResource("TipStory");
                                story.Begin();
                            }
                            break;
                        case "未知":
                            break;
                        default:
                            if (!txtTip.Text.Contains(result))
                            {
                                txtTip.Text = "你好，"+result;
                                Storyboard story = (Storyboard)this.FindResource("TipStory");
                                story.Begin();
                                AuthenticationPassed();
                            }
                            break;
                    }
                });

                resultQueue.Clear();
                return;
            }
            resultQueue.Dequeue();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (txtUserName.Text != "")
            {
                videoCaptureElement.Pause();
                if (!PasswordAuthentication())
                {
                    videoCaptureElement.Play();
                    return;
                }
                else
                    faceRec.EncodingEnter(txtUserName.Text);
                videoCaptureElement.Play();
                txtUserName.Text = "";
                tbFaceManagement.IsChecked = false;
                tbFaceManagement_Click(null, null);
            }
            else
                this.ShowModalMessageExternal("提示", "请输入用户名");
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            txtUserName.Text = "";
            tbFaceManagement.IsChecked = false;
            tbFaceManagement_Click(null, null);
        }

        private void tbFaceManagement_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)tbFaceManagement.IsChecked)
            {
                faceRec.ResultEvent = null;
                txtTip.Text = "请将脸放入取景框中";
                Storyboard story = (Storyboard)this.FindResource("TipStory");
                story.Begin();
            }
            else
            {
                txtTip.Text = "";
                faceRec.ResultEvent += ResultHandler;
            }
        }

        private void tbFaceKnown_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)tbFaceKnown.IsChecked)
            {
                FaceList.Clear();
                faceRec.FaceList?.ForEach((face) =>
                {
                    FaceList.Add(face);
                });
            }
        }

        private void DeleteFace(object obj)
        {
            if (!PasswordAuthentication())
                return;
            var faceName = obj as string;
            if (faceName == null)
                return;
            FaceList.Remove(faceName);
            faceRec.EncodingDelete(faceName);
        }

        private bool PasswordAuthentication()
        {
            LoginDialogData result = this.ShowModalLoginExternal("身份验证", "输入密码", new LoginDialogSettings { ColorScheme = this.MetroDialogOptions.ColorScheme, ShouldHideUsername = true });
            if (result != null)
            {
                if (result.Password == adminPwd)
                    return true;
            }
            this.ShowModalMessageExternal("提示", "密码错误");
            return false;
        }

        private void btnPwdLogin_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordAuthentication())
                AuthenticationPassed();
        }

        private void AuthenticationPassed()
        {

        }
    }
}

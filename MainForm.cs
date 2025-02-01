using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using OpenCvSharp;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Diagnostics;
using System.Collections.Generic;

namespace ScannedCardDenoiser
{
    enum EErrorType
    {
        WrongProperty,
        ErrorWaifu2x
    }
    public partial class MainForm : Form
    {
        private Thread th;
        string waifu2xExePath;

        public MainForm()
        {
            InitializeComponent();

            waifu2xExePath = Path.Combine(Environment.CurrentDirectory, "waifu2x-ncnn-vulkan\\waifu2x-ncnn-vulkan.exe");
            if (!File.Exists(waifu2xExePath))
            {
                waifu2xExePath = null;
            }
            else
            {
                BTN_waifu2x.Text = waifu2xExePath;
                BTN_waifu2x.Enabled = false;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateTBResize();
        }

        private void BTN_OpenFile_Click(object sender, EventArgs e)
        {            
            if (TB_Source.Text.Length > 0)
                openFileDialog.InitialDirectory = Path.GetDirectoryName(TB_Source.Text);

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                TB_Source.Clear();

                TB_Source.Text = openFileDialog.FileName;
            }
        }

        private void BTN_OpenFolder_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (TB_Source.Text.Length > 0)
                dialog.InitialDirectory = Path.GetDirectoryName(TB_Source.Text);

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                TB_Source.Clear();

                TB_Source.Text = dialog.FileName;
            }
        }

        private void BTN_TargetFolder_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (TB_Target.Text.Length > 0)
                dialog.InitialDirectory = Path.GetDirectoryName(TB_Target.Text);

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                TB_Target.Clear();

                TB_Target.Text = dialog.FileName;
            }
        }

        private void BTN_AutoLevelDefault_Click(object sender, EventArgs e)
        {
            TB_AutoLevelMax.Text = "0";
            TB_AutoLevelMin.Text = "5";
        }

        private void BTN_DenoiseDefault_Click(object sender, EventArgs e)
        {
            TB_DenoiseH.Text = "3";
            TB_DenoiseHColor.Text = "3";
            TB_DenoiseTSize.Text = "27";
            TB_DenoiseSSize.Text = "21";
        }

        private void CB_AutoLevel_CheckedChanged(object sender, EventArgs e)
        {
            TB_AutoLevelMax.Enabled = CB_AutoLevel.Checked;
            TB_AutoLevelMin.Enabled = CB_AutoLevel.Checked;
            BTN_AutoLevelDefault.Enabled = CB_AutoLevel.Checked;
        }

        private void CB_DenoiseColor_CheckedChanged(object sender, EventArgs e)
        {
            TB_DenoiseH.Enabled = CB_DenoiseColor.Checked;
            TB_DenoiseHColor.Enabled = CB_DenoiseColor.Checked;
            TB_DenoiseTSize.Enabled = CB_DenoiseColor.Checked;
            TB_DenoiseSSize.Enabled = CB_DenoiseColor.Checked;
            BTN_AutoLevelDefault.Enabled = CB_AutoLevel.Checked;
        }

        private void CB_Clip_CheckedChanged(object sender, EventArgs e)
        {
            TB_ClipTop.Enabled = CB_Clip.Checked;
            TB_ClipBottom.Enabled = CB_Clip.Checked;
            TB_ClipLeft.Enabled = CB_Clip.Checked;
            TB_ClipRight.Enabled = CB_Clip.Checked;
        }

        private void CB_ChangeSize_CheckedChanged(object sender, EventArgs e)
        {
            UpdateTBResize();
        }

        private void UpdateTBResize()
        {
            RB_ResizeCustom.Enabled = CB_ChangeSize.Checked;
            RB_Resize4x6.Enabled = CB_ChangeSize.Checked;
            RB_ResizeCarddass.Enabled = CB_ChangeSize.Checked;
            RB_ResizeTradingCard.Enabled = CB_ChangeSize.Checked;

            TB_ResizeW.Enabled = CB_ChangeSize.Checked;
            TB_ResizeH.Enabled = CB_ChangeSize.Checked && RB_ResizeCustom.Checked;

            if (RB_Resize4x6.Checked)
            {
                TB_ResizeH.Text = Convert.ToString((int)(Convert.ToInt32(TB_ResizeW.Text) / Math.Sqrt(2)));
            }
            else if (RB_ResizeCarddass.Checked)
            {
                TB_ResizeH.Text = Convert.ToString((int)(Convert.ToInt32(TB_ResizeW.Text) * 59 / 86));
            }
            else if (RB_ResizeTradingCard.Checked)
            {
                TB_ResizeH.Text = Convert.ToString((int)(Convert.ToInt32(TB_ResizeW.Text) * 63 / 89));
            }
        }

        private void RB_ResizeCustom_CheckedChanged(object sender, EventArgs e)
        {
            UpdateTBResize();
        }

        private void RB_Resize4x6_CheckedChanged(object sender, EventArgs e)
        {
            UpdateTBResize();
        }

        private void RB_ResizeCarddass_CheckedChanged(object sender, EventArgs e)
        {
            UpdateTBResize();
        }

        private void RB_ResizeTradingCard_CheckedChanged(object sender, EventArgs e)
        {
            UpdateTBResize();
        }

        private void CB_CornerRounding_CheckedChanged(object sender, EventArgs e)
        {
            TB_CornerRounding.Enabled = CB_CornerRounding.Checked;
        }

        private void BTN_Execute_Click(object sender, EventArgs e)
        {
            if (CB_waifu2x.Checked && waifu2xExePath == null)
            {
                if (!LinkWaifu2X())
                    return;
            }

            BTN_Execute.Enabled = false;
            BTN_Abort.Enabled = true;
            PB_Progress.Value = 0;

            if (th != null)
            {
                th.Abort();
                th = null;
            }

            th = new Thread(Execute);
            th.IsBackground = true;
            th.Start();
        }

        private void BTN_Abort_Click(object sender, EventArgs e)
        {
            if (th != null)
            {
                th.Abort();
                th = null;
            }

            UpdateExecuteButton();
        }

        private void TB_Target_TextChanged(object sender, EventArgs e)
        {
            UpdateExecuteButton();
        }

        private void TB_Source_TextChanged(object sender, EventArgs e)
        {
            UpdateExecuteButton();
            UpdatePreviewButton();
        }

        private void UpdateExecuteButton()
        {
            BTN_Execute.Enabled = TB_Target.Text.Length > 0 && TB_Source.Text.Length > 0;

            if (!Directory.Exists(TB_Source.Text) && !File.Exists(TB_Source.Text))
                BTN_Execute.Enabled &= false;

            if (th != null)
            {
                BTN_Execute.Enabled = !th.IsAlive;
                BTN_Abort.Enabled = th.IsAlive;
            }
            else
            {
                BTN_Execute.Enabled &= true;
                BTN_Abort.Enabled = false;
            }
        }
        
        private void Execute()
        {
            if (!Directory.Exists(TB_Target.Text))
            {
                Directory.CreateDirectory(TB_Target.Text);
            }

            bool bOneFile = false;
            string srcDirectory;
            string[] files;
            if (File.Exists(TB_Source.Text))
            {
                srcDirectory = TB_Source.Text.Remove(TB_Source.Text.LastIndexOf('\\'));
                files = new string[] { TB_Source.Text };
                bOneFile = true;
            }
            else if (Directory.Exists(TB_Source.Text))
            {
                srcDirectory = TB_Source.Text;
                if (CB_SubFolder.Checked)
                    files = Directory.GetFiles(TB_Source.Text, "*.*", SearchOption.AllDirectories);
                else
                    files = Directory.GetFiles(TB_Source.Text, "*.*", SearchOption.TopDirectoryOnly);
            }
            else
            {
                files = new string[] { };
            }

            Label_Progress.Invoke(new Action(() =>
            {
                Label_Progress.Text = "0 / " + files.Length.ToString();
                Label_Progress.Visible = true;
            }));

            PB_Progress.Invoke(new Action(() =>
            {
                PB_Progress.Enabled = true;
            }));

            int DoneCount = 0;
            foreach (string filepath in files)
            {
                string dstDirectory = TB_Target.Text + filepath.Substring(TB_Source.Text.Length);
                if (!bOneFile)
                    dstDirectory = dstDirectory.Remove(dstDirectory.LastIndexOf('\\'));

                if (!Directory.Exists(dstDirectory))
                {
                    Directory.CreateDirectory(dstDirectory);
                }

                Process(filepath, dstDirectory);
                DoneCount++;

                Label_Progress.Invoke(new Action(() =>
                    {
                        Label_Progress.Text = DoneCount.ToString() + " / " + files.Length.ToString();
                    }
                ));

                PB_Progress.Invoke(new Action(() =>
                    {
                        PB_Progress.Value = DoneCount * 100 / files.Length;
                    }
                ));
            }

            BTN_Execute.Invoke(new Action(() =>
            {
                BTN_Execute.Enabled = true;
                BTN_Abort.Enabled = false;
            }));
        }

        private Mat Process(string filepath)
        {
            if (!File.Exists(filepath))
                return null;

            Mat image = Cv2.ImRead(filepath);
            if (image == null)
                return null;

            Cv2.CvtColor(image, image, ColorConversionCodes.RGB2RGBA);
            OpenCvSharp.Size imageSize = new OpenCvSharp.Size(image.Cols, image.Rows);

            if (CB_AutoAdjust.Checked)
            {
                Cv2.CvtColor(image, image, ColorConversionCodes.RGBA2RGB);

                Mat EdgeImage = new Mat();
                Cv2.CvtColor(image, EdgeImage, ColorConversionCodes.RGB2GRAY);
                Cv2.Threshold(EdgeImage, EdgeImage, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
                Cv2.Canny(EdgeImage, EdgeImage, 10, 50);

#if DEBUG
                string newFileName = filepath.Split('\\').Last().Split('.')[0];
                Cv2.ImShow("Edge", EdgeImage);
                Cv2.WaitKey(0);

                Cv2.ImWrite(Path.Combine(Environment.CurrentDirectory, newFileName + "_Edges.png"), EdgeImage);
#endif
                LineSegmentPoint[] Lines = Cv2.HoughLinesP(EdgeImage, 0.5, Cv2.PI / 360, Convert.ToInt32(TB_AdjThreshold.Text), imageSize.Width / 5, imageSize.Width / 100);
                                
#if DEBUG
                Mat LineImage = new Mat();
                Cv2.CopyTo(EdgeImage, LineImage);
                LineImage = LineImage.CvtColor(ColorConversionCodes.GRAY2RGB);
#endif
                EdgeImage.Dispose();

                double maxSkewRad1 = 45 * Cv2.PI / 180;
                double maxSkewRad2 = -45 * Cv2.PI / 180;

                Dictionary<double, int> angleCounts = new Dictionary<double, int>();
#if DEBUG
                Dictionary<double, List<LineSegmentPoint>> angleLines = new Dictionary<double, List<LineSegmentPoint>>();
#endif
                double SelectedAngle = 0;
                foreach (LineSegmentPoint Line in Lines)
                {
                    double angle = Math.Atan2(Line.P2.Y - Line.P1.Y, Line.P2.X - Line.P1.X);

                    if (maxSkewRad1 < angle || angle < maxSkewRad2)
                    {
                        if (angle < 0)
                            angle = angle + Cv2.PI / 2;
                        else
                            angle = angle - Cv2.PI / 2;
                    }

                    angle = (int)(angle * 1000);
                    angle /= 1000;
                    if (angleCounts.ContainsKey(angle))
                        angleCounts[angle]++;
                    else 
                        angleCounts.Add(angle, 1);

#if DEBUG
                    if (angleLines.ContainsKey(angle))
                        angleLines[angle].Add(Line);
                    else
                        angleLines.Add(angle, new List<LineSegmentPoint>() { Line });

                    LineImage.Line(Line.P1, Line.P2, new Scalar(255, 0, 255));
#endif
                }

                angleCounts.OrderByDescending(elem => elem.Value);

                SelectedAngle = angleCounts.ElementAt(0).Key;

#if DEBUG
                List<LineSegmentPoint> SelectedLines = angleLines[SelectedAngle];
                foreach (LineSegmentPoint Line in SelectedLines)
                {
                    LineImage.Line(Line.P1, Line.P2, new Scalar(0, 0, 255), 3);
                }

                Cv2.ImShow("Lines", LineImage);
                Cv2.WaitKey(0);

                Cv2.ImWrite(Path.Combine(Environment.CurrentDirectory, newFileName + "_Lines.png"), LineImage);
                LineImage.Dispose();
#endif

                double rotAngle = SelectedAngle * 180 /Cv2.PI;
                if (rotAngle != 0)
                {
                    Mat RotMat = Cv2.GetRotationMatrix2D(new Point2f(imageSize.Width * 0.5f, imageSize.Height * 0.5f), rotAngle, 1.0);
                    Cv2.WarpAffine(image, image, RotMat, new OpenCvSharp.Size(imageSize.Width, imageSize.Height), InterpolationFlags.Linear, BorderTypes.Replicate);
                }

                int Top = imageSize.Height; int Bottom = 0; int Left = imageSize.Width; int Right = 0;

                Mat[] BinaryImages;                
                Cv2.Split(image, out BinaryImages);

                foreach (Mat BinaryImage in BinaryImages)
                {
                    Cv2.GaussianBlur(BinaryImage, BinaryImage, new OpenCvSharp.Size(5, 5), 0);
                    Cv2.Canny(BinaryImage, BinaryImage, 100, 200);
                    Cv2.Threshold(BinaryImage, BinaryImage, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                    for (int i = 0; i < imageSize.Width * imageSize.Height; ++i)
                    {
                        unsafe
                        {
                            if (*(byte*)IntPtr.Add(BinaryImage.Data, i).ToPointer() != 0)
                            {
                                int HeightIdx = i / imageSize.Width;
                                int WidthIdx = i % imageSize.Width;

                                if (HeightIdx < Top) Top = HeightIdx;
                                if (Bottom < HeightIdx) Bottom = HeightIdx;
                                if (WidthIdx < Left) Left = WidthIdx;
                                if (Right < WidthIdx) Right = WidthIdx;
                            }
                        }
                    }

                    BinaryImage.Dispose();
                }

#if DEBUG
                Mat RectImage = new Mat();
                Cv2.CopyTo(image, RectImage);

                RectImage.Rectangle(new OpenCvSharp.Point(Left, Top), new OpenCvSharp.Point(Right, Bottom), new Scalar(255, 0, 255), 3);

                Cv2.ImShow("Rect", RectImage);
                Cv2.WaitKey(0);

                Cv2.ImWrite(Path.Combine(Environment.CurrentDirectory, newFileName + "_Rect.png"), RectImage);
                RectImage.Dispose();
#endif

                image = image.GetRectSubPix(new OpenCvSharp.Size(Right - Left, Bottom - Top), new OpenCvSharp.Point((Right + Left) * 0.5f, (Bottom + Top) * 0.5f));
                Cv2.CvtColor(image, image, ColorConversionCodes.RGB2RGBA);
                imageSize = new OpenCvSharp.Size(image.Cols, image.Rows);
            }

            bool bHorizontal = imageSize.Width > imageSize.Height;

            int X = CB_ChangeSize.Checked ? (bHorizontal ? Convert.ToInt32(TB_ResizeW.Text) : Convert.ToInt32(TB_ResizeH.Text)) : imageSize.Width;
            int Y = CB_ChangeSize.Checked ? (bHorizontal ? Convert.ToInt32(TB_ResizeH.Text) : Convert.ToInt32(TB_ResizeW.Text)) : imageSize.Height;

            if (X * Y == 0)
                return null;

            if (X * Y > 268435456)
                return null;

            OpenCvSharp.Size size = new OpenCvSharp.Size(X, Y);

            int ClipTop = 0, ClipBottom = 0, ClipLeft = 0, ClipRight = 0;
            if (CB_Clip.Checked)
            {
                ClipTop = Convert.ToInt32(TB_ClipTop.Text);
                ClipBottom = Convert.ToInt32(TB_ClipBottom.Text);
                ClipLeft = Convert.ToInt32(TB_ClipLeft.Text);
                ClipRight = Convert.ToInt32(TB_ClipRight.Text);
            }

            int circleRadius = 0;
            if (CB_CornerRounding.Checked)
            {
                circleRadius = (int)((double)(bHorizontal ? size.Width : size.Height) * 0.01 * Convert.ToInt32(TB_CornerRounding.Text));
            }

            size.Width += ClipLeft + ClipRight;
            size.Height += ClipTop + ClipBottom;

            Mat Original = new Mat();
            Cv2.Resize(image, Original, size);

            Mat After = new Mat();
            if (CB_DenoiseColor.Checked)
            {
                Cv2.FastNlMeansDenoisingColored(
                    Original,
                    After,
                    Convert.ToInt32(TB_DenoiseH.Text),
                    Convert.ToInt32(TB_DenoiseHColor.Text),
                    Convert.ToInt32(TB_DenoiseTSize.Text),
                    Convert.ToInt32(TB_DenoiseSSize.Text)
                );
            }
            else
            {
                Cv2.CopyTo(Original, After);
            }

            Mat AutoLevelSrc = new Mat();
            if (CB_AutoLevel.Checked)
            {
                AutoLevelSrc = AutoLevel(After, Convert.ToInt32(TB_AutoLevelMin.Text), Convert.ToInt32(TB_AutoLevelMax.Text));
            }
            else
            {
                Cv2.CopyTo(After, AutoLevelSrc);
            }

            OpenCvSharp.Size ResultSize = new OpenCvSharp.Size(size.Width - ClipLeft - ClipRight, size.Height - ClipTop - ClipBottom);
            Mat Result = new Mat(ResultSize, MatType.CV_8UC4);

            IntPtr pSrcBitmap = Original.Data;
            IntPtr pDstBitmap = Result.Data;

            bool bCornerRounding = CB_CornerRounding.Checked;
            bool bEdgeLine = CB_EdgeLine.Checked;

            for (int i = 0, j = 0; i < size.Width * size.Height; ++i)
            {
                int HeightIdx = i / size.Width;
                int WidthIdx = i % size.Width;
                if (HeightIdx < ClipTop || HeightIdx > (size.Height - ClipBottom - 1) || WidthIdx < ClipLeft || WidthIdx > (size.Width - ClipRight - 1))
                    continue;

                unsafe
                {
                    IntPtr currentSrcPixel = IntPtr.Add(pSrcBitmap, i * 4);
                    IntPtr currentDstPixel = IntPtr.Add(pDstBitmap, j++ * 4);

                    uint* pSrcPixel = (uint*)currentSrcPixel.ToPointer();
                    uint* pDstPixel = (uint*)currentDstPixel.ToPointer();

                    if (bCornerRounding && IsOutside(WidthIdx, HeightIdx, size, ClipTop, ClipBottom, ClipLeft, ClipRight, circleRadius))
                    {
                        *pDstPixel = 0x00000000;

                        if (bEdgeLine)
                        {
                            if (!IsOutside(WidthIdx - 1, HeightIdx, size, ClipTop, ClipBottom, ClipLeft, ClipRight, circleRadius) ||
                            !IsOutside(WidthIdx + 1, HeightIdx, size, ClipTop, ClipBottom, ClipLeft, ClipRight, circleRadius) ||
                            !IsOutside(WidthIdx, HeightIdx - 1, size, ClipTop, ClipBottom, ClipLeft, ClipRight, circleRadius) ||
                            !IsOutside(WidthIdx, HeightIdx + 1, size, ClipTop, ClipBottom, ClipLeft, ClipRight, circleRadius))
                            {
                                *pDstPixel = 0xff000000;
                            }
                            else if (!IsOutside(WidthIdx - 1, HeightIdx - 1, size, ClipTop, ClipBottom, ClipLeft, ClipRight, circleRadius) ||
                                !IsOutside(WidthIdx - 1, HeightIdx + 1, size, ClipTop, ClipBottom, ClipLeft, ClipRight, circleRadius) ||
                                !IsOutside(WidthIdx + 1, HeightIdx - 1, size, ClipTop, ClipBottom, ClipLeft, ClipRight, circleRadius) ||
                                !IsOutside(WidthIdx + 1, HeightIdx + 1, size, ClipTop, ClipBottom, ClipLeft, ClipRight, circleRadius))
                            {
                                *pDstPixel = 0xff000000;
                            }
                        }
                    }
                    else if (bEdgeLine && ((HeightIdx == ClipTop || HeightIdx == (size.Height - ClipBottom - 1) || WidthIdx == ClipLeft || WidthIdx == (size.Width - ClipRight - 1))))
                    {
                        *pDstPixel = 0xff000000;
                    }
                    else
                    {
                        uint temp = AutoLevelSrc.At<uint>(HeightIdx, WidthIdx);
                        *pDstPixel = temp | 0xff000000;
                    }
                }
            }

            Original.Dispose();
            AutoLevelSrc.Dispose();

            return Result;
        }

        private void Process(string filepath, string dstDirectory)
        {
            Label_FileName.Invoke(new Action(() => 
            {
                Label_FileName.Visible = true;
                Label_FileName.Text = filepath.Split('\\').Last();
            }));
            
            Mat Result = Process(filepath);
            if (Result == null)
            {
                ShowWarning();
                return;
            }

            string newFileName = dstDirectory + '\\' + filepath.Split('\\').Last().Split('.')[0] + ".png";
            if (File.Exists(newFileName))
            {
                File.Delete(newFileName);
            }

            Cv2.ImWrite(newFileName, Result);            

            if (CB_waifu2x.Checked)
            {
                Process ps = new Process();

                ps.StartInfo.FileName = waifu2xExePath;
                ps.StartInfo.CreateNoWindow = true;
                ps.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                string modelPath = " -m ";
                if (RB_WaifuAnime.Checked)
                    modelPath += "models-upconv_7_anime_style_art_rgb";
                else if (RB_WaifuPhoto.Checked)
                    modelPath += "models-upconv_7_photo";
                else if (RB_WaifuCunet.Checked)
                    modelPath += "models-cunet";

                string denoiseLevel = " -n ";
                if (RB_WaifuLow.Checked)
                    denoiseLevel += "0";
                else if (RB_WaifuMed.Checked)
                    denoiseLevel += "1";
                else if (RB_WaifuHigh.Checked)
                    denoiseLevel += "2";
                else if (RB_WaifuHighest.Checked)
                    denoiseLevel += "3";

                ps.StartInfo.Arguments = "-i " + newFileName + " -o " + newFileName + modelPath + denoiseLevel + " -s 2";

                if (CB_Waifu2xTTA.Checked)
                    ps.StartInfo.Arguments += " -x";

                ps.Start();
                ps.WaitForExit();

                if (File.Exists(newFileName))
                {
                    Image image = Image.FromFile(newFileName);
                    System.Drawing.Size imageSize = image.Size;
                    if(imageSize.Width != Result.Width)
                        imageSize.Width = Result.Width;
                    if (imageSize.Height != Result.Height)
                        imageSize.Height = Result.Height;

                    Bitmap newImage = new Bitmap(image, imageSize);
                    image.Dispose();

                    newImage.Save(newFileName, ImageFormat.Png);
                }
                else
                {
                    ShowWarning(EErrorType.ErrorWaifu2x);
                }
            }

            Result.Dispose();
        }

        private void GetMinMaxHist(ref Mat Hist, int totalPixel, double LowCut, double HighCut, out int Min, out int Max)
        {
            Min = 0;
            Max = 0;

            float sum = 0;
            for (int i = 0; i < 256; i++)
            {
                sum += Hist.At<float>(i);
                if (sum >= totalPixel * LowCut * 0.01)
                {
                    Min = i;
                    break;
                }
            }

            sum = 0;
            for (int i = 255; i >= 0; i--)
            {
                sum = sum + Hist.At<float>(i);
                if (sum >= totalPixel * HighCut * 0.01)
                {
                    Max = i;
                    break;
                }
            }
        }

        private void AutoLevelChannelLUT(out byte TableValue, int IndexValue, int Min, int Max)
        {
            if (IndexValue < Min)
            {
                TableValue = 0;
            }
            else
            {
                if (IndexValue > Max || Max == Min)
                {
                    TableValue = 255;
                }
                else
                {
                    float temp = (float)(IndexValue - Min) / (Max - Min);
                    TableValue = (byte)(temp * 255);
                }
            }
        }

        private Mat AutoLevel(Mat src, double LowCut, double HighCut)
        {     
            int rows = src.Rows;
            int cols = src.Cols;
            int totalPixel = rows * cols;

            byte[] Pixel = new byte[256 * 4];

            Mat[] rgb;
            Cv2.Split(src, out rgb);
            Mat HistBlue = new Mat();
            Mat HistGreen = new Mat();
            Mat HistRed = new Mat();

            MatType type = src.Type();

            int[] hdims = { 256 };
            Rangef[] ranges = { new Rangef(0, 255) };

            CLAHE clahe = Cv2.CreateCLAHE();
            clahe.Apply(rgb[2], rgb[2]);
            clahe.Apply(rgb[1], rgb[1]);
            clahe.Apply(rgb[0], rgb[0]);

            Cv2.CalcHist(new Mat[] { rgb[2] }, new int[] { 0 }, null, HistRed, 1, hdims, ranges);
            Cv2.CalcHist(new Mat[] { rgb[1] }, new int[] { 0 }, null, HistGreen, 1, hdims, ranges);
            Cv2.CalcHist(new Mat[] { rgb[0] }, new int[] { 0 }, null, HistBlue, 1, hdims, ranges);

            int MinBlue = 0, MaxBlue = 255;            
            int MinGreen = 0, MaxGreen = 255;
            int MinRed = 0, MaxRed = 255;

            //Blue Channel
            GetMinMaxHist(ref HistBlue, totalPixel, LowCut, HighCut, out MinBlue, out MaxBlue);
            //Green channel
            GetMinMaxHist(ref HistGreen, totalPixel, LowCut, HighCut, out MinGreen, out MaxGreen);
            //Red channel
            GetMinMaxHist(ref HistRed, totalPixel, LowCut, HighCut, out MinRed, out MaxRed);

            for (int i = 0; i < 256; i++)
            {
                int PixelIndexBase = i * 4;
                Pixel[PixelIndexBase + 3] = 255;

                AutoLevelChannelLUT(out Pixel[PixelIndexBase + 2], i, MinBlue, MaxBlue);
                AutoLevelChannelLUT(out Pixel[PixelIndexBase + 1], i, MinGreen, MaxGreen);
                AutoLevelChannelLUT(out Pixel[PixelIndexBase + 0], i, MinRed, MaxRed);
            }

            Mat dst = new Mat();
            Mat TMP = Mat.FromPixelData(1, 256, MatType.CV_8UC4, Pixel);

            Cv2.LUT(src, TMP, dst);
            return dst;
        }

        private bool IsOutside(int WidthIdx, int HeightIdx, OpenCvSharp.Size size, int ClipTop, int ClipBottom, int ClipLeft, int ClipRight, int circleRadius)
        {
            return (WidthIdx < circleRadius && HeightIdx < circleRadius && Math.Sqrt((WidthIdx - circleRadius - ClipLeft) * (WidthIdx - circleRadius - ClipLeft) + (HeightIdx - circleRadius - ClipTop) * (HeightIdx - circleRadius - ClipTop)) > circleRadius) ||
                         (WidthIdx > size.Width - circleRadius && HeightIdx < circleRadius && Math.Sqrt((WidthIdx - size.Width + circleRadius + ClipRight) * (WidthIdx - size.Width + circleRadius + ClipRight) + (HeightIdx - circleRadius - ClipTop) * (HeightIdx - circleRadius - ClipTop)) > circleRadius) ||
                         (WidthIdx < circleRadius && HeightIdx > size.Height - circleRadius && Math.Sqrt((WidthIdx - circleRadius - ClipLeft) * (WidthIdx - circleRadius - ClipLeft) + (HeightIdx - size.Height + circleRadius + ClipBottom) * (HeightIdx - size.Height + circleRadius + ClipBottom)) > circleRadius) ||
                         (WidthIdx > size.Width - circleRadius && HeightIdx > size.Height - circleRadius && Math.Sqrt((WidthIdx - size.Width + circleRadius + ClipRight) * (WidthIdx - size.Width + circleRadius + ClipRight) + (HeightIdx - size.Height + circleRadius + ClipBottom) * (HeightIdx - size.Height + circleRadius + ClipBottom)) > circleRadius);
        }

        private void UpdatePreviewButton()
        {
            BTN_ShowPreview.Enabled = false;

            if (TB_Source.Text.Length > 0)
            {
                if (File.Exists(TB_Source.Text))
                    BTN_ShowPreview.Enabled = true;
            }
        }

        private void BTN_ShowPreview_Click(object sender, EventArgs e)
        {
            string Title = "";
            if (CB_ChangeSize.Checked)
            {
                Title += "[Resize:" + TB_ResizeW.Text + "x" + TB_ResizeH.Text + "]";
            }
            if (CB_Clip.Checked)
            {
                Title += "[Clip:T" + TB_ClipTop.Text + "B" + TB_ClipBottom.Text + "L" + TB_ClipLeft.Text + "R" + TB_ClipRight.Text + "]";
            }
            if (CB_EdgeLine.Checked)
            {
                Title += "[Edge Line]";
            }
            if (CB_CornerRounding.Checked)
            {
                Title += "[Corner:" + TB_CornerRounding.Text + "]";
            }
            if (CB_AutoLevel.Checked)
            {
                Title += "[AutoLevel:" + TB_AutoLevelMin.Text + "_" + TB_AutoLevelMax.Text + "]";
            }
            if (CB_DenoiseColor.Checked)
            {
                Title += "[Denoise:" + TB_DenoiseH.Text + "_" + TB_DenoiseHColor.Text + "_" + TB_DenoiseTSize.Text + "_" + TB_DenoiseSSize.Text + "]";
            }
            if (CB_AutoAdjust.Checked)
            {
                Title += "[AutoAdjust:" + TB_AdjThreshold.Text + "]";
            }

            Mat tmpImage = Process(TB_Source.Text);
            if(tmpImage == null)
            {
                ShowWarning();
                return;
            }                

            if (CB_waifu2x.Checked)
            {
                if (waifu2xExePath == null)
                {
                    if (!LinkWaifu2X())
                    {
                        ShowWarning();
                        return;
                    }
                }

                string tmpFileName = "PreviewTemp.png";

                Cv2.ImWrite("PreviewTemp.png", tmpImage); 

                Process ps = new Process();
                ps.StartInfo.FileName = waifu2xExePath;
                ps.StartInfo.CreateNoWindow = true;
                ps.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                string modelPath = " -m ";
                if (RB_WaifuAnime.Checked)
                {
                    modelPath += "models-upconv_7_anime_style_art_rgb";
                    Title += "[waifu2x : upconv7_anim / ";
                }
                else if (RB_WaifuPhoto.Checked)
                {
                    modelPath += "models-upconv_7_photo";
                    Title += "[waifu2x : upconv7_photo / ";
                }
                else if (RB_WaifuCunet.Checked)
                {
                    modelPath += "models-cunet";
                    Title += "[waifu2x : upconv7_cunet / ";
                }
                 
                string denoiseLevel = " -n ";
                if (RB_WaifuLow.Checked)
                {
                    denoiseLevel += "0";
                    Title += "Denoise Low]";
                }
                else if (RB_WaifuMed.Checked)
                {
                    denoiseLevel += "1";
                    Title += "Denoise Medium]";
                }
                else if (RB_WaifuHigh.Checked)
                {
                    denoiseLevel += "2";
                    Title += "Denoise High]";
                }
                else if (RB_WaifuHighest.Checked)
                {
                    denoiseLevel += "3";
                    Title += "Denoise Highest]";
                }

                ps.StartInfo.Arguments = "-i " + tmpFileName + " -o " + tmpFileName + modelPath + denoiseLevel + " -s 2";

                ps.Start();
                ps.WaitForExit();

                if (File.Exists(tmpFileName))
                {
                    Image image = Image.FromFile(tmpFileName);
                    System.Drawing.Size imageSize = image.Size;
                    if (imageSize.Width != tmpImage.Width)
                        imageSize.Width = tmpImage.Width;
                    if (imageSize.Height != tmpImage.Height)
                        imageSize.Height = tmpImage.Height;

                    Bitmap newImage = new Bitmap(image, imageSize);
                    image.Dispose();

                    newImage.Save(tmpFileName, ImageFormat.Png);

                    tmpImage = Cv2.ImRead(tmpFileName);

                    File.Delete(tmpFileName);
                }
                else
                {
                    ShowWarning(EErrorType.ErrorWaifu2x);
                }
            }

            Cv2.ImShow("Original", Cv2.ImRead(TB_Source.Text));
            Cv2.ImShow(Title, tmpImage);
        }

        private void CB_waifu2x_CheckedChanged(object sender, EventArgs e)
        {
            RB_WaifuAnime.Enabled = CB_waifu2x.Checked;
            RB_WaifuPhoto.Enabled = CB_waifu2x.Checked;
            RB_WaifuCunet.Enabled = CB_waifu2x.Checked;
            RB_WaifuLow.Enabled = CB_waifu2x.Checked;
            RB_WaifuMed.Enabled = CB_waifu2x.Checked;
            RB_WaifuHigh.Enabled = CB_waifu2x.Checked;
            RB_WaifuHighest.Enabled = CB_waifu2x.Checked;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            LinkWaifu2X();
        }

        private bool LinkWaifu2X()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = Environment.CurrentDirectory;
            openFileDialog1.Filter = "waifu2x exe file|waifu2x-ncnn-vulkan.exe";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                waifu2xExePath = openFileDialog1.FileName;
                BTN_waifu2x.Text = waifu2xExePath;
                return true;
            }

            return false;
        }

        private void TB_OnlyDigit_KeyPressed(object sender, KeyPressEventArgs e)
        {
            //숫자와 백스페이스만 입력
            if (!(char.IsDigit(e.KeyChar) || e.KeyChar == Convert.ToChar(Keys.Back)))
            {
                e.Handled = true;
            }
        }

        private void TB_ResizeW_TextChanged(object sender, EventArgs e)
        {
            UpdateTBResize();
        }

        private void ShowWarning(EErrorType Err = EErrorType.WrongProperty)
        {
            switch(Err)
            {
                case EErrorType.WrongProperty: System.Windows.Forms.MessageBox.Show("설정 값을 확인하세요.", "경고"); break;
                case EErrorType.ErrorWaifu2x: System.Windows.Forms.MessageBox.Show("waifu2x 실행 실패", "경고"); break;
            }
            
        }

        private void CB_AutoAdjust_CheckedChanged(object sender, EventArgs e)
        {
            TB_AdjThreshold.Enabled = CB_AutoAdjust.Checked;
        }
    }
}

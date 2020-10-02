using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LiveCharts;
using LiveCharts.Wpf;
using MnistML.Model;

namespace Mnist_WPF
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<float, string> Formatter { get; set; }
        /// <summary>
        /// MainWIndow 생성자
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            SeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "확률",
                    Values = new ChartValues<float> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
                }
            };

            Labels = new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            Formatter = value => value.ToString("N");
            DataContext = this;
        }

        /// <summary>
        /// 이미지의 사이즈를 조정하는 메소드
        /// </summary>
        /// <param name="image">바꿀 이미지</param>
        /// <param name="width">너비</param>
        /// <param name="height">높이</param>
        /// <returns>조정된 이미지</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        /// <summary>
        /// 그림판의 변화가 끝났을 때
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InputCanvasStrokeCollected(object sender, System.Windows.Controls.InkCanvasStrokeCollectedEventArgs e)
        {
            int width = (int)InputCanvas.ActualWidth;
            int height = (int)InputCanvas.ActualHeight;
            // 그림판의 그림을 28 * 28 크기 비트맵으로 변환해서 저장
            RenderTargetBitmap rb = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Default);
            rb.Render(InputCanvas);
            MemoryStream stream = new MemoryStream();
            BitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rb));
            encoder.Save(stream);
            Bitmap bitmap = new Bitmap(stream);
            bitmap = ResizeImage(bitmap, 28, 28);
            bitmap.Save(@"C:\Users\User\source\repos\Mnist\Mnist\bin\Debug\test.png", ImageFormat.Png);

            // 모델의 입력을 방금 저장한 파일로 지정해서 결과 출력
            ModelInput input = new ModelInput();
            input.ImageSource = @"C:\Users\User\source\repos\Mnist\Mnist\bin\Debug\test.png";
            ModelOutput output = ConsumeModel.Predict(input);
            ResultText.Text = output.Prediction;
            SeriesCollection[0].Values = new ChartValues<float>(output.Score.AsEnumerable());
        }

        /// <summary>
        /// 리셋 버튼 눌렀을 때
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetButtonClick(object sender, RoutedEventArgs e)
        {
            InputCanvas.Strokes.Clear();    // 그림판 초기화
        }
    }
}

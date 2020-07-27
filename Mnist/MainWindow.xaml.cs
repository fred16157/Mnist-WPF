using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Mnist
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<float, string> Formatter { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            SeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "확률"
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
            var destRect = new System.Drawing.Rectangle(0, 0, width, height);
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

        private void InputCanvasStrokeCollected(object sender, System.Windows.Controls.InkCanvasStrokeCollectedEventArgs e)
        {
            int width = (int)InputCanvas.ActualWidth;
            int height = (int)InputCanvas.ActualHeight;
            RenderTargetBitmap rb = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Default);
            rb.Render(InputCanvas);
            MemoryStream stream = new MemoryStream();
            BitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rb));
            encoder.Save(stream);

            Bitmap bitmap = new Bitmap(stream);
            bitmap = ResizeImage(bitmap, 28, 28);
            bitmap.Save(@"C:\Users\User\source\repos\Mnist\Mnist\bin\Debug\test.png", ImageFormat.Png);
            ModelInput input = new ModelInput();
            input.ImageSource = @"C:\Users\User\source\repos\Mnist\Mnist\bin\Debug\test.png";
            ModelOutput output = ConsumeModel.Predict(input);
            ResultText.Text = output.Prediction;
            SeriesCollection[0].Values = (IChartValues)output.Score.AsEnumerable();
        }
    }

    public class ConsumeModel
    {
        private static Lazy<PredictionEngine<ModelInput, ModelOutput>> PredictionEngine = new Lazy<PredictionEngine<ModelInput, ModelOutput>>(CreatePredictionEngine);

        // For more info on consuming ML.NET models, visit https://aka.ms/mlnet-consume
        // Method for consuming model in your app
        public static ModelOutput Predict(ModelInput input)
        {
            ModelOutput result = PredictionEngine.Value.Predict(input);
            return result;
        }

        public static PredictionEngine<ModelInput, ModelOutput> CreatePredictionEngine()
        {
            // Create new MLContext
            MLContext mlContext = new MLContext();

            // Load model & create prediction engine
            string modelPath = @"MLModel.zip";
            ITransformer mlModel = mlContext.Model.Load(modelPath, out var modelInputSchema);
            var predEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(mlModel);

            return predEngine;
        }
    }

    public class ModelInput
    {
        [ColumnName("Label"), LoadColumn(0)]
        public string Label { get; set; }


        [ColumnName("ImageSource"), LoadColumn(1)]
        public string ImageSource { get; set; }


    }

    public class ModelOutput
    {
        // ColumnName attribute is used to change the column name from
        // its default value, which is the name of the field.
        [ColumnName("PredictedLabel")]
        public String Prediction { get; set; }
        public float[] Score { get; set; }
    }
}

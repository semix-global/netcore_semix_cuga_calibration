using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithm.MathNet.Helper;
using Net.Utilities.Algorithm.MathNet.Modules;
using ScottPlot;
using ScottPlot.Palettes;
using ScottPlot.WPF;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace AlgorithmTest;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ButtonSavitzkyGolayFilterOnClick(object sender, RoutedEventArgs e)
    {
        const int k = 3;
        const int f = 51;
        var g = SavitzkyGolayFilter.Sgolayfilt(k, f);

        const double dt = 5e-2;
        // 生成间隔dt的x序列, 0到4π Dense(稠密向量)Dense表示稠密存储
        var x = Vector<double>.Build.Dense(Enumerable.Range(0, (int)(10 * Math.PI / dt)).Select(i => i * dt).ToArray());
        var y = Vector<double>.Build.Dense(x.Count, i => Math.Sin(x[i]) * Math.Pow(x[i], 3) + MathNet.Numerics.Distributions.Normal.Sample(10, 20));

        var yG0 = Convolution.Convolve(y, g.Column(0), true);
        var yG1 = Convolution.Convolve(y, g.Column(1), true) / dt;

        var category20 = new Category10();
        var wpfPlot = new WpfPlot();
        var crossHair = wpfPlot.Plot.Add.Crosshair(0, 0);
        crossHair.LineColor = Colors.Red;
        crossHair.TextColor = Colors.White;
        crossHair.TextBackgroundColor = Colors.Red;
        wpfPlot.MouseMove += (senderTemp, eTemp) =>
        {
            if (senderTemp is not WpfPlot tempWpfPlot) return;
            var position = eTemp.GetPosition(wpfPlot);
            var mousePixel = new Pixel(position.X, position.Y);
            var mouseCoordinates = tempWpfPlot.Plot.GetCoordinates(mousePixel);

            crossHair.Position = mouseCoordinates;
            crossHair.VerticalLine.Text = $"{mouseCoordinates.X:f3}";
            crossHair.HorizontalLine.Text = $"{mouseCoordinates.Y:f3}";
            wpfPlot.Refresh();
        };

        // 添加噪声信号
        var scatter = wpfPlot.Plot.Add.Scatter((double[])[.. x], [.. y], category20.GetColor(0));
        scatter.LegendText = "Noisy";

        // 添加平滑信号
        scatter = wpfPlot.Plot.Add.Scatter((double[])[.. x], [.. yG0], category20.GetColor(1));
        scatter.LegendText = "Smoothed";

        // 添加一阶导数
        scatter = wpfPlot.Plot.Add.Scatter((double[])[.. x], [.. yG1], category20.GetColor(2));
        scatter.LegendText = "First Derivative";

        wpfPlot.Plot.Title("Savitzky-Golay Filter");
        wpfPlot.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);
        wpfPlot.Plot.Axes.AutoScale();
        wpfPlot.Refresh();

        var window = new Window
        {
            Title = "SavitzkyGolayFilter",
            Content = wpfPlot,
            Width = 800,
            Height = 800,
            Padding = new Thickness(5, 5, 5, 5),
            WindowState = WindowState.Maximized,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        window.ShowDialog();
    }

    private void ButtonAutomaticMPeakDetectionOnClick(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Image File",
            Filter = "Image Files (*.jpg;*.raw)|*.jpg;*.raw|All Files (*.*)|*.*", // 允许选择JPG和RAW文件
            RestoreDirectory = true // 记住上次打开的目录
        };

        if (openFileDialog.ShowDialog() == false) return;

        double[] y;
        var extension = Path.GetExtension(openFileDialog.FileName);
        switch (extension)
        {
            case ".jpg":
                var imageToMatrix1 = ImageToMatrix(openFileDialog.FileName);
                // 将所有的行相加取平均值，并取反
                y = [.. imageToMatrix1.RowSums().Divide(imageToMatrix1.RowCount).Select(t => -t)];
                break;

            case ".raw":
                if (false)
                {
                    var imageToMatrix2 = RawImageToMatrix(openFileDialog.FileName);
                    // 将所有的行相加取平均值，并取反
                    y = [.. imageToMatrix2.RowSums().Divide(imageToMatrix2.RowCount).Select(t => -t)];
                }
                else
                {
                    var imageToMatrix2 = RawImageToMatrix(openFileDialog.FileName);
                    // 将所有的行相加取平均值，并取反
                    y = [.. imageToMatrix2.ColumnSums().Divide(imageToMatrix2.ColumnCount)];
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(extension);
        }

        var x = Enumerable.Range(0, y.Length).Select(t => (double)t).ToArray();

        // 使用AMPD算法找出波峰
        var signal = Vector<double>.Build.DenseOfArray(y);
        var peaks = AutomaticMPeakDetection.Ampd(signal);
        // 所有后一个减去前一个，得到差值, 然后取得均值
        var mean = peaks.Skip(1).Select((t, i) => (double)t - peaks[i]).Average();
        var pixelY = 10 / mean;

        var category20 = new Category10();
        var wpfPlot = new WpfPlot();
        var crossHair = wpfPlot.Plot.Add.Crosshair(0, 0);
        crossHair.LineColor = Colors.Red;
        crossHair.TextColor = Colors.White;
        crossHair.TextBackgroundColor = Colors.Red;
        wpfPlot.MouseMove += (senderTemp, eTemp) =>
        {
            if (senderTemp is not WpfPlot tempWpfPlot) return;
            var position = eTemp.GetPosition(wpfPlot);
            var mousePixel = new Pixel(position.X, position.Y);
            var mouseCoordinates = tempWpfPlot.Plot.GetCoordinates(mousePixel);

            crossHair.Position = mouseCoordinates;
            crossHair.VerticalLine.Text = $"{mouseCoordinates.X:f3}";
            crossHair.HorizontalLine.Text = $"{mouseCoordinates.Y:f3}";
            wpfPlot.Refresh();
        };

        // 添加噪声信号
        var scatter = wpfPlot.Plot.Add.Scatter((double[])[.. x], [.. y], category20.GetColor(0));
        scatter.LegendText = "Noisy";

        var markers = wpfPlot.Plot.Add.Markers((double[])[.. peaks], peaks.Select(t => y[t]).ToArray(), MarkerShape.FilledDiamond, 10, category20.GetColor(1));
        markers.LegendText = "Peaks";

        wpfPlot.Plot.Title("AutomaticMPeakDetection");
        wpfPlot.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);
        wpfPlot.Plot.Axes.AutoScale();
        wpfPlot.Refresh();

        var window = new Window
        {
            Title = $"AutomaticMPeakDetection {pixelY}",
            Content = wpfPlot,
            Width = 800,
            Height = 800,
            Padding = new Thickness(5, 5, 5, 5),
            WindowState = WindowState.Maximized,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        window.ShowDialog();
        return;

        static Matrix<double> RawImageToMatrix(string imagePath)
        {
            var data = File.ReadAllBytes(imagePath);
            TryProtocolBytesToMatrix(data, out var shorts);

            return MathNetHelper.ConvertToDoubleMatrix(shorts);
        }

        static Matrix<double> ImageToMatrix(string imagePath)
        {
            // Load the image using System.Drawing
            var bitmap = new Bitmap(imagePath);

            // Initialize a matrix to store the image data
            var matrix = Matrix<double>.Build.Dense(bitmap.Height, bitmap.Width);

            // Iterate through each pixel in the image
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    // Get the color of the pixel
                    var pixelColor = bitmap.GetPixel(x, y);

                    // Convert the color to grayscale
                    double grayscaleValue = pixelColor.R + pixelColor.G + pixelColor.B;

                    // Store the grayscale value in the matrix
                    matrix[y, x] = grayscaleValue;
                }
            }

            return matrix;
        }

        static bool TryProtocolBytesToMatrix(byte[] rawBytes, out short[,] matrix)
        {
            matrix = MatrixHelper.EmptyMatrix<short>();

            if (TryResolveProtocolBytes(rawBytes, out var widthPixel, out var heightPixel, out var startIndex, out var imageRawBytesLength) == false) return false;

            var pictureRawSpan = rawBytes.AsSpan().Slice(startIndex, imageRawBytesLength);

            var shorts = MemoryMarshal.Cast<byte, short>(pictureRawSpan).ToArray();

            if (heightPixel * widthPixel != shorts.Length) throw new ArgumentException("Data length is not a multiple of width."); // 高度不是整数

            var convertToMatrix = MatrixHelper.ToMatrixByRow(shorts, widthPixel, heightPixel);
            matrix = MatrixHelper.Transpose(convertToMatrix); // 线扫相机扫图是一列一列的拼接上去的, 所以这里的宽度是列, 高度是行. 所以需要按照常数[不会改变像素值]旋转90度

            return true;
        }

        static bool TryResolveProtocolBytes(byte[] rawBytes, out int widthPixel, out int heightPixel, out int imageRawBytesStartIndex, out int imageRawBytesLength)
        {
            widthPixel = 0;
            heightPixel = 0;
            imageRawBytesStartIndex = 0;
            imageRawBytesLength = 0;

            widthPixel = BitConverter.ToInt32(rawBytes, 10);
            heightPixel = BitConverter.ToInt32(rawBytes, 18);
            imageRawBytesStartIndex = 28;
            imageRawBytesLength = widthPixel * heightPixel * 2; // (16位图片0-65535, 并且是单通道), 实际上我们线扫相机是12bit(0-4095)[为了明暗差别大], 为了解析方便解析16bit浪费多余的传输带宽

            if (imageRawBytesLength > rawBytes.Length - imageRawBytesStartIndex) throw new ArgumentException("Data length is not a multiple of width."); // 高度不是整数

            return true;
        }
    }

    private void ButtonOnMovMeanClick(object sender, RoutedEventArgs e)
    {
        const int length = 100;
        const int stepSize = 5;
        const int minValue = 0;
        const int maxValue = 10;

        // 生成随机阶梯数组
        var randomStepArray = GenerateRandomStepArray(length, stepSize, minValue, maxValue);

        // 计算滑动平均
        const int windowSize = 10;
        var smoothedArray = MovMeanFilter.Smooth(windowSize, Vector<double>.Build.DenseOfArray(randomStepArray));

        var category20 = new Category10();
        var wpfPlot = new WpfPlot();
        var crossHair = wpfPlot.Plot.Add.Crosshair(0, 0);
        crossHair.LineColor = Colors.Red;
        crossHair.TextColor = Colors.White;
        crossHair.TextBackgroundColor = Colors.Red;
        wpfPlot.MouseMove += (senderTemp, eTemp) =>
        {
            if (senderTemp is not WpfPlot tempWpfPlot) return;
            var position = eTemp.GetPosition(wpfPlot);
            var mousePixel = new Pixel(position.X, position.Y);
            var mouseCoordinates = tempWpfPlot.Plot.GetCoordinates(mousePixel);

            crossHair.Position = mouseCoordinates;
            crossHair.VerticalLine.Text = $"{mouseCoordinates.X:f3}";
            crossHair.HorizontalLine.Text = $"{mouseCoordinates.Y:f3}";
            wpfPlot.Refresh();
        };

        // 添加噪声信号
        var scatter = wpfPlot.Plot.Add.Scatter(Enumerable.Range(0, randomStepArray.Length).Select(x => (double)x).ToArray(), randomStepArray, category20.GetColor(1));
        scatter.LegendText = "Random Step Array";

        var markers = wpfPlot.Plot.Add.Scatter(Enumerable.Range(0, smoothedArray.Count).Select(x => (double)x).ToArray(), [.. smoothedArray], category20.GetColor(2));
        markers.LegendText = "Smoothed (MovMean)";

        wpfPlot.Plot.Title("MovMean");
        wpfPlot.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);
        wpfPlot.Plot.Axes.AutoScale();
        wpfPlot.Refresh();

        var window = new Window
        {
            Title = "MovMean",
            Content = wpfPlot,
            Width = 800,
            Height = 800,
            Padding = new Thickness(5, 5, 5, 5),
            WindowState = WindowState.Maximized,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        window.ShowDialog();
        return;

        static double[] GenerateRandomStepArray(int length, int stepSize, int minValue, int maxValue)
        {
            var rand = new Random();
            var data = new double[length];
            var currentValue = rand.Next(minValue, maxValue);

            for (var i = 0; i < length; i++)
            {
                if (i % stepSize == 0)
                {
                    currentValue = rand.Next(minValue, maxValue); // 每stepSize改变一次值
                }

                data[i] = currentValue;
            }

            return data;
        }
    }

    private void ButtonOnRawSplitClick(object sender, RoutedEventArgs e)
    {
    }
}
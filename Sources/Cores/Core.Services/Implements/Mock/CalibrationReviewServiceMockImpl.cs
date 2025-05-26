using Core.Models.Helper;
using Core.Services.Interfaces;
using HalconDotNet;
using Net.Utilities.Algorithm.Halcon.Helper;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helper.IOC.Providers;
using Net.Utilities.WPF.Helper;
using Semix.CoreLib;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Size = Net.Utilities.Models.Size;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationReviewService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationReviewServiceMockImpl(ISynchronizationContextProvider contextProvider) : ICalibrationReviewService
{
    private static readonly Random Random = new();

    private const int Width = 2448;
    private const int Height = 2048;
    private const int Channels = 4;

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<HObject> GetBrightFieldImage()
    {
        var bitmapMemoryByteArray = GetBrightFieldImageMemoryByteArray().Anything;
        var bytes = BitmapSourceHelper.BitmapSourceToByteRawArray(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(bitmapMemoryByteArray));

        return SxExecuteRetHelper.CreateSuccess(HalconHelper.ImageRawBytesToHObject(bytes, Width, Height, Channels));
    }

    public SxExecuteRet<byte[]> GetBrightFieldImageMemoryByteArray()
    {
        try
        {
            BitmapSource bmp = null!;
            contextProvider.Send(() =>
            {
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawRectangle(Brushes.DarkGray, null, new Rect(0, 0, Width, Height));
                    // 画50个随机椭圆
                    for (var i = 0; i < 10; i++)
                    {
                        double centerX = Random.Next(Width);
                        double centerY = Random.Next(Height);
                        double radiusX = Random.Next(500);
                        double radiusY = Random.Next(500);
                        drawingContext.DrawEllipse(Brushes.LightBlue, null, new Point(centerX, centerY), radiusX, radiusY);
                    }

                    drawingContext.DrawEllipse(Brushes.Black, null, new Point(Width / 2d + Random.NextDouble(), Height / 2d + Random.NextDouble()), 10, 10);
                }

                // 创建一个RenderTargetBitmap，用于保存绘制的内容
                var temp = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);
                temp.Render(drawingVisual);
                temp.Freeze();
                bmp = new FormatConvertedBitmap(temp, BitmapSourceHelper.GetPixelFormat(Channels), null, 0);
                bmp.Freeze();
            });

            return SxExecuteRetHelper.CreateSuccess(BitmapSourceHelper.BitmapSourceToBitmapMemoryByteArray(bmp));
        }
        catch (Exception ex)
        {
            return SxExecuteRetHelper.CreateError(ex.Message, Array.Empty<byte>());
        }
    }

    public SxExecuteRet<Size> GetBrightFieldImagePixelSize()
    {
        return SxExecuteRetHelper.CreateSuccess(new Size(Width, Height));
    }
}
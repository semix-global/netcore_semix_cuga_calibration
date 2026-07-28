using HalconDotNet;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Models.Geometries;

namespace Net.Utilities.Calibration;

public static class HImageDrawExtensions
{
    /// <param name="this">图片</param>
    extension(HImage @this)
    {
        /// <summary>
        /// 图片画直线组合
        /// </summary>
        /// <param name="lines">线段</param>
        /// <param name="lineWidth">线段宽度</param>
        /// <returns>图片画直线组合</returns>
        public HImage DrawLines((Point Point1, Point Point2)[] lines, double lineWidth)
        {
            var size = @this.GetSize();
            using var window = HalconFactory.CreateBufferWindow(size);

            window.SetPart(0, 0, size.Height - 1, size.Width - 1);
            @this.DispImage(window);

            window.SetColor("red");
            window.SetLineWidth(lineWidth);
            window.SetDraw("margin"); // 设置绘图模式为边框(不要填充)
            foreach (var (point1, point2) in lines) window.DispLine(point1.Y, point1.X, point2.Y, point2.X);

            var resultImage = window.DumpWindowImage();

            window.CloseWindow();

            return resultImage;
        }
    }
}
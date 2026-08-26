using Net.Utilities.Models.Geometries;

namespace Net.Utilities.Calibration;

public static class RectExtensions
{
    extension(Rect rect)
    {
        /// <summary>
        /// 将两个 Rect 的尺寸求最大公共交集尺寸，并分别以该尺寸从各自 Rect 中心裁剪，
        /// 返回两个裁剪后的 Rect。
        /// </summary>
        /// <param name="cropRect">另一个待裁剪的 Rect。</param>
        /// <returns>裁剪后的两个 Rect。</returns>
        public (Rect CropRect1, Rect CropRect2) Intersection(Rect cropRect)
        {
            var size1 = new Size(rect.Width, rect.Height);
            var size2 = new Size(cropRect.Width, cropRect.Height);

            var intersectionSize = new Size(
                Math.Min(size1.Width, size2.Width),
                Math.Min(size1.Height, size2.Height));

            var cropRect1 = new Rect(
                rect.X + (rect.Width - intersectionSize.Width) / 2,
                rect.Y + (rect.Height - intersectionSize.Height) / 2,
                intersectionSize.Width,
                intersectionSize.Height);

            var cropRect2 = new Rect(
                cropRect.X + (cropRect.Width - intersectionSize.Width) / 2,
                cropRect.Y + (cropRect.Height - intersectionSize.Height) / 2,
                intersectionSize.Width,
                intersectionSize.Height);

            return (cropRect1, cropRect2);
        }
    }
}
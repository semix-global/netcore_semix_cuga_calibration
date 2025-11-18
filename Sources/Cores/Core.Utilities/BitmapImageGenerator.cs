using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using SkiaSharp;

namespace Core.Utilities;

public static class BitmapImageGenerator
{
    public static BitmapImage GenerateRandomImage(int width, int height, int shapeCount, Random random)
    {
        using var skBitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(skBitmap);

        canvas.Clear(new SKColor(
            (byte)random.Next(256),
            (byte)random.Next(256),
            (byte)random.Next(256)
        ));

        using var paint = new SKPaint();
        paint.IsAntialias = true;

        for (var i = 0; i < shapeCount; i++)
        {
            paint.Color = new SKColor(
                (byte)random.Next(256),
                (byte)random.Next(256),
                (byte)random.Next(256));

            if (random.NextDouble() < 0.5)
            {
                // 随机圆
                float r = random.Next(20, 120);
                float x = random.Next((int)r, width - (int)r);
                float y = random.Next((int)r, height - (int)r);
                canvas.DrawCircle(x, y, r, paint);
            }
            else
            {
                // 随机矩形
                float w = random.Next(40, 200);
                float h = random.Next(40, 200);
                float x = random.Next(0, width - (int)w);
                float y = random.Next(0, height - (int)h);
                canvas.DrawRect(x, y, w, h, paint);
            }
        }

        var centerX = width / 2f;
        var centerY = height / 2f;
        var centerRadius = Math.Min(width, height) * 0.1f; // 可改成固定值

        paint.Color = SKColors.Black;
        canvas.DrawCircle(centerX, centerY, centerRadius, paint);

        return new BitmapImage(ImageInfoFactory.Create(skBitmap.Info), skBitmap.GetPixels());
    }
}
using CanvasViewer.Geometry;
using CanvasViewer.Media;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CanvasViewer.Drawables;

public sealed class DrawImage : AbstractDrawable
{
    private BitmapSource? _image;

    public BitmapSource? Image
    {
        get => _image;
        set
        {
            GC.Collect();
            value?.Freeze();
            SetField(ref _image, value);
        }
    }

    public override void Draw(Renderer renderer)
    {
        if (Image is null) return;

        // 绘制图像到指定位置
        renderer.DrawImage(Image, new Rect(-Image.PixelWidth / 2f, -Image.PixelHeight / 2f, Image.PixelWidth, Image.PixelHeight));
    }

    public override Extents2D GetExtents()
    {
        if (Image is null) return Extents2D.Empty;

        var extents2D = new Extents2D();
        // 2.19f 是为了让图像的边界与控件更接近
        extents2D.Add(new Point2D(-Image.PixelWidth / 2.19f, -Image.PixelHeight / 2.19f));
        extents2D.Add(new Point2D(Image.PixelWidth / 2.19f, Image.PixelHeight / 2.19f));
        return extents2D;
    }

    public override void TransformBy(Matrix2D transformation)
    {
    }

    public override bool Contains(Point2D pt, double pickBoxSize)
    {
        return false;
    }
}
using AwesomeAssertions;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;

namespace CugaCalibrationUnitTest.OpticsFourierImageViewer;

/// <summary>
/// BitmapImageDrawable 图片坐标系 (原点左上, Y 向下) 与笛卡尔坐标系 (原点左下, Y 向上) 的取整和转换测试.
/// Round 收到所在像素左上角: 图片坐标 Floor(X), Floor(Y); 笛卡尔坐标 Floor(X), Ceiling(Y).
/// 恒等前提: BitmapImageDrawable.Point 为整数. 整数格点往返必须还原; 非整数先吸附到取整格点, 再转换必须等于该格点.
/// 图高 205, 纵向中心 102.5 不是整数格点. Point 取 (0,0) 与 (20,-218).
/// </summary>
public class BitmapImageDrawableCoordinateTest
{
    private const int ImageWidth = 244;
    private const int ImageHeight = 205;

    private static Point[] ImagePoints() =>
    [
        new(0d, 0d),
        new(7d, 33d),
        new(ImageWidth / 2d, (ImageHeight - 1) / 2d),
        new(ImageWidth / 2d, ImageHeight / 2d),
        new(ImageWidth - 1d, ImageHeight - 1d),
        new(ImageWidth, ImageHeight),
        new(-5d, -3d),
        new(ImageWidth + 5d, ImageHeight + 3d),
        new(10.5, 20.25),
        new(0.5, 99.5),
        new(-1.5, -0.5)
    ];

    private static Rect[] ImageRects() =>
    [
        new(10d, 20d, 30d, 40d),
        new(10.5, 20.25, 5.5, 3.25),
        new(-1.5, -0.5, 2.5, 1.5)
    ];

    [Theory]
    [InlineData(10.5, 20.25, 10d, 20d)]
    [InlineData(0.5, 99.5, 0d, 99d)]
    [InlineData(ImageWidth / 2d, ImageHeight / 2d, ImageWidth / 2d, 102d)]
    [InlineData(-1.5, -0.5, -2d, -1d)]
    [InlineData(-0.5, 5d, -1d, 5d)]
    [InlineData(5d, -0.5, 5d, -1d)]
    [InlineData(0d, 0d, 0d, 0d)]
    [InlineData(7d, 33d, 7d, 33d)]
    [InlineData(-5d, -3d, -5d, -3d)]
    public void ImageCoordinateRound_ShouldSnapToContainingPixelTopLeft(double x, double y, double expectedX, double expectedY)
    {
        new Point(x, y).ImageCoordinateRound().Should().Be(new Point(expectedX, expectedY));
    }

    [Theory]
    [InlineData(10.5, 20.25, 10d, 21d)]
    [InlineData(0.5, 99.5, 0d, 100d)]
    [InlineData(ImageWidth / 2d, ImageHeight / 2d, ImageWidth / 2d, 103d)]
    [InlineData(-1.5, -0.5, -2d, 0d)]
    [InlineData(-0.5, 5.5, -1d, 6d)]
    [InlineData(5d, -2.5, 5d, -2d)]
    [InlineData(0d, 0d, 0d, 0d)]
    [InlineData(7d, 33d, 7d, 33d)]
    [InlineData(-5d, -3d, -5d, -3d)]
    public void CartesianCoordinateRound_ShouldSnapToContainingPixelTopLeft(double x, double y, double expectedX, double expectedY)
    {
        new Point(x, y).CartesianCoordinateRound().Should().Be(new Point(expectedX, expectedY));
    }

    [Fact]
    public void CoordinateRound_Rect_ShouldSnapBothCornersToContainingPixelTopLeft()
    {
        new Rect(10.5, 20.25, 5.5, 3.25).ImageCoordinateRound().Should().Be(new Rect(10d, 20d, 6d, 3d));
        new Rect(-1.5, -0.5, 2.5, 1.5).ImageCoordinateRound().Should().Be(new Rect(-2d, -1d, 3d, 2d));
        new Rect(10d, 20d, 30d, 40d).ImageCoordinateRound().Should().Be(new Rect(10d, 20d, 30d, 40d));

        new Rect(10.5, 20.25, 5.5, 3.25).CartesianCoordinateRound().Should().Be(new Rect(10d, 21d, 6d, 3d));
        new Rect(-1.5, -0.5, 2.5, 1.5).CartesianCoordinateRound().Should().Be(new Rect(-2d, 0d, 3d, 1d));
        new Rect(10d, 20d, 30d, 40d).CartesianCoordinateRound().Should().Be(new Rect(10d, 20d, 30d, 40d));
    }

    [Theory]
    [InlineData(0d, 0d)]
    [InlineData(20d, -218d)]
    public void ImageCoordinate_RoundThenConvert_ShouldEqualRoundedPoint(double pointX, double pointY)
    {
        using var drawable = CreateDrawable(new Point(pointX, pointY));

        foreach (var imagePoint in ImagePoints())
        {
            var rounded = imagePoint.ImageCoordinateRound();
            var worldPoint = drawable.ImageCoordinateToCartesianCoordinate(imagePoint);

            worldPoint.Should().Be(new Point(pointX + rounded.X, pointY + ImageHeight - rounded.Y));
            worldPoint.Should().Be(drawable.ImageCoordinateToCartesianCoordinate(rounded));
            drawable.CartesianCoordinateToImageCoordinate(worldPoint).Should().Be(rounded);
        }
    }

    [Theory]
    [InlineData(0d, 0d)]
    [InlineData(20d, -218d)]
    public void CartesianCoordinate_RoundThenConvert_ShouldEqualRoundedPoint(double pointX, double pointY)
    {
        using var drawable = CreateDrawable(new Point(pointX, pointY));

        foreach (var imagePoint in ImagePoints())
        {
            var worldPoint = new Point(pointX + imagePoint.X, pointY + imagePoint.Y);
            var rounded = worldPoint.CartesianCoordinateRound();
            var convertedImagePoint = drawable.CartesianCoordinateToImageCoordinate(worldPoint);

            convertedImagePoint.Should().Be(new Point(rounded.X - pointX, ImageHeight - (rounded.Y - pointY)));
            convertedImagePoint.Should().Be(drawable.CartesianCoordinateToImageCoordinate(rounded));
            drawable.ImageCoordinateToCartesianCoordinate(convertedImagePoint).Should().Be(rounded);
        }
    }

    [Theory]
    [InlineData(0d, 0d)]
    [InlineData(20d, -218d)]
    public void ImageCoordinate_RoundThenConvert_ShouldEqualRoundedRect(double pointX, double pointY)
    {
        using var drawable = CreateDrawable(new Point(pointX, pointY));

        foreach (var imageRect in ImageRects())
        {
            var rounded = imageRect.ImageCoordinateRound();
            var worldRect = drawable.ImageCoordinateToCartesianCoordinate(imageRect);

            worldRect.Should().Be(drawable.ImageCoordinateToCartesianCoordinate(rounded));
            drawable.CartesianCoordinateToImageCoordinate(worldRect).Should().Be(rounded);
        }
    }

    [Theory]
    [InlineData(0d, 0d)]
    [InlineData(20d, -218d)]
    public void CartesianCoordinate_RoundThenConvert_ShouldEqualRoundedRect(double pointX, double pointY)
    {
        using var drawable = CreateDrawable(new Point(pointX, pointY));

        foreach (var imageRect in ImageRects())
        {
            var worldRect = new Rect(pointX + imageRect.X, pointY + imageRect.Y, imageRect.Width, imageRect.Height);
            var rounded = worldRect.CartesianCoordinateRound();
            var convertedImageRect = drawable.CartesianCoordinateToImageCoordinate(worldRect);

            convertedImageRect.Should().Be(drawable.CartesianCoordinateToImageCoordinate(rounded));
            drawable.ImageCoordinateToCartesianCoordinate(convertedImageRect).Should().Be(rounded);
        }
    }

    private static BitmapImageDrawable CreateDrawable(Point point)
    {
        return new BitmapImageDrawable
        {
            BitmapImage = BitmapImage.Random(ImageWidth, ImageHeight, 0),
            Point = point
        };
    }
}
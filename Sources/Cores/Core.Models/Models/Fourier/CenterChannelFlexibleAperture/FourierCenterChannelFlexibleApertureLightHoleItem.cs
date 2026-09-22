using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Enums.Editors;
using Net.Utilities.OpticsFourierImageViewer.WPF.Editors;
using Net.Utilities.WPF.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;

namespace Core.Models.Models.Fourier.CenterChannelFlexibleAperture;

/// <summary>
/// 中心通道 60 度通光孔的圆拟合结果.
/// </summary>
public sealed partial class FourierCenterChannelFlexibleApertureLightHoleItem : ObservableObject, IDisposable
{
    private readonly BitmapImageDrawable _bitmapImageDrawable = new();
    private readonly BitmapImageROIDrawable[] _pointROIs;

    [ObservableProperty]
    public partial string ImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Point[] Points { get; set; } = new Point[4];

    [ObservableProperty]
    public partial Point Center { get; private set; }

    [ObservableProperty]
    public partial double Radius { get; private set; }

    public CenterChannelMotionKind Axis { get; }

    [Newtonsoft.Json.JsonIgnore]
    public OpticsFourierImageDocument Document { get; } = new();

    [Newtonsoft.Json.JsonIgnore]
    public bool IsCalculated => Radius > 0d && double.IsFinite(Radius) && Center.X is var centerX && Center.Y is var centerY && double.IsFinite(centerX) && double.IsFinite(centerY);

    public FourierCenterChannelFlexibleApertureLightHoleItem(CenterChannelMotionKind axis)
    {
        if (axis == CenterChannelMotionKind.Push) throw new ArgumentOutOfRangeException(nameof(axis));

        Axis = axis;

        _pointROIs = [.. Enumerable.Range(0, 4).Select(_ => new BitmapImageROIDrawable(_bitmapImageDrawable)
        {
            ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.None
        })];

        Document.RunDesign(() =>
        {
            Document.ImageModel.Add(_bitmapImageDrawable);
            Document.ROIModel.AddRange(_pointROIs);
        });
        Document.Reset();
    }

    [Newtonsoft.Json.JsonConstructor]
    private FourierCenterChannelFlexibleApertureLightHoleItem(
        CenterChannelMotionKind axis,
        string imageFilePath,
        Point[] points,
        Point center,
        double radius)
        : this(axis)
    {
        ImageFilePath = imageFilePath;
        Points = points;
        Center = center;
        Radius = radius;
    }

    public void SetResult(Point center, double radius)
    {
        if (double.IsFinite(center.X) == false || double.IsFinite(center.Y) == false || double.IsFinite(radius) == false || radius <= 0d)
            throw new ArgumentOutOfRangeException(nameof(radius), "Circle result must contain finite values and a positive radius.");

        Center = center;
        Radius = radius;
    }

    public void Calculate(Func<IReadOnlyList<Point>, (Point CenterPosition, double Radius)> fitCircle)
    {
        if (fitCircle is null) throw new ArgumentNullException(nameof(fitCircle));
        if (Points.Length != 4 || Points.Any(t => double.IsFinite(t.X) == false || double.IsFinite(t.Y) == false))
            throw new InvalidOperationException("Four circle points are required.");

        var result = fitCircle(Points);
        SetResult(result.CenterPosition, result.Radius);
    }

    public void Review()
    {
        Document.Reset();
        if (string.IsNullOrWhiteSpace(ImageFilePath)) return;

        _bitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ImageFilePath);
        for (var i = 0; i < _pointROIs.Length && i < Points.Length; i++)
        {
            var point = Points[i];
            _pointROIs[i].Rect = _bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(new Rect(point.X - 5d, point.Y - 5d, 10d, 10d));
            _pointROIs[i].Text = $"{i + 1}";
            _pointROIs[i].IsFixed = true;
        }

        Document.View.ZoomToFit();
    }

    public async Task CalibratingAsync(CancellationToken cancellationToken)
    {
        var imageFilePath = ImageFilePath;
        Reset();
        ImageFilePath = imageFilePath;
        Guard.IsNotNullOrWhiteSpace(ImageFilePath);
        _bitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ImageFilePath);
        var imageSize = _bitmapImageDrawable.BitmapImage?.Size ?? throw new InvalidOperationException("The calibration image is empty.");
        var points = new[]
        {
            new Point(imageSize.Width * 0.25d, imageSize.Height * 0.5d),
            new Point(imageSize.Width * 0.5d, imageSize.Height * 0.25d),
            new Point(imageSize.Width * 0.75d, imageSize.Height * 0.5d),
            new Point(imageSize.Width * 0.5d, imageSize.Height * 0.75d)
        };

        for (var i = 0; i < _pointROIs.Length; i++)
        {
            var point = points[i];
            _pointROIs[i].Rect = _bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(new Rect(point.X - 5d, point.Y - 5d, 10d, 10d));
            _pointROIs[i].IsFixed = false;
            _pointROIs[i].Text = $"{i + 1}";
        }

        Document.View.ZoomToFit();
        var output = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(_bitmapImageDrawable)
        {
            Title = $"Select four {Axis} light hole points.",
            BitmapImageROIDragMoveTypeEnum = BitmapImageROIDragMoveTypeEnum.All,
            IsDeleteEnabled = false,
            CancellationToken = cancellationToken
        }).ConfigureAwait(false);
        Guard.IsTrue(output.OutputResultModeEnum == OutputResultModeEnum.Ok);

        Points = [.. _pointROIs.Select(t => _bitmapImageDrawable.CartesianCoordinateToImageCoordinate(t.Rect).Center)];
        foreach (var roi in _pointROIs) roi.IsFixed = true;
        Document.View.ZoomToFit();
    }

    public void Reset()
    {
        ImageFilePath = string.Empty;
        Points = new Point[4];
        Center = Point.Origin;
        Radius = 0d;
        Document.Reset();
    }

    public FourierCenterChannelFlexibleApertureLightHoleItem Clone() => new(Axis, ImageFilePath, [.. Points], Center, Radius);

    public void Dispose()
    {
        _bitmapImageDrawable.Dispose();
    }
}
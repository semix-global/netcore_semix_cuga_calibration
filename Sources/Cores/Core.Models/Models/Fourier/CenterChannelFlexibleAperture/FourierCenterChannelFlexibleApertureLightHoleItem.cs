using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using MathNet.Numerics;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Enums.Editors;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.OpticsFourierImageViewer.WPF;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Editors;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;

namespace Core.Models.Models.Fourier.CenterChannelFlexibleAperture;

public sealed partial class FourierCenterChannelFlexibleApertureLightHoleItem : ObservableObject, ICloneable<FourierCenterChannelFlexibleApertureLightHoleItem>, IDisposable
{
    private const int FindItemCount = 5;
    private readonly BitmapImageDrawable _bitmapImageDrawable = new();

    public CenterChannelMotionKindEnum CenterChannelMotionKindEnum { get; }

    [ObservableProperty]
    public partial string ChannelImageFilePath { get; set; } = string.Empty;

    public FourierCenterChannelFlexibleApertureDTO.FindItem[] FindItems { get; }

    [ObservableProperty]
    public partial Circle Circle { get; private set; }

    [Newtonsoft.Json.JsonIgnore]
    public OpticsFourierImageDocument Document { get; } = new();

    public FourierCenterChannelFlexibleApertureLightHoleItem(CenterChannelMotionKindEnum centerChannelMotionKindEnum)
    {
        CenterChannelMotionKindEnum = centerChannelMotionKindEnum;

        FindItems =
        [
            .. Generate.Repeat(FindItemCount, 0d).Select(_ => new FourierCenterChannelFlexibleApertureDTO.FindItem(_bitmapImageDrawable)
            {
                BitmapImageROIDrawable = { ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.All }
            })
        ];

        Document.RunDesign(() =>
        {
            Document.ImageModel.Add(_bitmapImageDrawable);
            Document.ROIModel.AddRange([.. FindItems.Select(t => t.BitmapImageROIDrawable)]);
        });

        Document.Reset();
    }

    [Newtonsoft.Json.JsonConstructor]
    private FourierCenterChannelFlexibleApertureLightHoleItem(
        CenterChannelMotionKindEnum centerChannelMotionKindEnum,
        FourierCenterChannelFlexibleApertureDTO.FindItem[] findItems)
        : this(centerChannelMotionKindEnum)
    {
        foreach (var (index, target) in FindItems.Index()) target.AdaptIn(findItems[index]);
    }

    #region Mapper

    public FourierCenterChannelFlexibleApertureLightHoleItem Clone()
    {
        var item = new FourierCenterChannelFlexibleApertureLightHoleItem(CenterChannelMotionKindEnum)
        {
            ChannelImageFilePath = ChannelImageFilePath,
            Circle = Circle
        };

        foreach (var (index, target) in item.FindItems.Index()) target.AdaptIn(FindItems[index]);

        return item;
    }

    #endregion Mapper

    #region 校准

    public void Reset()
    {
        ChannelImageFilePath = string.Empty;

        foreach (var findItem in FindItems) findItem.Reset();

        Circle = Circle.Empty;

        Document.Reset();
    }

    public async Task CalibratingAsync(Func<IReadOnlyList<Point>, (Point CenterPosition, double Radius)> fitCircle, CancellationToken cancellationToken)
    {
        try
        {
            Document.Reset();

            Guard.IsNotNullOrWhiteSpace(ChannelImageFilePath);

            _bitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ChannelImageFilePath);

        InnerLoop:

            var rects = CreateSquareROIs(
                new Rect(Point.Origin, _bitmapImageDrawable.BitmapImage.Size),
                FindItems.Length);
            for (var i = 0; i < rects.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FindItems[i].BitmapImageROIDrawable.Rect = _bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(rects[i]);
            }

            Document.View.ZoomToFit();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var outputResult = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(_bitmapImageDrawable)
                {
                    Title = $"Adjust {CenterChannelMotionKindEnum} light hole points.",
                    BitmapImageROIDragMoveTypeEnum = BitmapImageROIDragMoveTypeEnum.All,
                    IsDeleteEnabled = false,
                    CancellationToken = cancellationToken
                }).ConfigureAwait(false);

                switch (outputResult)
                {
                    case { OutputResultModeEnum: OutputResultModeEnum.Ok }:
                        try
                        {
                            foreach (var findItem in FindItems)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                Guard.IsTrue(findItem.BitmapImageROIDrawable.IsVisible);

                                Guard.IsTrue(findItem.BitmapImageROIDrawable.Rect is { Width: > 0d, Height: > 0d });
                            }

                            goto OuterLoop;
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            switch (ShouldContinue(ex))
                            {
                                case DialogResultEnum.Yes:
                                    continue;

                                case DialogResultEnum.No:
                                    goto InnerLoop;

                                default:
                                    throw;
                            }
                        }

                    case { OutputResultModeEnum: OutputResultModeEnum.Cancel, CancelReason: CancelReasonEnum.Escape }:

                        continue;

                    case { OutputResultModeEnum: OutputResultModeEnum.Cancel, CancelReason: CancelReasonEnum.OperationCanceledException }:
                        ThrowHelper.ThrowOperationCanceledException(cancellationToken);

                        break;

                    default:
                        ThrowHelper.ThrowInvalidOperationException($"{nameof(outputResult.CancelReason)}: {outputResult.CancelReason}, {nameof(outputResult.ErrorMessage)}: {outputResult.ErrorMessage}");

                        break;
                }
            }

        OuterLoop:

            foreach (var findItem in FindItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                findItem.ImageROI = _bitmapImageDrawable.CartesianCoordinateToImageCoordinate(findItem.BitmapImageROIDrawable.Rect);
                findItem.BitmapImageROIDrawable.IsFixed = true;
            }

            var (resultCenter, resultRadius) = fitCircle([.. FindItems.Select(t => t.ImageROI.Center)]);

            Circle = new Circle(resultCenter, resultRadius);
        }
        finally
        {
            Document.View.ZoomToFit();
        }

        return;

        static DialogResultEnum ShouldContinue(Exception ex)
        {
            var dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();

            dialogWindowProvider.TryShowDialog($"""
                                                Error: {ex.Message}
                                                Yes: continue to modify ROI.
                                                No: restart from the beginning.
                                                Cancel: abort calibration.
                                                """, out var dialogResult, DialogButtonsEnum.YesNoCancel, DialogIconEnum.Warning);

            return dialogResult;
        }
    }

    /// <summary>
    /// 在边界中心的参考圆周上等分布置正方形ROI, 中心取最近整数坐标后统一计算最大可用偶数边长.
    /// </summary>
    /// <param name="bounds">布局边界, 坐标必须有限且宽高为正数, 支持非零原点.</param>
    /// <param name="count">ROI数量, 必须大于零.</param>
    /// <returns>与边界处于同一坐标系的矩形数组. 所有ROI尺寸相同且内部不重叠, 允许边缘接触. 整数中心仅近似圆周等分.</returns>
    /// <exception cref="InvalidOperationException">取整后的中心无法容纳至少两个像素边长的ROI.</exception>
    private static Rect[] CreateSquareROIs(Rect bounds, int count)
    {
        var center = new Point(bounds.XMin + bounds.Width / 2d, bounds.YMin + bounds.Height / 2d);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.25d;
        var points = new Point[count];
        var angleStep = 2d * Math.PI / points.Length;
        for (var i = 0; i < points.Length; i++)
        {
            var angle = -Math.PI / 2d + i * angleStep;
            points[i] = new Point(
                Math.Round(center.X + radius * Math.Cos(angle), MidpointRounding.AwayFromZero),
                Math.Round(center.Y + radius * Math.Sin(angle), MidpointRounding.AwayFromZero));
        }

        var rectLength = double.MaxValue;
        for (var i = 0; i < points.Length; i++)
        {
            // 统一限制边长, 保证所有矩形均在边界内且不需要单独裁剪.
            var point = points[i];
            var boundaryDistance = Math.Min(
                Math.Min(point.X - bounds.XMin, bounds.XMax - point.X),
                Math.Min(point.Y - bounds.YMin, bounds.YMax - point.Y));
            rectLength = Math.Min(rectLength, 2d * boundaryDistance);

            for (var j = i + 1; j < points.Length; j++)
            {
                var dx = Math.Abs(points[i].X - points[j].X);
                var dy = Math.Abs(points[i].Y - points[j].Y);

                // 任一坐标方向的间距不小于边长即可避免内部重叠, 允许边缘接触.
                rectLength = Math.Min(rectLength, Math.Max(dx, dy));
            }
        }

        // 使用偶数像素边长, 保持整数中心及四条边的位置, 并确保所有ROI尺寸一致.
        var halfLength = Math.Floor(rectLength / 2d);
        if (halfLength < 1d)
            throw new InvalidOperationException("The layout cannot accommodate non-overlapping ROIs with a minimum side length of two pixels.");

        rectLength = halfLength * 2d;
        var rects = new Rect[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            rects[i] = new Rect(
                points[i].X - halfLength,
                points[i].Y - halfLength,
                rectLength,
                rectLength);
        }

        return rects;
    }

    public void Review()
    {
        try
        {
            Document.Reset();

            if (string.IsNullOrWhiteSpace(ChannelImageFilePath)) return;

            _bitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ChannelImageFilePath);

            foreach (var findItem in FindItems)
            {
                findItem.BitmapImageROIDrawable.IsFixed = true;
                findItem.BitmapImageROIDrawable.Rect = _bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(findItem.ImageROI);
            }
        }
        finally
        {
            Document.View.ZoomToFit();
        }
    }

    #endregion 校准

    public object ToImageHtmlAnonymous() => new
    {
        FindItemCount,
        CenterChannelMotionKindEnum,
        ChannelImageFilePath,
        Image = new HtmlImage(ChannelImageFilePath)
    };

    public object ToHtmlAnonymous() => new
    {
        FindItemCount,
        CenterChannelMotionKindEnum,
        ChannelImageFilePath,
        Image = new HtmlImage(ChannelImageFilePath, htmlImageOverlays:
        [
            .. FindItems.Select(t => new HtmlImageRectangleOverlay(t.ImageROI)),
            .. FindItems.Select(t => new HtmlImageCrossOverlay(t.ImageROI.Center))
        ]),
        Circle
    };

    public void Dispose()
    {
        _bitmapImageDrawable.Dispose();
    }
}
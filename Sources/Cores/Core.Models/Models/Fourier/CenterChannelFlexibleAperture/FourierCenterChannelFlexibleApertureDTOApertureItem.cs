using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Fourier.PupilCameraAlignment;
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

public sealed partial class FourierCenterChannelFlexibleApertureDTOApertureItem : ObservableObject, ICloneable<FourierCenterChannelFlexibleApertureDTOApertureItem>, IDisposable
{
    private const int FindItemCount = 8;
    private readonly BitmapImageDrawable _bitmapImageDrawable = new();

    public CenterChannelMotionKindEnum CenterChannelMotionKindEnum { get; }

    [ObservableProperty]
    public partial string ChannelImageFilePath { get; set; } = string.Empty;

    public FourierPupilCameraAlignmentDTO.ROI[] FindItems { get; }

    public FourierCenterChannelFlexibleApertureDTO.Aperture Aperture { get; }

    [Newtonsoft.Json.JsonIgnore]
    public OpticsFourierImageDocument Document { get; } = new();

    public FourierCenterChannelFlexibleApertureDTOApertureItem(CenterChannelMotionKindEnum centerChannelMotionKindEnum)
    {
        CenterChannelMotionKindEnum = centerChannelMotionKindEnum;

        FindItems =
        [
            .. Generate.Repeat(FindItemCount, 0d).Select(_ => new FourierPupilCameraAlignmentDTO.ROI(_bitmapImageDrawable)
            {
                BitmapImageROIDrawable = { IsShowCrossLine = true, ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.All }
            })
        ];
        Aperture = new FourierCenterChannelFlexibleApertureDTO.Aperture(_bitmapImageDrawable);

        Document.RunDesign(() =>
        {
            Document.ImageModel.Add(_bitmapImageDrawable);
            Document.ROIModel.AddRange([.. FindItems.Select(t => t.BitmapImageROIDrawable)]);
            Document.CircleModel.Add(Aperture.BitmapImageCircleDrawable);
        });

        Document.Reset();
    }

    [Newtonsoft.Json.JsonConstructor]
    private FourierCenterChannelFlexibleApertureDTOApertureItem(
        CenterChannelMotionKindEnum centerChannelMotionKindEnum,
        FourierPupilCameraAlignmentDTO.ROI[] findItems,
        FourierCenterChannelFlexibleApertureDTO.Aperture aperture)
        : this(centerChannelMotionKindEnum)
    {
        foreach (var (index, target) in FindItems.Index()) target.AdaptIn(findItems[index]);

        Aperture.AdaptIn(aperture);
    }

    #region Mapper

    public FourierCenterChannelFlexibleApertureDTOApertureItem Clone()
    {
        var item = new FourierCenterChannelFlexibleApertureDTOApertureItem(CenterChannelMotionKindEnum)
        {
            ChannelImageFilePath = ChannelImageFilePath
        };

        item.Aperture.AdaptIn(Aperture);
        foreach (var (index, target) in item.FindItems.Index()) target.AdaptIn(FindItems[index]);

        return item;
    }

    #endregion Mapper

    #region 校准

    public void Reset()
    {
        ChannelImageFilePath = string.Empty;

        foreach (var findItem in FindItems) findItem.Reset();
        Aperture.Reset();

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

            var rects = CreateSquareROIs(new Rect(Point.Origin, _bitmapImageDrawable.BitmapImage.Size), FindItems.Length);
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
                    Title = $"{CenterChannelMotionKindEnum} aperture - angle 60 degrees, position 0: place all {FindItemCount} ROI centers on distinct points spread along the curved aperture edge. Avoid straight edges. Press Enter to fit the circle.",
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

            Aperture.BitmapImageCircleDrawable.Circle = new Circle(resultCenter, resultRadius);
            Aperture.ImageCircle = _bitmapImageDrawable.CartesianCoordinateToImageCoordinate(Aperture.BitmapImageCircleDrawable.Circle);
            Guard.IsEqualTo(_bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(Aperture.ImageCircle), Aperture.BitmapImageCircleDrawable.Circle);
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

            Aperture.BitmapImageCircleDrawable.Circle = _bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(Aperture.ImageCircle);
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
        FindItems = new HtmlTable([.. FindItems.Select(t => new { t.ImageROI })]),
        Aperture.ImageCircle,
        Image = new HtmlImage(ChannelImageFilePath, htmlImageOverlays:
        [
            .. FindItems.Select(t => new HtmlImageRectangleOverlay(t.ImageROI)),
            .. FindItems.Select(t => new HtmlImageCrossOverlay(t.ImageROI.Center))
        ])
    };

    public void Dispose()
    {
        _bitmapImageDrawable.Dispose();
    }

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

        var halfLength = Math.Floor(rectLength / 2d);
        if (halfLength < 1d) ThrowHelper.ThrowInvalidOperationException("The layout cannot accommodate non-overlapping ROIs with a minimum side length of two pixels.");

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
}
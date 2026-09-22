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
    private const string StepsComment = "At 60 degree, pick 5 light-hole points and fit a circle.";
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

            var imageSize = _bitmapImageDrawable.BitmapImage.Size;
            var center = new Point(
                imageSize.Width / 2d,
                imageSize.Height / 2d);
            var radius = Math.Min(imageSize.Width, imageSize.Height) * 0.25d;

            var points = new Point[FindItemCount];
            for (var i = 0; i < points.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var angle = -Math.PI / 2d + i * 2d * Math.PI / points.Length;

                points[i] = new Point(
                    center.X + radius * Math.Cos(angle),
                    center.Y + radius * Math.Sin(angle));
            }

            var rectLength = double.MaxValue;
            for (var i = 0; i < FindItemCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (var j = i + 1; j < points.Length; j++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var dx = Math.Abs(points[i].X - points[j].X);
                    var dy = Math.Abs(points[i].Y - points[j].Y);

                    rectLength = Math.Min(rectLength, Math.Max(dx, dy));
                }
            }

            var rects = new Rect[points.Length];
            for (var i = 0; i < points.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                rects[i] = new Rect(
                        points[i].X - rectLength / 2d,
                        points[i].Y - rectLength / 2d,
                        rectLength,
                        rectLength)
                    .ClampToBounds(new Rect(Point.Origin, imageSize))
                    .ImageCoordinateRound();
            }

            foreach (var (index, rect) in rects.Index())
            {
                FindItems[index].BitmapImageROIDrawable.Rect = _bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(rect);
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
        Comment = new HtmlComment(StepsComment),
        FindItemCount,
        CenterChannelMotionKindEnum,
        ChannelImageFilePath,
        Image = new HtmlImage(ChannelImageFilePath)
    };

    public object ToHtmlAnonymous() => new
    {
        Comment = new HtmlComment(StepsComment),
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
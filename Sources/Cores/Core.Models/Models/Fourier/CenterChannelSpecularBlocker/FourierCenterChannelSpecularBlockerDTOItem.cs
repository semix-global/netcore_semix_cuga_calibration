using System.IO;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Fourier.CenterChannelFlexibleAperture;
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
using Point = Net.Utilities.Models.Geometries.Point;

namespace Core.Models.Models.Fourier.CenterChannelSpecularBlocker;

public sealed partial class FourierCenterChannelSpecularBlockerDTOItem : ObservableObject, ICloneable<FourierCenterChannelSpecularBlockerDTOItem>, IDisposable
{
    private const string StepsComment = "Step0: Move Y rod and X reflection push home; Step1: move them to the calibrated positions. Grab Fourier images during calibration and CIB images during verification";

    private readonly BitmapImageDrawable _step0FourierBitmapImageDrawable = new();
    private readonly BitmapImageDrawable _step1FourierBitmapImageDrawable = new();
    private readonly BitmapImageROIDrawable _step1YRodBitmapImageROIDrawable;
    private readonly BitmapImageROIDrawable _step1XReflectionPushBitmapImageROIDrawable;

    private readonly BitmapImageDrawable _step0CIBBitmapImageDrawable = new();
    private readonly BitmapImageDrawable _step1CIBBitmapImageDrawable = new();

    [ObservableProperty]
    public partial string Step0FourierImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Step1FourierImageFilePath { get; set; } = string.Empty;

    public FourierCenterChannelSpecularBlockerDTO.ROI YRodROI { get; }

    public FourierCenterChannelSpecularBlockerDTO.ROI XReflectionPushROI { get; }

    [ObservableProperty]
    public partial string RawStep0CIBImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RawStep1CIBImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Step0CIBImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Step1CIBImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double Step0CIBImageAverageValue { get; set; }

    [ObservableProperty]
    public partial double Step1CIBImageAverageValue { get; set; }

    [ObservableProperty]
    public partial double ExtinctionRatio { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public OpticsFourierImageDocument Document { get; } = new();

    [Newtonsoft.Json.JsonIgnore]
    public OpticsFourierImageDocument CIBDocument { get; } = new();

    public FourierCenterChannelSpecularBlockerDTOItem()
    {
        YRodROI = new FourierCenterChannelSpecularBlockerDTO.ROI(_step0FourierBitmapImageDrawable)
        {
            BitmapImageROIDrawable = { ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.None }
        };
        XReflectionPushROI = new FourierCenterChannelSpecularBlockerDTO.ROI(_step0FourierBitmapImageDrawable)
        {
            BitmapImageROIDrawable = { ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.None }
        };
        _step1YRodBitmapImageROIDrawable = new BitmapImageROIDrawable(_step1FourierBitmapImageDrawable);
        _step1XReflectionPushBitmapImageROIDrawable = new BitmapImageROIDrawable(_step1FourierBitmapImageDrawable);

        Document.RunDesign(() =>
        {
            Document.ImageModel.AddRange([_step0FourierBitmapImageDrawable, _step1FourierBitmapImageDrawable]);
            Document.ROIModel.AddRange([YRodROI.BitmapImageROIDrawable, XReflectionPushROI.BitmapImageROIDrawable, _step1YRodBitmapImageROIDrawable, _step1XReflectionPushBitmapImageROIDrawable]);
        });

        CIBDocument.RunDesign(() => CIBDocument.ImageModel.AddRange([_step0CIBBitmapImageDrawable, _step1CIBBitmapImageDrawable]));

        Document.Reset();
        CIBDocument.Reset();
    }

    [Newtonsoft.Json.JsonConstructor]
    private FourierCenterChannelSpecularBlockerDTOItem(
        FourierCenterChannelSpecularBlockerDTO.ROI yRodROI,
        FourierCenterChannelSpecularBlockerDTO.ROI xReflectionPushROI)
        : this()
    {
        YRodROI.AdaptIn(yRodROI);
        XReflectionPushROI.AdaptIn(xReflectionPushROI);
    }

    #region Mapper

#pragma warning disable IDISP003

    public FourierCenterChannelSpecularBlockerDTOItem Clone()
    {
        var item = new FourierCenterChannelSpecularBlockerDTOItem
        {
            Step0FourierImageFilePath = Step0FourierImageFilePath,
            Step1FourierImageFilePath = Step1FourierImageFilePath,
            RawStep0CIBImageFilePath = RawStep0CIBImageFilePath,
            RawStep1CIBImageFilePath = RawStep1CIBImageFilePath,
            Step0CIBImageFilePath = Step0CIBImageFilePath,
            Step1CIBImageFilePath = Step1CIBImageFilePath,
            Step0CIBImageAverageValue = Step0CIBImageAverageValue,
            Step1CIBImageAverageValue = Step1CIBImageAverageValue,
            ExtinctionRatio = ExtinctionRatio
        };

        item.YRodROI.AdaptIn(YRodROI);
        item.XReflectionPushROI.AdaptIn(XReflectionPushROI);

        return item;
    }

#pragma warning restore IDISP003

    #endregion Mapper

    #region 校准

    public void Reset()
    {
        Step0FourierImageFilePath = string.Empty;
        Step1FourierImageFilePath = string.Empty;

        YRodROI.Reset();
        XReflectionPushROI.Reset();

        Document.Reset();

        ResetCIB();
    }

    public void ResetCIB()
    {
        RawStep0CIBImageFilePath = string.Empty;
        RawStep1CIBImageFilePath = string.Empty;
        Step0CIBImageFilePath = string.Empty;
        Step1CIBImageFilePath = string.Empty;
        Step0CIBImageAverageValue = 0d;
        Step1CIBImageAverageValue = 0d;
        ExtinctionRatio = 0d;

        CIBDocument.Reset();
    }

    public async Task CalibratingAsync(
        FourierCenterChannelFlexibleApertureDTOItem? xReflectionPushItem,
        FourierCenterChannelFlexibleApertureDTOItem? yRodItem,
        (double Min, double Max) xReflectionPushMotorRange,
        (double Min, double Max) yMotorRange,
        CancellationToken cancellationToken)
    {
        Guard.IsTrue(xReflectionPushItem is not null || yRodItem is not null, "Select at least one X reflection push or Y rod.");

        try
        {
            Document.Reset();

            Guard.IsNotNullOrWhiteSpace(Step0FourierImageFilePath);

            _step0FourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step0FourierImageFilePath);
            Document.View.ZoomToFit();

            if (xReflectionPushItem is not null)
            {
                Guard.IsTrue(xReflectionPushItem.CenterChannelMotionKindEnum is CenterChannelMotionKindEnum.XReflectionCoarsePush or CenterChannelMotionKindEnum.XReflectionFinePush);
                Guard.IsTrue(double.IsFinite(xReflectionPushItem.PixelSize) && xReflectionPushItem.PixelSize != 0d);

                XReflectionPushROI.BitmapImageROIDrawable.Rect = _step0FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(xReflectionPushItem.Step0ROI.ImageROI);

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var outputResult = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(_step0FourierBitmapImageDrawable)
                    {
                        Title = "Adjust x-reflection-push on the Channel3 Fourier image.",
                        BitmapImageROIDragMoveTypeEnum = BitmapImageROIDragMoveTypeEnum.X,
                        IsOutsideImageAllowed = true,
                        IsDeleteEnabled = false,
                        CancellationToken = cancellationToken
                    });

                    switch (outputResult)
                    {
                        case { OutputResultModeEnum: OutputResultModeEnum.Ok }:
                            goto OuterLoop0;

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

            OuterLoop0:

                XReflectionPushROI.BitmapImageROIDrawable.IsFixed = true;
                XReflectionPushROI.ImageROI = _step0FourierBitmapImageDrawable.CartesianCoordinateToImageCoordinate(XReflectionPushROI.BitmapImageROIDrawable.Rect);
                XReflectionPushROI.AngleMotorAbsoluteValue = xReflectionPushItem.AngleMotorAbsoluteValue;
                XReflectionPushROI.MotorAbsoluteValue = Math.Clamp(
                    xReflectionPushItem.Step0MotorAbsoluteValue + (XReflectionPushROI.ImageROI.Center - xReflectionPushItem.Step0ROI.ImageROI.Center).X * xReflectionPushItem.PixelSize,
                    xReflectionPushMotorRange.Min,
                    xReflectionPushMotorRange.Max);

                // 根据限位后的电机位置反算图像中心, 保持 ROI 尺寸并同步显示.
                var xReflectionPushCenterX = xReflectionPushItem.Step0ROI.ImageROI.Center.X + (XReflectionPushROI.MotorAbsoluteValue - xReflectionPushItem.Step0MotorAbsoluteValue) / xReflectionPushItem.PixelSize;
                XReflectionPushROI.ImageROI = (XReflectionPushROI.ImageROI + new Vector(xReflectionPushCenterX - XReflectionPushROI.ImageROI.Center.X, 0d)).ImageCoordinateRound();
                XReflectionPushROI.BitmapImageROIDrawable.Rect = _step0FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(XReflectionPushROI.ImageROI);
                XReflectionPushROI.IsSelected = true;
            }
            else
            {
                XReflectionPushROI.BitmapImageROIDrawable.IsVisible = false;
                XReflectionPushROI.ImageROI = Rect.Empty;
                XReflectionPushROI.AngleMotorAbsoluteValue = 0d;
                XReflectionPushROI.MotorAbsoluteValue = 0d;
                XReflectionPushROI.IsSelected = false;
            }

            if (yRodItem is not null)
            {
                Guard.IsTrue(yRodItem.CenterChannelMotionKindEnum == CenterChannelMotionKindEnum.YRod);
                Guard.IsTrue(double.IsFinite(yRodItem.PixelSize) && yRodItem.PixelSize != 0d);

                YRodROI.BitmapImageROIDrawable.Rect = _step0FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(yRodItem.Step0ROI.ImageROI);

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var outputResult = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(_step0FourierBitmapImageDrawable)
                    {
                        Title = "Adjust y-rod on the Channel3 Fourier image.",
                        BitmapImageROIDragMoveTypeEnum = BitmapImageROIDragMoveTypeEnum.X,
                        IsOutsideImageAllowed = true,
                        IsDeleteEnabled = false,
                        CancellationToken = cancellationToken
                    });

                    switch (outputResult)
                    {
                        case { OutputResultModeEnum: OutputResultModeEnum.Ok }:
                            goto OuterLoop1;

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

            OuterLoop1:

                YRodROI.BitmapImageROIDrawable.IsFixed = true;
                YRodROI.ImageROI = _step0FourierBitmapImageDrawable.CartesianCoordinateToImageCoordinate(YRodROI.BitmapImageROIDrawable.Rect);
                YRodROI.AngleMotorAbsoluteValue = yRodItem.AngleMotorAbsoluteValue;
                YRodROI.MotorAbsoluteValue = Math.Clamp(
                    yRodItem.Step0MotorAbsoluteValue + (YRodROI.ImageROI.Center - yRodItem.Step0ROI.ImageROI.Center).X * yRodItem.PixelSize,
                    yMotorRange.Min,
                    yMotorRange.Max);

                // Y 方向同样使用带符号的像素尺寸反算, 避免显示位置超出电机限位.
                var yRodCenterX = yRodItem.Step0ROI.ImageROI.Center.X + (YRodROI.MotorAbsoluteValue - yRodItem.Step0MotorAbsoluteValue) / yRodItem.PixelSize;
                YRodROI.ImageROI = (YRodROI.ImageROI + new Vector(yRodCenterX - YRodROI.ImageROI.Center.X, 0d)).ImageCoordinateRound();
                YRodROI.BitmapImageROIDrawable.Rect = _step0FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(YRodROI.ImageROI);
                YRodROI.IsSelected = true;
            }
            else
            {
                YRodROI.BitmapImageROIDrawable.IsVisible = false;
                YRodROI.ImageROI = Rect.Empty;
                YRodROI.AngleMotorAbsoluteValue = 0d;
                YRodROI.MotorAbsoluteValue = 0d;
                YRodROI.IsSelected = false;
            }
        }
        finally
        {
            Document.View.ZoomToFit();
        }
    }

    public void Review()
    {
        try
        {
            Document.Reset();

            if (string.IsNullOrWhiteSpace(Step0FourierImageFilePath) || string.IsNullOrWhiteSpace(Step1FourierImageFilePath)) return;

            _step0FourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step0FourierImageFilePath);

            _step1FourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step1FourierImageFilePath);
            _step1FourierBitmapImageDrawable.Point = _step0FourierBitmapImageDrawable.Point - new Vector(0d, _step0FourierBitmapImageDrawable.BitmapImage.Height + 10d);

            YRodROI.BitmapImageROIDrawable.IsFixed = _step1YRodBitmapImageROIDrawable.IsFixed = true;
            YRodROI.BitmapImageROIDrawable.Rect = _step0FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(YRodROI.ImageROI);
            _step1YRodBitmapImageROIDrawable.Rect = _step1FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(YRodROI.ImageROI);
            YRodROI.BitmapImageROIDrawable.IsVisible = _step1YRodBitmapImageROIDrawable.IsVisible = YRodROI.IsSelected;

            XReflectionPushROI.BitmapImageROIDrawable.IsFixed = _step1XReflectionPushBitmapImageROIDrawable.IsFixed = true;
            XReflectionPushROI.BitmapImageROIDrawable.Rect = _step0FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(XReflectionPushROI.ImageROI);
            _step1XReflectionPushBitmapImageROIDrawable.Rect = _step1FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(XReflectionPushROI.ImageROI);
            XReflectionPushROI.BitmapImageROIDrawable.IsVisible = _step1XReflectionPushBitmapImageROIDrawable.IsVisible = XReflectionPushROI.IsSelected;
        }
        finally
        {
            Document.View.ZoomToFit();
        }
    }

    public void ReviewCIB()
    {
        try
        {
            CIBDocument.Reset();

            if (string.IsNullOrWhiteSpace(Step0CIBImageFilePath)) return;

            _step0CIBBitmapImageDrawable.Point = Point.Origin;
            _step0CIBBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(File.Exists(RawStep0CIBImageFilePath) ? RawStep0CIBImageFilePath : Step0CIBImageFilePath);

            if (string.IsNullOrWhiteSpace(Step1CIBImageFilePath)) return;

            _step1CIBBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(File.Exists(RawStep1CIBImageFilePath) ? RawStep1CIBImageFilePath : Step1CIBImageFilePath);
            _step1CIBBitmapImageDrawable.Point = _step0CIBBitmapImageDrawable.Point - new Vector(0d, _step0CIBBitmapImageDrawable.BitmapImage.Height + 10d);
        }
        finally
        {
            CIBDocument.View.ZoomToFit();
        }
    }

    #endregion 校准

    public object ToImageHtmlAnonymous() => new
    {
        Comment = new HtmlComment(StepsComment),
        Step0FourierImageFilePath,
        Step0FourierImage = new HtmlImage(Step0FourierImageFilePath)
    };

    public object ToHtmlAnonymous() => new
    {
        Comment = new HtmlComment(StepsComment),
        Step0FourierImageFilePath,
        Step1FourierImageFilePath,
        ROIs = new HtmlTable([
            new { Name = "X Reflection Push", XReflectionPushROI.ImageROI, XReflectionPushROI.AngleMotorAbsoluteValue, XReflectionPushROI.MotorAbsoluteValue, XReflectionPushROI.IsSelected },
            new { Name = "Y Rod", YRodROI.ImageROI, YRodROI.AngleMotorAbsoluteValue, YRodROI.MotorAbsoluteValue, YRodROI.IsSelected }
        ]),
        Step0FourierImage = new HtmlImage(Step0FourierImageFilePath, htmlImageOverlays:
        [
            new HtmlImageRectangleOverlay(XReflectionPushROI.ImageROI),
            new HtmlImageRectangleOverlay(YRodROI.ImageROI)
        ]),
        Step1FourierImage = new HtmlImage(Step1FourierImageFilePath, htmlImageOverlays:
        [
            new HtmlImageRectangleOverlay(XReflectionPushROI.ImageROI),
            new HtmlImageRectangleOverlay(YRodROI.ImageROI)
        ])
    };

    public object ToCIBHtmlAnonymous() => new
    {
        Comment = new HtmlComment(StepsComment),
        RawStep0CIBImageFilePath,
        RawStep1CIBImageFilePath,
        Step0CIBImageFilePath,
        Step1CIBImageFilePath,
        Step0CIBImageAverageValue,
        Step1CIBImageAverageValue,
        ExtinctionRatio,
        Step0CIBImage = new HtmlImage(Step0CIBImageFilePath),
        Step1CIBImage = new HtmlImage(Step1CIBImageFilePath)
    };

    public void Dispose()
    {
        _step0FourierBitmapImageDrawable.Dispose();
        _step1FourierBitmapImageDrawable.Dispose();
        _step0CIBBitmapImageDrawable.Dispose();
        _step1CIBBitmapImageDrawable.Dispose();
    }
}
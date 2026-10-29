using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Fourier.PupilCameraAlignment;
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

namespace Core.Models.Models.Fourier.CenterChannelFlexibleAperture;

public sealed partial class FourierCenterChannelFlexibleApertureDTOOriginPositionItem : ObservableObject, ICloneable<FourierCenterChannelFlexibleApertureDTOOriginPositionItem>, IDisposable
{
    private const string StepsComment = "Step0: home X and move the origin ROI to its desired position. Step1: move X to the calibrated origin angle and calculated position, then grab an image.";

    private readonly BitmapImageDrawable _step0FourierBitmapImageDrawable = new();
    private readonly BitmapImageDrawable _step1FourierBitmapImageDrawable = new();
    private readonly BitmapImageROIDrawable _step1XOriginBitmapImageROIDrawable;

    [ObservableProperty]
    public partial double AngleMotorAbsoluteValue { get; private set; }

    [ObservableProperty]
    public partial double MotorAbsoluteValue { get; private set; }

    [ObservableProperty]
    public partial string Step0FourierImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Step1FourierImageFilePath { get; set; } = string.Empty;

    public FourierPupilCameraAlignmentDTO.ROI XOriginROI { get; }

    [Newtonsoft.Json.JsonIgnore]
    public OpticsFourierImageDocument Document { get; } = new();

    public FourierCenterChannelFlexibleApertureDTOOriginPositionItem()
    {
        XOriginROI = new FourierPupilCameraAlignmentDTO.ROI(_step0FourierBitmapImageDrawable)
        {
            BitmapImageROIDrawable = { ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.None }
        };
        _step1XOriginBitmapImageROIDrawable = new BitmapImageROIDrawable(_step1FourierBitmapImageDrawable);

        Document.RunDesign(() =>
        {
            Document.ImageModel.AddRange([_step0FourierBitmapImageDrawable, _step1FourierBitmapImageDrawable]);
            Document.ROIModel.AddRange([XOriginROI.BitmapImageROIDrawable, _step1XOriginBitmapImageROIDrawable]);
        });

        Document.Reset();
    }

    [Newtonsoft.Json.JsonConstructor]
    private FourierCenterChannelFlexibleApertureDTOOriginPositionItem(FourierPupilCameraAlignmentDTO.ROI xOriginROI)
        : this()
    {
        XOriginROI.AdaptIn(xOriginROI);
    }

    #region Mapper

    public FourierCenterChannelFlexibleApertureDTOOriginPositionItem Clone()
    {
        var item = new FourierCenterChannelFlexibleApertureDTOOriginPositionItem
        {
            AngleMotorAbsoluteValue = AngleMotorAbsoluteValue,
            MotorAbsoluteValue = MotorAbsoluteValue,
            Step0FourierImageFilePath = Step0FourierImageFilePath,
            Step1FourierImageFilePath = Step1FourierImageFilePath
        };

        item.XOriginROI.AdaptIn(XOriginROI);

        return item;
    }

    #endregion Mapper

    #region 校准

    public void Reset()
    {
        MotorAbsoluteValue = 0d;
        Step0FourierImageFilePath = string.Empty;
        Step1FourierImageFilePath = string.Empty;

        XOriginROI.Reset();

        Document.Reset();
    }

    public void UpdatePosition(FourierCenterChannelFlexibleApertureDTOItem xOriginItem, (double Min, double Max) xMotorRange)
    {
        Guard.IsTrue(xOriginItem.CenterChannelMotionKindEnum == CenterChannelMotionKindEnum.XRod && xOriginItem.DefaultAngleMotorAbsoluteValue == 0d);
        Guard.IsTrue(double.IsFinite(xOriginItem.PixelSize) && xOriginItem.PixelSize != 0d);
        Guard.IsTrue(double.IsFinite(xOriginItem.Step0MotorAbsoluteValue));
        Guard.IsTrue(double.IsFinite(xOriginItem.AngleMotorAbsoluteValue));
        Guard.IsTrue(xOriginItem.Step0ROI.ImageROI is { Width: > 0d, Height: > 0d });
        Guard.IsTrue(XOriginROI.ImageROI is { Width: > 0d, Height: > 0d });
        Guard.IsTrue(double.IsFinite(xMotorRange.Min) && double.IsFinite(xMotorRange.Max));
        Guard.IsGreaterThan(xMotorRange.Max, xMotorRange.Min);

        var motorAbsoluteValue = xOriginItem.Step0MotorAbsoluteValue + (XOriginROI.ImageROI.Center - xOriginItem.Step0ROI.ImageROI.Center).X * xOriginItem.PixelSize;
        Guard.IsTrue(double.IsFinite(motorAbsoluteValue));

        AngleMotorAbsoluteValue = xOriginItem.AngleMotorAbsoluteValue;
        MotorAbsoluteValue = Math.Clamp(motorAbsoluteValue, xMotorRange.Min, xMotorRange.Max);

        // 限位后反算 ROI 中心, 保持显示位置与实际电机位置一致.
        var xOriginCenterX = xOriginItem.Step0ROI.ImageROI.Center.X + (MotorAbsoluteValue - xOriginItem.Step0MotorAbsoluteValue) / xOriginItem.PixelSize;
        XOriginROI.ImageROI = (XOriginROI.ImageROI + new Vector(xOriginCenterX - XOriginROI.ImageROI.Center.X, 0d)).ImageCoordinateRound();
    }

    public async Task CalibratingAsync(
        FourierCenterChannelFlexibleApertureDTOItem xOriginItem,
        (double Min, double Max) xMotorRange,
        CancellationToken cancellationToken)
    {
        Guard.IsTrue(double.IsFinite(xOriginItem.PixelSize) && xOriginItem.PixelSize != 0d);
        Guard.IsTrue(double.IsFinite(xOriginItem.AngleMotorAbsoluteValue));

        AngleMotorAbsoluteValue = xOriginItem.AngleMotorAbsoluteValue;

        try
        {
            Document.Reset();

            Guard.IsNotNullOrWhiteSpace(Step0FourierImageFilePath);

            _step0FourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step0FourierImageFilePath);
            XOriginROI.BitmapImageROIDrawable.Rect = _step0FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(xOriginItem.Step0ROI.ImageROI);
            Document.View.ZoomToFit();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var outputResult = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(_step0FourierBitmapImageDrawable)
                {
                    Title = "Move the X origin ROI to its desired position.",
                    BitmapImageROIDragMoveTypeEnum = BitmapImageROIDragMoveTypeEnum.X,
                    IsOutsideImageAllowed = true,
                    IsDeleteEnabled = false,
                    CancellationToken = cancellationToken
                });

                switch (outputResult)
                {
                    case { OutputResultModeEnum: OutputResultModeEnum.Ok }:
                        goto OuterLoop;

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

            XOriginROI.ImageROI = _step0FourierBitmapImageDrawable.CartesianCoordinateToImageCoordinate(XOriginROI.BitmapImageROIDrawable.Rect);
            UpdatePosition(xOriginItem, xMotorRange);

            XOriginROI.BitmapImageROIDrawable.Rect = _step0FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(XOriginROI.ImageROI);
            XOriginROI.BitmapImageROIDrawable.IsFixed = true;
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

            XOriginROI.BitmapImageROIDrawable.IsFixed = _step1XOriginBitmapImageROIDrawable.IsFixed = true;
            XOriginROI.BitmapImageROIDrawable.Rect = _step0FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(XOriginROI.ImageROI);
            _step1XOriginBitmapImageROIDrawable.Rect = _step1FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(XOriginROI.ImageROI);
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
        AngleMotorAbsoluteValue,
        MotorAbsoluteValue,
        Step0FourierImageFilePath,
        Step1FourierImageFilePath,
        Step0FourierImage = new HtmlImage(Step0FourierImageFilePath),
        Step1FourierImage = new HtmlImage(Step1FourierImageFilePath)
    };

    public object ToHtmlAnonymous() => new
    {
        Comment = new HtmlComment(StepsComment),
        AngleMotorAbsoluteValue,
        MotorAbsoluteValue,
        Step0FourierImageFilePath,
        Step1FourierImageFilePath,
        XOriginROI = XOriginROI.ImageROI,
        Step0FourierImage = new HtmlImage(Step0FourierImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(XOriginROI.ImageROI)]),
        Step1FourierImage = new HtmlImage(Step1FourierImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(XOriginROI.ImageROI)])
    };

    public void Dispose()
    {
        _step0FourierBitmapImageDrawable.Dispose();
        _step1FourierBitmapImageDrawable.Dispose();
    }
}
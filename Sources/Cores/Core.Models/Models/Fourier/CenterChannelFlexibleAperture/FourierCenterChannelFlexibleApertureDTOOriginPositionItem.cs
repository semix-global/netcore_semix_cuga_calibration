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
    public partial string Step0FourierImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Step1FourierImageFilePath { get; set; } = string.Empty;

    public FourierCenterChannelFlexibleApertureDTO.ROI XOriginROI { get; }

    [Newtonsoft.Json.JsonIgnore]
    public OpticsFourierImageDocument Document { get; } = new();

    public FourierCenterChannelFlexibleApertureDTOOriginPositionItem()
    {
        XOriginROI = new FourierCenterChannelFlexibleApertureDTO.ROI(_step0FourierBitmapImageDrawable)
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
    private FourierCenterChannelFlexibleApertureDTOOriginPositionItem(FourierPupilCameraAlignmentDTO.ROI xOriginROI) : this()
    {
        XOriginROI.AdaptIn(xOriginROI);
    }

    #region Mapper

    public FourierCenterChannelFlexibleApertureDTOOriginPositionItem Clone()
    {
        var item = new FourierCenterChannelFlexibleApertureDTOOriginPositionItem
        {
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
        Step0FourierImageFilePath = string.Empty;
        Step1FourierImageFilePath = string.Empty;

        XOriginROI.Reset();

        Document.Reset();
    }

    public async Task CalibratingAsync(
        FourierCenterChannelFlexibleApertureDTOItem xOriginItem,
        (double Min, double Max) xMotorRange,
        CancellationToken cancellationToken)
    {
        try
        {
            Document.Reset();

            Guard.IsNotNullOrWhiteSpace(Step0FourierImageFilePath);

            _step0FourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step0FourierImageFilePath);
            Document.View.ZoomToFit();

            Guard.IsTrue(double.IsFinite(xOriginItem.PixelSize) && xOriginItem.PixelSize != 0d);
            Guard.IsTrue(double.IsFinite(xOriginItem.AngleMotorAbsoluteValue));

            XOriginROI.BitmapImageROIDrawable.Rect = _step0FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(xOriginItem.Step0ROI.ImageROI);

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

            XOriginROI.BitmapImageROIDrawable.IsFixed = true;
            XOriginROI.ImageROI = _step0FourierBitmapImageDrawable.CartesianCoordinateToImageCoordinate(XOriginROI.BitmapImageROIDrawable.Rect);
            XOriginROI.MotorAbsoluteValue = Math.Clamp(
                xOriginItem.Step0MotorAbsoluteValue + (XOriginROI.ImageROI.Center - xOriginItem.Step0ROI.ImageROI.Center).X * xOriginItem.PixelSize,
                xMotorRange.Min,
                xMotorRange.Max);

            // 限位后反算 ROI 中心, 保持显示位置与实际电机位置一致.
            var xOriginCenterX = xOriginItem.Step0ROI.ImageROI.Center.X + (XOriginROI.MotorAbsoluteValue - xOriginItem.Step0MotorAbsoluteValue) / xOriginItem.PixelSize;
            XOriginROI.ImageROI = (XOriginROI.ImageROI + new Vector(xOriginCenterX - XOriginROI.ImageROI.Center.X, 0d)).ImageCoordinateRound();
            XOriginROI.BitmapImageROIDrawable.Rect = _step0FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(XOriginROI.ImageROI);
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
        Step0FourierImageFilePath,
        Step1FourierImageFilePath,
        Step0FourierImage = new HtmlImage(Step0FourierImageFilePath),
        Step1FourierImage = new HtmlImage(Step1FourierImageFilePath)
    };

    public object ToHtmlAnonymous() => new
    {
        Comment = new HtmlComment(StepsComment),
        Step0FourierImageFilePath,
        Step1FourierImageFilePath,
        XOrigin = new HtmlQuote(new
        {
            XOriginROI.ImageROI,
            XOriginROI.MotorAbsoluteValue,
        }),
        Step0FourierImage = new HtmlImage(Step0FourierImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(XOriginROI.ImageROI)]),
        Step1FourierImage = new HtmlImage(Step1FourierImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(XOriginROI.ImageROI)])
    };

    public void Dispose()
    {
        _step0FourierBitmapImageDrawable.Dispose();
        _step1FourierBitmapImageDrawable.Dispose();
    }
}
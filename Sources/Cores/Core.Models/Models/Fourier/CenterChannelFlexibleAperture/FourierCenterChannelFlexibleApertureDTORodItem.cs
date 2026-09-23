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
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;

namespace Core.Models.Models.Fourier.CenterChannelFlexibleAperture;

public sealed partial class FourierCenterChannelFlexibleApertureDTORodItem : ObservableObject, ICloneable<FourierCenterChannelFlexibleApertureDTORodItem>, IDisposable
{
    private const string StepsComment = "At the selected angle, Step 0 defines the rod ROI at the first motor position. X/Push: move horizontally and resize width with the left/right center handles. Y: move vertically and resize height with the top/bottom center handles. Step 1: move the same ROI onto the same rod at the second position without resizing. PixelSize = signed motor displacement / signed ROI center displacement (X for X/Push, Y for Y).";

    private readonly BitmapImageDrawable _step0BitmapImageDrawable = new();
    private readonly BitmapImageDrawable _step1BitmapImageDrawable = new();

    public CenterChannelMotionKindEnum CenterChannelMotionKindEnum { get; }

    public double DefaultAngleMotorAbsoluteValue { get; }

    [ObservableProperty]
    public partial double AngleMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double Step0MotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double Step1MotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial string Step0ChannelImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Step1ChannelImageFilePath { get; set; } = string.Empty;

    public FourierPupilCameraAlignmentDTO.ROI Step0ROI { get; }

    public FourierPupilCameraAlignmentDTO.ROI Step1ROI { get; }

    [ObservableProperty]
    public partial double PixelSize { get; private set; }

    [Newtonsoft.Json.JsonIgnore]
    public OpticsFourierImageDocument Document { get; } = new();

    public FourierCenterChannelFlexibleApertureDTORodItem(CenterChannelMotionKindEnum centerChannelMotionKindEnum, double defaultAngleMotorAbsoluteValue)
    {
        CenterChannelMotionKindEnum = centerChannelMotionKindEnum;
        DefaultAngleMotorAbsoluteValue = defaultAngleMotorAbsoluteValue;
        AngleMotorAbsoluteValue = defaultAngleMotorAbsoluteValue;

        Step0ROI = new FourierPupilCameraAlignmentDTO.ROI(_step0BitmapImageDrawable)
        {
            BitmapImageROIDrawable =
            {
                ResizeJoystickStateEnum = centerChannelMotionKindEnum switch
                {
                    CenterChannelMotionKindEnum.X or CenterChannelMotionKindEnum.Push => BitmapImageROIResizeJoystickStateEnum.XMinYCenter | BitmapImageROIResizeJoystickStateEnum.XMaxYCenter,
                    CenterChannelMotionKindEnum.Y => BitmapImageROIResizeJoystickStateEnum.XCenterYMin | BitmapImageROIResizeJoystickStateEnum.XCenterYMax,
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<BitmapImageROIResizeJoystickStateEnum>(nameof(centerChannelMotionKindEnum))
                }
            }
        };

        Step1ROI = new FourierPupilCameraAlignmentDTO.ROI(_step1BitmapImageDrawable)
        {
            BitmapImageROIDrawable = { ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.None }
        };

        Document.RunDesign(() =>
        {
            Document.ImageModel.AddRange([_step0BitmapImageDrawable, _step1BitmapImageDrawable]);
            Document.ROIModel.AddRange([Step0ROI.BitmapImageROIDrawable, Step1ROI.BitmapImageROIDrawable]);
        });

        Document.Reset();
    }

    [Newtonsoft.Json.JsonConstructor]
    private FourierCenterChannelFlexibleApertureDTORodItem(
        CenterChannelMotionKindEnum centerChannelMotionKindEnum,
        double defaultAngleMotorAbsoluteValue,
        FourierPupilCameraAlignmentDTO.ROI step0ROI,
        FourierPupilCameraAlignmentDTO.ROI step1ROI)
        : this(centerChannelMotionKindEnum, defaultAngleMotorAbsoluteValue)
    {
        Step0ROI.AdaptIn(step0ROI);
        Step1ROI.AdaptIn(step1ROI);
    }

    #region Mapper

    public FourierCenterChannelFlexibleApertureDTORodItem Clone()
    {
        var item = new FourierCenterChannelFlexibleApertureDTORodItem(CenterChannelMotionKindEnum, DefaultAngleMotorAbsoluteValue)
        {
            AngleMotorAbsoluteValue = AngleMotorAbsoluteValue,
            Step0MotorAbsoluteValue = Step0MotorAbsoluteValue,
            Step1MotorAbsoluteValue = Step1MotorAbsoluteValue,
            Step0ChannelImageFilePath = Step0ChannelImageFilePath,
            Step1ChannelImageFilePath = Step1ChannelImageFilePath,
            PixelSize = PixelSize
        };

        item.Step0ROI.AdaptIn(Step0ROI);
        item.Step1ROI.AdaptIn(Step1ROI);

        return item;
    }

    #endregion Mapper

    #region 校准

    public void Reset()
    {
        Step0ChannelImageFilePath = string.Empty;
        Step1ChannelImageFilePath = string.Empty;

        Step0ROI.Reset();
        Step1ROI.Reset();
        PixelSize = 0d;

        Document.Reset();
    }

    public async Task CalibratingAsync(CancellationToken cancellationToken)
    {
        try
        {
            Document.Reset();

            Guard.IsNotNullOrWhiteSpace(Step0ChannelImageFilePath);
            Guard.IsNotNullOrWhiteSpace(Step1ChannelImageFilePath);

            _step0BitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step0ChannelImageFilePath);

            _step1BitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step1ChannelImageFilePath);
            _step1BitmapImageDrawable.Point = _step0BitmapImageDrawable.Point - new Vector(0d, _step0BitmapImageDrawable.BitmapImage.Height + 10d);

            Document.View.ZoomToFit();

            var bitmapImageROIDragMoveTypeEnum = CenterChannelMotionKindEnum switch
            {
                CenterChannelMotionKindEnum.X or CenterChannelMotionKindEnum.Push => BitmapImageROIDragMoveTypeEnum.X,
                CenterChannelMotionKindEnum.Y => BitmapImageROIDragMoveTypeEnum.Y,
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<BitmapImageROIDragMoveTypeEnum>(nameof(CenterChannelMotionKindEnum))
            };

            await Task.Delay(1000, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            await Step0Async(bitmapImageROIDragMoveTypeEnum);

            await Task.Delay(500, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            await Step1Async(bitmapImageROIDragMoveTypeEnum);
        }
        finally
        {
            Document.View.ZoomToFit();
        }

        return;

        async Task Step0Async(BitmapImageROIDragMoveTypeEnum bitmapImageROIDragMoveTypeEnum)
        {
        InnerLoop:

            if (CenterChannelMotionKindEnum is CenterChannelMotionKindEnum.Push or CenterChannelMotionKindEnum.X)
            {
                var width = _step0BitmapImageDrawable.BitmapImage.Width / 4;
                Step0ROI.BitmapImageROIDrawable.Rect = _step0BitmapImageDrawable.ImageCoordinateToCartesianCoordinate(new Rect(_step0BitmapImageDrawable.BitmapImage.Size.Width / 2d - width / 2d, 0d, width, _step0BitmapImageDrawable.BitmapImage.Height)).ImageCoordinateRound();
            }
            else
            {
                var height = _step0BitmapImageDrawable.BitmapImage.Height / 4;
                Step0ROI.BitmapImageROIDrawable.Rect = _step0BitmapImageDrawable.ImageCoordinateToCartesianCoordinate(new Rect(0d, _step0BitmapImageDrawable.BitmapImage.Height / 2d - height / 2d, _step0BitmapImageDrawable.BitmapImage.Width, height)).ImageCoordinateRound();
            }

            Document.View.SetViewBounds(_step0BitmapImageDrawable.GetExtents().Expand(new Padding(10d)));

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var outputResult = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(_step0BitmapImageDrawable)
                {
                    Title = CenterChannelMotionKindEnum == CenterChannelMotionKindEnum.Y
                        ? $"Step 0 - {CenterChannelMotionKindEnum}, angle {AngleMotorAbsoluteValue}, position {Step0MotorAbsoluteValue}: move vertically and adjust height using the top/bottom center handles. Press Enter to lock the ROI size."
                        : $"Step 0 - {CenterChannelMotionKindEnum}, angle {AngleMotorAbsoluteValue}, position {Step0MotorAbsoluteValue}: move horizontally and adjust width using the left/right center handles. Press Enter to lock the ROI size.",
                    BitmapImageROIDragMoveTypeEnum = bitmapImageROIDragMoveTypeEnum,
                    IsDeleteEnabled = false,
                    CancellationToken = cancellationToken
                });

                switch (outputResult)
                {
                    case { OutputResultModeEnum: OutputResultModeEnum.Ok }:
                        try
                        {
                            Guard.IsTrue(Step0ROI.BitmapImageROIDrawable.IsVisible);

                            Guard.IsTrue(Step0ROI.BitmapImageROIDrawable.Rect is { Width: > 0d, Height: > 0d });

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

            Step0ROI.ImageROI = _step0BitmapImageDrawable.CartesianCoordinateToImageCoordinate(Step0ROI.BitmapImageROIDrawable.Rect);
            Step0ROI.BitmapImageROIDrawable.IsFixed = true;
        }

        async Task Step1Async(BitmapImageROIDragMoveTypeEnum bitmapImageROIDragMoveTypeEnum)
        {
        InnerLoop:

            Step1ROI.ImageROI = Step0ROI.ImageROI;
            Step1ROI.BitmapImageROIDrawable.Rect = _step1BitmapImageDrawable.ImageCoordinateToCartesianCoordinate(Step1ROI.ImageROI);

            Document.View.SetViewBounds(_step1BitmapImageDrawable.GetExtents().Expand(new Padding(10d)));

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var outputResult = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(_step1BitmapImageDrawable)
                {
                    Title = $"Step 1 - {CenterChannelMotionKindEnum}, angle {AngleMotorAbsoluteValue}, position {Step1MotorAbsoluteValue}: move the fixed-size ROI {(CenterChannelMotionKindEnum == CenterChannelMotionKindEnum.Y ? "vertically" : "horizontally")} onto the same rod. Resizing is disabled. Press Enter to calculate PixelSize.",
                    BitmapImageROIDragMoveTypeEnum = bitmapImageROIDragMoveTypeEnum,
                    IsDeleteEnabled = false,
                    CancellationToken = cancellationToken
                });

                switch (outputResult)
                {
                    case { OutputResultModeEnum: OutputResultModeEnum.Ok }:
                        try
                        {
                            Guard.IsTrue(Step1ROI.BitmapImageROIDrawable.IsVisible);

                            Guard.IsTrue(Step1ROI.BitmapImageROIDrawable.Rect is { Width: > 0d, Height: > 0d });

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

            Step1ROI.ImageROI = _step1BitmapImageDrawable.CartesianCoordinateToImageCoordinate(Step1ROI.BitmapImageROIDrawable.Rect);
            Step1ROI.BitmapImageROIDrawable.IsFixed = true;

            var deltaCenter = Step1ROI.ImageROI.Center - Step0ROI.ImageROI.Center;
            var isVertical = CenterChannelMotionKindEnum == CenterChannelMotionKindEnum.Y;
            var deltaPixel = isVertical ? deltaCenter.Y : deltaCenter.X;

            var deltaMotorAbsoluteValue = Step1MotorAbsoluteValue - Step0MotorAbsoluteValue;
            if (deltaPixel == 0d || (deltaMotorAbsoluteValue < 0d ? deltaPixel > 0d : deltaPixel < 0d))
                ThrowHelper.ThrowInvalidOperationException($"""
                                                            Center-channel {CenterChannelMotionKindEnum}, angle {AngleMotorAbsoluteValue:0.###}: PixelSize cannot be calculated because the motor displacement or ROI center displacement is zero or non-finite.
                                                            Motor: Step0={Step0MotorAbsoluteValue:0.###}, Step1={Step1MotorAbsoluteValue:0.###}, Step1-Step0={deltaMotorAbsoluteValue:0.###}.
                                                            ROI center {(isVertical ? "Y" : "X")} (image px): Step0={(isVertical ? Step0ROI.ImageROI.Center.Y : Step0ROI.ImageROI.Center.X):0.###}, Step1={(isVertical ? Step1ROI.ImageROI.Center.Y : Step1ROI.ImageROI.Center.X):0.###}, Step1-Step0={deltaPixel:0.###}.
                                                            Check the recorded motor positions and image order. Move the fixed-size Step1 ROI onto the same rod along the {(isVertical ? "vertical" : "horizontal")} axis, then confirm again.
                                                            """);

            PixelSize = deltaMotorAbsoluteValue / deltaPixel;
            Guard.IsGreaterThan(PixelSize, 0d);
        }

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

            if (string.IsNullOrWhiteSpace(Step0ChannelImageFilePath) || string.IsNullOrWhiteSpace(Step1ChannelImageFilePath)) return;

            _step0BitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step0ChannelImageFilePath);

            _step1BitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step1ChannelImageFilePath);
            _step1BitmapImageDrawable.Point = _step0BitmapImageDrawable.Point - new Vector(0d, _step0BitmapImageDrawable.BitmapImage.Height + 10d);

            RestoreFindItem(Step0ROI, _step0BitmapImageDrawable);
            RestoreFindItem(Step1ROI, _step1BitmapImageDrawable);
        }
        finally
        {
            Document.View.ZoomToFit();
        }

        return;

        static void RestoreFindItem(FourierPupilCameraAlignmentDTO.ROI rod, BitmapImageDrawable bitmapImageDrawable)
        {
            rod.BitmapImageROIDrawable.IsFixed = true;
            rod.BitmapImageROIDrawable.Rect = bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(rod.ImageROI);
        }
    }

    #endregion 校准

    public object ToImageHtmlAnonymous() => new
    {
        Comment = new HtmlComment(StepsComment),
        CenterChannelMotionKindEnum,
        DefaultAngleMotorAbsoluteValue,
        AngleMotorAbsoluteValue,
        Step0MotorAbsoluteValue,
        Step1MotorAbsoluteValue,
        Step0ChannelImageFilePath,
        Step1ChannelImageFilePath,
        Step0Image = new HtmlImage(Step0ChannelImageFilePath),
        Step1Image = new HtmlImage(Step1ChannelImageFilePath)
    };

    public object ToHtmlAnonymous() => new
    {
        Comment = new HtmlComment(StepsComment),
        CenterChannelMotionKindEnum,
        DefaultAngleMotorAbsoluteValue,
        AngleMotorAbsoluteValue,
        Step0MotorAbsoluteValue,
        Step1MotorAbsoluteValue,
        Step0ChannelImageFilePath,
        Step1ChannelImageFilePath,
        Step0ImageROI = Step0ROI.ImageROI,
        Step1ImageROI = Step1ROI.ImageROI,
        Step0Image = new HtmlImage(Step0ChannelImageFilePath, htmlImageOverlays:
        [
            new HtmlImageRectangleOverlay(Step0ROI.ImageROI),
            new HtmlImageCrossOverlay(Step0ROI.ImageROI.Center)
        ]),
        Step1Image = new HtmlImage(Step1ChannelImageFilePath, htmlImageOverlays:
        [
            new HtmlImageRectangleOverlay(Step1ROI.ImageROI),
            new HtmlImageCrossOverlay(Step1ROI.ImageROI.Center)
        ]),
        PixelSize
    };

    public void Dispose()
    {
        _step0BitmapImageDrawable.Dispose();
        _step1BitmapImageDrawable.Dispose();
    }
}
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
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

public sealed partial class FourierCenterChannelFlexibleApertureMotionItem : ObservableObject, ICloneable<FourierCenterChannelFlexibleApertureMotionItem>, IDisposable
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

    public FourierCenterChannelFlexibleApertureDTO.FindItem Step0FindItem { get; }

    public FourierCenterChannelFlexibleApertureDTO.FindItem Step1FindItem { get; }

    /// <summary>
    /// 有符号的电机位移与图片ROI中心位移之比. X/Push使用中心X差, Y使用中心Y差, 单位为电机位置单位/像素.
    /// </summary>
    [ObservableProperty]
    public partial double PixelSize { get; private set; }

    [Newtonsoft.Json.JsonIgnore]
    public OpticsFourierImageDocument Document { get; } = new();

    public FourierCenterChannelFlexibleApertureMotionItem(CenterChannelMotionKindEnum centerChannelMotionKindEnum, double defaultAngleMotorAbsoluteValue)
    {
        CenterChannelMotionKindEnum = centerChannelMotionKindEnum;
        DefaultAngleMotorAbsoluteValue = defaultAngleMotorAbsoluteValue;
        AngleMotorAbsoluteValue = defaultAngleMotorAbsoluteValue;

        Step0FindItem = new FourierCenterChannelFlexibleApertureDTO.FindItem(_step0BitmapImageDrawable)
        {
            BitmapImageROIDrawable =
            {
                IsShowCrossLine = true,
                ResizeJoystickStateEnum = centerChannelMotionKindEnum switch
                {
                    CenterChannelMotionKindEnum.X or CenterChannelMotionKindEnum.Push => BitmapImageROIResizeJoystickStateEnum.XMinYCenter | BitmapImageROIResizeJoystickStateEnum.XMaxYCenter,
                    CenterChannelMotionKindEnum.Y => BitmapImageROIResizeJoystickStateEnum.XCenterYMin | BitmapImageROIResizeJoystickStateEnum.XCenterYMax,
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<BitmapImageROIResizeJoystickStateEnum>(nameof(centerChannelMotionKindEnum))
                }
            }
        };

        Step1FindItem = new FourierCenterChannelFlexibleApertureDTO.FindItem(_step1BitmapImageDrawable)
        {
            BitmapImageROIDrawable = { IsShowCrossLine = true, ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.None }
        };

        Document.RunDesign(() =>
        {
            Document.ImageModel.AddRange([_step0BitmapImageDrawable, _step1BitmapImageDrawable]);
            Document.ROIModel.AddRange([Step0FindItem.BitmapImageROIDrawable, Step1FindItem.BitmapImageROIDrawable]);
        });

        Document.Reset();
    }

    [Newtonsoft.Json.JsonConstructor]
    private FourierCenterChannelFlexibleApertureMotionItem(
        CenterChannelMotionKindEnum centerChannelMotionKindEnum,
        double defaultAngleMotorAbsoluteValue,
        FourierCenterChannelFlexibleApertureDTO.FindItem step0FindItem,
        FourierCenterChannelFlexibleApertureDTO.FindItem step1FindItem)
        : this(centerChannelMotionKindEnum, defaultAngleMotorAbsoluteValue)
    {
        Step0FindItem.AdaptIn(step0FindItem);
        Step1FindItem.AdaptIn(step1FindItem);
    }

    #region Mapper

    public FourierCenterChannelFlexibleApertureMotionItem Clone()
    {
        var item = new FourierCenterChannelFlexibleApertureMotionItem(CenterChannelMotionKindEnum, DefaultAngleMotorAbsoluteValue)
        {
            AngleMotorAbsoluteValue = AngleMotorAbsoluteValue,
            Step0MotorAbsoluteValue = Step0MotorAbsoluteValue,
            Step1MotorAbsoluteValue = Step1MotorAbsoluteValue,
            Step0ChannelImageFilePath = Step0ChannelImageFilePath,
            Step1ChannelImageFilePath = Step1ChannelImageFilePath,
            PixelSize = PixelSize
        };

        item.Step0FindItem.AdaptIn(Step0FindItem);
        item.Step1FindItem.AdaptIn(Step1FindItem);

        return item;
    }

    #endregion Mapper

    #region 校准

    public void Reset()
    {
        // 保留界面输入的角度及两步电机位置, 仅清空测量结果.
        Step0ChannelImageFilePath = string.Empty;
        Step1ChannelImageFilePath = string.Empty;

        Step0FindItem.Reset();
        Step1FindItem.Reset();
        PixelSize = 0d;

        Document.Reset();
    }

    public async Task CalibratingAsync(CancellationToken cancellationToken)
    {
        try
        {
            Document.Reset();
            PixelSize = 0d;

            Guard.IsNotNullOrWhiteSpace(Step0ChannelImageFilePath);
            Guard.IsNotNullOrWhiteSpace(Step1ChannelImageFilePath);

            _step0BitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step0ChannelImageFilePath);

            _step1BitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step1ChannelImageFilePath);
            if (_step0BitmapImageDrawable.BitmapImage.Size != _step1BitmapImageDrawable.BitmapImage.Size)
                ThrowHelper.ThrowInvalidOperationException("Step 0 and Step 1 images must have the same dimensions.");

            _step1BitmapImageDrawable.Point = _step0BitmapImageDrawable.Point - new Vector(0d, _step0BitmapImageDrawable.BitmapImage.Height + 10d);

            Document.View.ZoomToFit();

            var bitmapImageROIDragMoveTypeEnum = CenterChannelMotionKindEnum switch
            {
                CenterChannelMotionKindEnum.X or CenterChannelMotionKindEnum.Push => BitmapImageROIDragMoveTypeEnum.X,
                CenterChannelMotionKindEnum.Y => BitmapImageROIDragMoveTypeEnum.Y,
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<BitmapImageROIDragMoveTypeEnum>(nameof(CenterChannelMotionKindEnum))
            };

            cancellationToken.ThrowIfCancellationRequested();
            await Step0Async(bitmapImageROIDragMoveTypeEnum);

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
                Step0FindItem.BitmapImageROIDrawable.Rect = _step0BitmapImageDrawable.ImageCoordinateToCartesianCoordinate(new Rect(_step0BitmapImageDrawable.BitmapImage.Size.Width / 2d - width / 2d, 0d, width, _step0BitmapImageDrawable.BitmapImage.Height)).ImageCoordinateRound();
            }
            else
            {
                var height = _step0BitmapImageDrawable.BitmapImage.Height / 4;
                Step0FindItem.BitmapImageROIDrawable.Rect = _step0BitmapImageDrawable.ImageCoordinateToCartesianCoordinate(new Rect(0d, _step0BitmapImageDrawable.BitmapImage.Height / 2d - height / 2d, _step0BitmapImageDrawable.BitmapImage.Width, height)).ImageCoordinateRound();
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
                            Guard.IsTrue(Step0FindItem.BitmapImageROIDrawable.IsVisible);

                            Guard.IsTrue(Step0FindItem.BitmapImageROIDrawable.Rect is { Width: > 0d, Height: > 0d });

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

            Step0FindItem.ImageROI = _step0BitmapImageDrawable.CartesianCoordinateToImageCoordinate(Step0FindItem.BitmapImageROIDrawable.Rect);
            Step0FindItem.BitmapImageROIDrawable.IsFixed = true;
        }

        async Task Step1Async(BitmapImageROIDragMoveTypeEnum bitmapImageROIDragMoveTypeEnum)
        {
        InnerLoop:

            Step1FindItem.ImageROI = Step0FindItem.ImageROI;
            Step1FindItem.BitmapImageROIDrawable.Rect = _step1BitmapImageDrawable.ImageCoordinateToCartesianCoordinate(Step1FindItem.ImageROI);

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
                            Guard.IsTrue(Step1FindItem.BitmapImageROIDrawable.IsVisible);

                            Guard.IsTrue(Step1FindItem.BitmapImageROIDrawable.Rect is { Width: > 0d, Height: > 0d });

                            var imageROI = _step1BitmapImageDrawable.CartesianCoordinateToImageCoordinate(Step1FindItem.BitmapImageROIDrawable.Rect);
                            PixelSize = CalculatePixelSize(imageROI);

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

            Step1FindItem.ImageROI = _step1BitmapImageDrawable.CartesianCoordinateToImageCoordinate(Step1FindItem.BitmapImageROIDrawable.Rect);
            Step1FindItem.BitmapImageROIDrawable.IsFixed = true;
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

    private double CalculatePixelSize(Rect step1ImageROI)
    {
        var step0ImageROI = Step0FindItem.ImageROI;
        if (step1ImageROI.Width != step0ImageROI.Width || step1ImageROI.Height != step0ImageROI.Height)
            ThrowHelper.ThrowInvalidOperationException("Step 1 must retain the ROI dimensions established in Step 0.");

        var deltaCenter = step1ImageROI.Center - step0ImageROI.Center;
        var isVertical = CenterChannelMotionKindEnum == CenterChannelMotionKindEnum.Y;
        if ((isVertical ? deltaCenter.X : deltaCenter.Y) != 0d)
            ThrowHelper.ThrowInvalidOperationException("The ROI must only move along the calibrated axis.");

        // 图片坐标系与电机的正方向可能相反, 保留有符号比例, 不将中心位移误写成尺寸差.
        var deltaPixel = isVertical ? deltaCenter.Y : deltaCenter.X;
        var deltaMotorAbsoluteValue = Step1MotorAbsoluteValue - Step0MotorAbsoluteValue;
        if (!double.IsFinite(deltaMotorAbsoluteValue) || deltaMotorAbsoluteValue == 0d || !double.IsFinite(deltaPixel) || deltaPixel == 0d)
            ThrowHelper.ThrowInvalidOperationException($"""
                Center-channel {CenterChannelMotionKindEnum}, angle {AngleMotorAbsoluteValue:G17}: PixelSize cannot be calculated because the motor displacement or ROI center displacement is zero or non-finite.
                Motor: Step0={Step0MotorAbsoluteValue:G17}, Step1={Step1MotorAbsoluteValue:G17}, Step1-Step0={deltaMotorAbsoluteValue:G17}.
                ROI center {(isVertical ? "Y" : "X")} (image px): Step0={(isVertical ? step0ImageROI.Center.Y : step0ImageROI.Center.X):G17}, Step1={(isVertical ? step1ImageROI.Center.Y : step1ImageROI.Center.X):G17}, Step1-Step0={deltaPixel:G17}.
                Check the recorded motor positions and image order. Move the fixed-size Step1 ROI onto the same rod along the {(isVertical ? "vertical" : "horizontal")} axis, then confirm again.
                """);

        var pixelSize = deltaMotorAbsoluteValue / deltaPixel;
        if (!double.IsFinite(pixelSize) || pixelSize == 0d)
            ThrowHelper.ThrowInvalidOperationException($"Center-channel {CenterChannelMotionKindEnum}, angle {AngleMotorAbsoluteValue:G17}: PixelSize = motor displacement / ROI center {(isVertical ? "Y" : "X")} displacement = {deltaMotorAbsoluteValue:G17} / {deltaPixel:G17} = {pixelSize:G17}. The result must be finite and nonzero. Check both motor positions and ROI centers before recalibrating.");

        return pixelSize;
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

            RestoreFindItem(Step0FindItem, _step0BitmapImageDrawable);
            RestoreFindItem(Step1FindItem, _step1BitmapImageDrawable);
        }
        finally
        {
            Document.View.ZoomToFit();
        }

        return;

        static void RestoreFindItem(FourierCenterChannelFlexibleApertureDTO.FindItem findItem, BitmapImageDrawable bitmapImageDrawable)
        {
            findItem.BitmapImageROIDrawable.IsFixed = true;
            findItem.BitmapImageROIDrawable.Rect = bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(findItem.ImageROI);
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
        Step0ImageROI = Step0FindItem.ImageROI,
        Step1ImageROI = Step1FindItem.ImageROI,
        Step0Image = new HtmlImage(Step0ChannelImageFilePath, htmlImageOverlays:
        [
            new HtmlImageRectangleOverlay(Step0FindItem.ImageROI),
            new HtmlImageCrossOverlay(Step0FindItem.ImageROI.Center)
        ]),
        Step1Image = new HtmlImage(Step1ChannelImageFilePath, htmlImageOverlays:
        [
            new HtmlImageRectangleOverlay(Step1FindItem.ImageROI),
            new HtmlImageCrossOverlay(Step1FindItem.ImageROI.Center)
        ]),
        PixelSize
    };

    public void Dispose()
    {
        _step0BitmapImageDrawable.Dispose();
        _step1BitmapImageDrawable.Dispose();
    }
}
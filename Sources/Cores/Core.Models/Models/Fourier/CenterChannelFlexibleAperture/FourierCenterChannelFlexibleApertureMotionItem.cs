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
    private const string StepsComment = "Step0: Capture the center rod at Step0MotorAbsoluteValue; Step1: Capture the same rod at Step1MotorAbsoluteValue. X/Push measure width, Y measures height.";

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
            BitmapImageROIDrawable = { ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.None }
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
                    Title = $"Step 0: Select the {CenterChannelMotionKindEnum} center rod ROI.",
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
                    Title = $"Step 1: Adjust the {CenterChannelMotionKindEnum} center rod size.",
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

            var deltaMotorAbsoluteValue = Step1MotorAbsoluteValue - Step0MotorAbsoluteValue;
            Guard.IsNotEqualTo(deltaMotorAbsoluteValue, 0d);

            // 检查实际写入的尺寸差, 取整或裁剪后为零或反向时直接报错, 不使用回退值.
            var deltaPixel = CenterChannelMotionKindEnum switch
            {
                CenterChannelMotionKindEnum.X or CenterChannelMotionKindEnum.Push => (Step1FindItem.ImageROI.Center - Step0FindItem.ImageROI.Center).X,
                CenterChannelMotionKindEnum.Y => (Step1FindItem.ImageROI.Center - Step0FindItem.ImageROI.Center).Y,
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(CenterChannelMotionKindEnum))
            };
            if (deltaPixel == 0d || (deltaMotorAbsoluteValue < 0d ? deltaPixel > 0d : deltaPixel < 0d))
            {
                ThrowHelper.ThrowInvalidOperationException($"The extrapolated size difference is inconsistent with the motor displacement. {deltaPixel}: Step0 center={Step1FindItem.ImageROI.Center}, Step1 center={Step0FindItem.ImageROI.Center}, Delta Motor Absolute={deltaMotorAbsoluteValue}.");
            }

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
            new HtmlImageRectangleOverlay(Step0FindItem.ImageROI)
        ]),
        Step1Image = new HtmlImage(Step1ChannelImageFilePath, htmlImageOverlays:
        [
            new HtmlImageRectangleOverlay(Step1FindItem.ImageROI)
        ]),
        PixelSize
    };

    public void Dispose()
    {
        _step0BitmapImageDrawable.Dispose();
        _step1BitmapImageDrawable.Dispose();
    }
}
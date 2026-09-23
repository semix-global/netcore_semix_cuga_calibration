using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Fourier.V0CenterChannelFlexibleAperture;
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

namespace Core.Models.Models.Fourier.V0CenterChannelSpecularBlocker;

public sealed partial class V0FourierCenterChannelSpecularBlockerDTOItem : ObservableObject, ICloneable<V0FourierCenterChannelSpecularBlockerDTOItem>, IDisposable
{
    private const string StepsComment = "Step0: Move Y rod and X push home; Step1: move them to the calibrated positions. Grab Fourier images during calibration and CIB images during verification";

    private readonly BitmapImageDrawable _step0FourierBitmapImageDrawable = new();
    private readonly BitmapImageDrawable _step1FourierBitmapImageDrawable = new();
    private readonly BitmapImageROIDrawable _yRodBitmapImageROIDrawable;
    private readonly BitmapImageROIDrawable _pushBitmapImageROIDrawable;
    private readonly BitmapImageROIDrawable _step1YRodBitmapImageROIDrawable;
    private readonly BitmapImageROIDrawable _step1PushBitmapImageROIDrawable;

    private readonly BitmapImageDrawable _step0CIBBitmapImageDrawable = new();
    private readonly BitmapImageDrawable _step1CIBBitmapImageDrawable = new();

    public int ChannelId { get; }

    [ObservableProperty]
    public partial double YRodAngleMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial Rect YRodImageROI { get; set; }

    [ObservableProperty]
    public partial double YRodMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial Rect PushImageROI { get; set; }

    [ObservableProperty]
    public partial double PushMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial string Step0FourierImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Step1FourierImageFilePath { get; set; } = string.Empty;

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

    public V0FourierCenterChannelSpecularBlockerDTOItem(int channelId)
    {
        ChannelId = channelId;

        _yRodBitmapImageROIDrawable = new BitmapImageROIDrawable(_step0FourierBitmapImageDrawable)
        {
            Text = "Y",
            ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.None
        };
        _pushBitmapImageROIDrawable = new BitmapImageROIDrawable(_step0FourierBitmapImageDrawable)
        {
            Text = "Push",
            ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.None
        };
        _step1YRodBitmapImageROIDrawable = new BitmapImageROIDrawable(_step1FourierBitmapImageDrawable) { Text = "Y" };
        _step1PushBitmapImageROIDrawable = new BitmapImageROIDrawable(_step1FourierBitmapImageDrawable) { Text = "Push" };

        Document.RunDesign(() =>
        {
            Document.ImageModel.AddRange([_step0FourierBitmapImageDrawable, _step1FourierBitmapImageDrawable]);
            Document.ROIModel.AddRange([_yRodBitmapImageROIDrawable, _pushBitmapImageROIDrawable, _step1YRodBitmapImageROIDrawable, _step1PushBitmapImageROIDrawable]);
        });

        CIBDocument.RunDesign(() => CIBDocument.ImageModel.AddRange([_step0CIBBitmapImageDrawable, _step1CIBBitmapImageDrawable]));

        ResetDocument();
        ResetCIBDocument();
    }

    #region Mapper

#pragma warning disable IDISP003

    public V0FourierCenterChannelSpecularBlockerDTOItem Clone() => new(ChannelId)
    {
        YRodAngleMotorAbsoluteValue = YRodAngleMotorAbsoluteValue,
        YRodImageROI = YRodImageROI,
        YRodMotorAbsoluteValue = YRodMotorAbsoluteValue,
        PushImageROI = PushImageROI,
        PushMotorAbsoluteValue = PushMotorAbsoluteValue,
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

#pragma warning restore IDISP003

    #endregion Mapper

    #region 校准

    public void Reset()
    {
        Step0FourierImageFilePath = string.Empty;
        Step1FourierImageFilePath = string.Empty;
        YRodImageROI = Rect.Empty;
        YRodMotorAbsoluteValue = 0d;
        PushImageROI = Rect.Empty;
        PushMotorAbsoluteValue = 0d;

        ResetDocument();
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

        ResetCIBDocument();
    }

    public async Task CalibratingAsync(
        V0FourierCenterChannelFlexibleApertureDTOItem? yRodItem,
        V0FourierCenterChannelFlexibleApertureDTOItem pushItem,
        (double Min, double Max) yMotorRange,
        (double Min, double Max) pushMotorRange,
        CancellationToken cancellationToken)
    {
        try
        {
            ResetDocument();

            Guard.IsNotNullOrWhiteSpace(Step0FourierImageFilePath);
            Guard.IsTrue(pushItem.CenterChannelMotionKindEnum == CenterChannelMotionKindEnum.XPush);
            Guard.IsTrue(double.IsFinite(pushItem.PixelSize) && pushItem.PixelSize > 0d);
            Guard.IsTrue(pushItem.Step0ROI.ImageROI is { Width: > 0d, Height: > 0d });
            Guard.IsLessThanOrEqualTo(pushMotorRange.Min, pushMotorRange.Max);

            var showYRod = yRodItem is not null;
            if (yRodItem is not null)
            {
                Guard.IsTrue(yRodItem.CenterChannelMotionKindEnum == CenterChannelMotionKindEnum.YRod);
                Guard.IsEqualTo(yRodItem.DefaultAngleMotorAbsoluteValue, YRodAngleMotorAbsoluteValue);
                Guard.IsTrue(double.IsFinite(yRodItem.PixelSize) && yRodItem.PixelSize > 0d);
                Guard.IsTrue(yRodItem.Step0ROI.ImageROI is { Width: > 0d, Height: > 0d });
                Guard.IsLessThanOrEqualTo(yMotorRange.Min, yMotorRange.Max);
            }

            _step0FourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step0FourierImageFilePath);
            Document.View.ZoomToFit();

            Place(_pushBitmapImageROIDrawable, pushItem.Step0ROI.ImageROI, "Push");
            if (showYRod) Place(_yRodBitmapImageROIDrawable, yRodItem!.Step0ROI.ImageROI, $"Y {YRodAngleMotorAbsoluteValue:0.###}");

            if (showYRod)
            {
                _pushBitmapImageROIDrawable.IsFixed = true;
                await EditAsync(_yRodBitmapImageROIDrawable, BitmapImageROIDragMoveTypeEnum.Y, $"Move the {YRodAngleMotorAbsoluteValue:0.###} degree Y rod vertically.", cancellationToken);
                _yRodBitmapImageROIDrawable.IsFixed = true;
            }

            _pushBitmapImageROIDrawable.IsFixed = false;
            await EditAsync(_pushBitmapImageROIDrawable, BitmapImageROIDragMoveTypeEnum.X, "Move the X push horizontally.", cancellationToken);

            PushImageROI = _step0FourierBitmapImageDrawable.CartesianCoordinateToImageCoordinate(_pushBitmapImageROIDrawable.Rect);
            _pushBitmapImageROIDrawable.IsFixed = true;
            PushMotorAbsoluteValue = Math.Clamp(
                pushItem.Step0MotorAbsoluteValue + (PushImageROI.Center.X - pushItem.Step0ROI.ImageROI.Center.X) * pushItem.PixelSize,
                pushMotorRange.Min,
                pushMotorRange.Max);

            if (showYRod)
            {
                YRodImageROI = _step0FourierBitmapImageDrawable.CartesianCoordinateToImageCoordinate(_yRodBitmapImageROIDrawable.Rect);
                YRodMotorAbsoluteValue = Math.Clamp(
                    yRodItem!.Step0MotorAbsoluteValue + (YRodImageROI.Center.Y - yRodItem.Step0ROI.ImageROI.Center.Y) * yRodItem.PixelSize,
                    yMotorRange.Min,
                    yMotorRange.Max);
            }
        }
        finally
        {
            Document.View.ZoomToFit();
        }

        return;

        void Place(BitmapImageROIDrawable drawable, Rect imageROI, string text)
        {
            drawable.Text = text;
            drawable.ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.None;
            drawable.IsFixed = false;
            drawable.Rect = _step0FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(imageROI);
            drawable.IsVisible = drawable.Rect is { Width: > 0d, Height: > 0d };
        }

        async Task EditAsync(BitmapImageROIDrawable drawable, BitmapImageROIDragMoveTypeEnum dragMoveTypeEnum, string title, CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();

                var outputResult = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(_step0FourierBitmapImageDrawable)
                {
                    Title = title,
                    BitmapImageROIDragMoveTypeEnum = dragMoveTypeEnum,
                    IsDeleteEnabled = false,
                    CancellationToken = token
                });

                switch (outputResult)
                {
                    case { OutputResultModeEnum: OutputResultModeEnum.Ok }:
                        Guard.IsTrue(drawable.IsVisible);
                        Guard.IsTrue(drawable.Rect is { Width: > 0d, Height: > 0d });

                        return;

                    case { OutputResultModeEnum: OutputResultModeEnum.Cancel, CancelReason: CancelReasonEnum.Escape }:
                        continue;

                    case { OutputResultModeEnum: OutputResultModeEnum.Cancel, CancelReason: CancelReasonEnum.OperationCanceledException }:
                        ThrowHelper.ThrowOperationCanceledException(token);

                        break;

                    default:
                        ThrowHelper.ThrowInvalidOperationException($"{nameof(outputResult.CancelReason)}: {outputResult.CancelReason}, {nameof(outputResult.ErrorMessage)}: {outputResult.ErrorMessage}");

                        break;
                }
            }
        }
    }

    public void Review()
    {
        try
        {
            ResetDocument();

            if (string.IsNullOrWhiteSpace(Step0FourierImageFilePath) || string.IsNullOrWhiteSpace(Step1FourierImageFilePath)) return;

            _step0FourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step0FourierImageFilePath);

            _step1FourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step1FourierImageFilePath);
            _step1FourierBitmapImageDrawable.Point = _step0FourierBitmapImageDrawable.Point - new Vector(0d, _step0FourierBitmapImageDrawable.BitmapImage.Height + 10d);

            ShowReviewROI(_pushBitmapImageROIDrawable, _step1PushBitmapImageROIDrawable, PushImageROI, "Push");
            if (YRodAngleMotorAbsoluteValue != 0d) ShowReviewROI(_yRodBitmapImageROIDrawable, _step1YRodBitmapImageROIDrawable, YRodImageROI, $"Y {YRodAngleMotorAbsoluteValue:0.###}");
        }
        finally
        {
            Document.View.ZoomToFit();
        }

        return;

        void ShowReviewROI(BitmapImageROIDrawable step0Drawable, BitmapImageROIDrawable step1Drawable, Rect imageROI, string text)
        {
            step0Drawable.Text = step1Drawable.Text = text;
            step0Drawable.IsFixed = step1Drawable.IsFixed = true;
            step0Drawable.Rect = _step0FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(imageROI);
            step1Drawable.Rect = _step1FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(imageROI);
            step0Drawable.IsVisible = step1Drawable.IsVisible = step0Drawable.Rect is { Width: > 0d };
        }
    }

    public void ReviewCIB()
    {
        try
        {
            ResetCIBDocument();

            if (string.IsNullOrWhiteSpace(Step0CIBImageFilePath)) return;

            _step0CIBBitmapImageDrawable.Point = Point.Origin;
            _step0CIBBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(RawStep0CIBImageFilePath);

            if (string.IsNullOrWhiteSpace(Step1CIBImageFilePath)) return;

            _step1CIBBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(RawStep1CIBImageFilePath);
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
        ChannelId,
        YRodAngleMotorAbsoluteValue,
        Step0FourierImageFilePath,
        Step0FourierImage = new HtmlImage(Step0FourierImageFilePath)
    };

    public object ToHtmlAnonymous() => new
    {
        Comment = new HtmlComment(StepsComment),
        ChannelId,
        YRodAngleMotorAbsoluteValue,
        YRodImageROI,
        YRodMotorAbsoluteValue,
        PushImageROI,
        PushMotorAbsoluteValue,
        Step0FourierImageFilePath,
        Step1FourierImageFilePath,
        Step0FourierImage = new HtmlImage(Step0FourierImageFilePath, htmlImageOverlays: Overlays()),
        Step1FourierImage = new HtmlImage(Step1FourierImageFilePath, htmlImageOverlays: Overlays())
    };

    public object ToCIBHtmlAnonymous() => new
    {
        Comment = new HtmlComment(StepsComment),
        ChannelId,
        YRodAngleMotorAbsoluteValue,
        YRodMotorAbsoluteValue,
        PushMotorAbsoluteValue,
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

    private HtmlImageOverlay[] Overlays()
    {
        List<HtmlImageOverlay> overlays = [];

        if (YRodAngleMotorAbsoluteValue != 0d && YRodImageROI is { Width: > 0d })
        {
            overlays.Add(new HtmlImageRectangleOverlay(YRodImageROI));
            overlays.Add(new HtmlImageTextOverlay(YRodImageROI.Center, $"Y {YRodAngleMotorAbsoluteValue:0.###}"));
        }

        if (PushImageROI is { Width: > 0d })
        {
            overlays.Add(new HtmlImageRectangleOverlay(PushImageROI));
            overlays.Add(new HtmlImageTextOverlay(PushImageROI.Center, "Push"));
        }

        return [.. overlays];
    }

    private void ResetDocument()
    {
        Document.Reset();

        foreach (var drawable in new[] { _yRodBitmapImageROIDrawable, _pushBitmapImageROIDrawable, _step1YRodBitmapImageROIDrawable, _step1PushBitmapImageROIDrawable })
        {
            drawable.IsVisible = false;
            drawable.IsFixed = false;
        }
    }

    private void ResetCIBDocument()
    {
        CIBDocument.Reset();
    }

    public void Dispose()
    {
        _step0FourierBitmapImageDrawable.Dispose();
        _step1FourierBitmapImageDrawable.Dispose();
        _step0CIBBitmapImageDrawable.Dispose();
        _step1CIBBitmapImageDrawable.Dispose();
    }
}

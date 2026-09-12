using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Fourier.SideChannelFlexibleAperture;
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

namespace Core.Models.Models.Fourier.SideChannelSpecularBlocker;

public sealed partial class FourierSideChannelSpecularBlockerDTOItem : ObservableObject, ICloneable<FourierSideChannelSpecularBlockerDTOItem>, IDisposable
{
    private const string StepsComment = "Step0: Move rods home and grab Fourier / PMT images; Step1: Drop rods to the calibrated positions and grab Fourier / PMT images";

    private readonly BitmapImageDrawable _step0FourierBitmapImageDrawable = new();
    private readonly BitmapImageDrawable _step1FourierBitmapImageDrawable = new();
    private readonly BitmapImageDrawable _step0PMTBitmapImageDrawable = new();
    private readonly BitmapImageDrawable _step1PMTBitmapImageDrawable = new();

    [Newtonsoft.Json.JsonProperty]
    private readonly int _rodTotalCount;

    [Newtonsoft.Json.JsonProperty]
    public int ChannelId { get; }

    [ObservableProperty]
    public partial string Step0FourierImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Step1FourierImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RawStep0PMTImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RawStep1PMTImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Step0PMTImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Step1PMTImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double Step0PMTImageAverageValue { get; set; }

    [ObservableProperty]
    public partial double Step1PMTImageAverageValue { get; set; }

    [Newtonsoft.Json.JsonProperty]
    public Rod[] Rods { get; }

    [ObservableProperty]
    public partial double ExtinctionRatio { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public OpticsFourierImageDocument Document { get; } = new();

    public FourierSideChannelSpecularBlockerDTOItem(int rodTotalCount, int channelId)
    {
        _rodTotalCount = rodTotalCount;
        ChannelId = channelId;
        Rods =
        [
            .. Generate.LinearRangeInt32(0, rodTotalCount - 1)
                .Select(i => new Rod(_step0FourierBitmapImageDrawable)
                {
                    Index = i,
                    BitmapImageROIDrawable = { ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.XCenterYMin }
                })
        ];

        Document.RunDesign(() =>
        {
            Document.ImageModel.AddRange(
            [
                _step0FourierBitmapImageDrawable,
                _step1FourierBitmapImageDrawable,
                _step0PMTBitmapImageDrawable,
                _step1PMTBitmapImageDrawable
            ]);
            Document.ROIModel.AddRange(Rods.Select(t => t.BitmapImageROIDrawable));
        });

        ResetDocument();
    }

    [Newtonsoft.Json.JsonConstructor]
    private FourierSideChannelSpecularBlockerDTOItem(
        [Newtonsoft.Json.JsonProperty(nameof(_rodTotalCount))]
        int rodTotalCount,
        int channelId,
        Rod[] rods)
        : this(rodTotalCount, channelId)
    {
        foreach (var target in Rods) target.AdaptIn(rods.Single(t => t.Index == target.Index));
    }

    private void ResetDocument()
    {
        Document.Reset();

        foreach (var rod in Rods) rod.BitmapImageROIDrawable.Text = $"{rod.Index + 1}";
    }

    #region Mapper

#pragma warning disable IDISP003

    public FourierSideChannelSpecularBlockerDTOItem Clone()
    {
        var item = new FourierSideChannelSpecularBlockerDTOItem(_rodTotalCount, ChannelId)
        {
            Step0FourierImageFilePath = Step0FourierImageFilePath,
            Step1FourierImageFilePath = Step1FourierImageFilePath,
            RawStep0PMTImageFilePath = RawStep0PMTImageFilePath,
            RawStep1PMTImageFilePath = RawStep1PMTImageFilePath,
            Step0PMTImageFilePath = Step0PMTImageFilePath,
            Step1PMTImageFilePath = Step1PMTImageFilePath,
            Step0PMTImageAverageValue = Step0PMTImageAverageValue,
            Step1PMTImageAverageValue = Step1PMTImageAverageValue,
            ExtinctionRatio = ExtinctionRatio
        };

        foreach (var target in item.Rods) target.AdaptIn(Rods.Single(t => t.Index == target.Index));

        return item;
    }

#pragma warning restore IDISP003

    #endregion

    #region 校准

    public void Reset()
    {
        Step0FourierImageFilePath = string.Empty;
        Step1FourierImageFilePath = string.Empty;
        RawStep0PMTImageFilePath = string.Empty;
        RawStep1PMTImageFilePath = string.Empty;
        Step0PMTImageFilePath = string.Empty;
        Step1PMTImageFilePath = string.Empty;
        Step0PMTImageAverageValue = 0d;
        Step1PMTImageAverageValue = 0d;
        ExtinctionRatio = 0d;

        foreach (var rod in Rods) rod.Reset();

        ResetDocument();
    }

    public async Task CalibratingAsync(FourierSideChannelFlexibleApertureDTOItem fourierSideChannelFlexibleApertureItem, CancellationToken cancellationToken)
    {
        try
        {
            ResetDocument();

            Guard.IsNotNullOrWhiteSpace(Step0FourierImageFilePath);
            Guard.IsEqualTo(fourierSideChannelFlexibleApertureItem.RodResults.Length, _rodTotalCount);

            _step0FourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step0FourierImageFilePath);
            Document.View.ZoomToFit();

            foreach (var rod in Rods)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rodResult = fourierSideChannelFlexibleApertureItem.RodResults.Single(t => t.Index == rod.Index);

                rod.IsDeleted = rodResult.IsDeleted;
                rod.ImageROI = rodResult.MinImageROI
                    .ClampToBounds(new Rect(Point.Origin, _step0FourierBitmapImageDrawable.BitmapImage.Size));
                if (rod.IsDeleted) rod.BitmapImageROIDrawable.Text = $"X {rod.BitmapImageROIDrawable.Text}";
                rod.BitmapImageROIDrawable.IsFixed = false;
                rod.BitmapImageROIDrawable.Rect = _step0FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(rod.ImageROI);
                rod.BitmapImageROIDrawable.IsVisible = rod.BitmapImageROIDrawable.Rect is { Width: > 0, Height: > 0 };
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var outputResult = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(_step0FourierBitmapImageDrawable)
                {
                    BitmapImageROIDragMoveTypeEnum = BitmapImageROIDragMoveTypeEnum.None,
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

            foreach (var rod in Rods)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rodResult = fourierSideChannelFlexibleApertureItem.RodResults.Single(t => t.Index == rod.Index);

                rod.BitmapImageROIDrawable.IsFixed = true;
                rod.ImageROI = _step0FourierBitmapImageDrawable.CartesianCoordinateToImageCoordinate(rod.BitmapImageROIDrawable.Rect);
                rod.MotorAbsoluteValue = Math.Clamp(
                    fourierSideChannelFlexibleApertureItem.MinMotorAbsoluteValue + (rod.ImageROI.Height - rodResult.MinImageROI.Height) * rodResult.PixelSize,
                    fourierSideChannelFlexibleApertureItem.MinMotorAbsoluteValue,
                    fourierSideChannelFlexibleApertureItem.MaxMotorAbsoluteValue);
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
            ResetDocument();

            if (string.IsNullOrWhiteSpace(Step0FourierImageFilePath)
                || string.IsNullOrWhiteSpace(Step1FourierImageFilePath)
                || string.IsNullOrWhiteSpace(Step0PMTImageFilePath)
                || string.IsNullOrWhiteSpace(Step1PMTImageFilePath)) return;

            _step0FourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step0FourierImageFilePath);

            _step1FourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step1FourierImageFilePath);
            _step1FourierBitmapImageDrawable.Point = _step0FourierBitmapImageDrawable.Point - new Vector(0d, _step0FourierBitmapImageDrawable.BitmapImage.Height + 10d);

            _step0PMTBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step0PMTImageFilePath);
            _step0PMTBitmapImageDrawable.Point = _step0FourierBitmapImageDrawable.Point + new Vector(_step0FourierBitmapImageDrawable.BitmapImage.Width + 10d, 0d);

            _step1PMTBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step1PMTImageFilePath);
            _step1PMTBitmapImageDrawable.Point = _step0PMTBitmapImageDrawable.Point - new Vector(0d, _step0PMTBitmapImageDrawable.BitmapImage.Height + 10d);

            foreach (var rod in Rods)
            {
                if (rod.IsDeleted) rod.BitmapImageROIDrawable.Text = $"X {rod.BitmapImageROIDrawable.Text}";
                rod.BitmapImageROIDrawable.IsFixed = true;
                rod.BitmapImageROIDrawable.Rect = _step0FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(rod.ImageROI);
                rod.BitmapImageROIDrawable.IsVisible = rod.BitmapImageROIDrawable.Rect is { Width: > 0, Height: > 0 };
            }
        }
        finally
        {
            Document.View.ZoomToFit();
        }
    }

    #endregion

    public object ToImageHtmlAnonymous() => new
    {
        Comment = new HtmlComment(StepsComment),
        ChannelId,
        Step0FourierImageFilePath,
        RawStep0PMTImageFilePath,
        Step0PMTImageFilePath,
        Step0FourierImage = new HtmlImage(Step0FourierImageFilePath),
        Step0PMTImage = new HtmlImage(Step0PMTImageFilePath)
    };

    public object ToHtmlAnonymous() => new
    {
        Comment = new HtmlComment(StepsComment),
        ChannelId,
        Step0PMTImageAverageValue,
        Step1PMTImageAverageValue,
        ExtinctionRatio,
        Step0FourierImageFilePath,
        Step1FourierImageFilePath,
        RawStep0PMTImageFilePath,
        RawStep1PMTImageFilePath,
        Step0PMTImageFilePath,
        Step1PMTImageFilePath,
        Rods = new HtmlTable([.. Rods.Select(t => new { t.Index, t.IsDeleted, t.ImageROI, t.MotorAbsoluteValue })]),
        Step0FourierImage = new HtmlImage(Step0FourierImageFilePath, htmlImageOverlays:
        [
            .. Rods.Select(t => new HtmlImageRectangleOverlay(t.ImageROI)),
            .. Rods.Select(t => new HtmlImageTextOverlay(t.ImageROI.Center, t.BitmapImageROIDrawable.Text))
        ]),
        Step1FourierImage = new HtmlImage(Step1FourierImageFilePath, htmlImageOverlays:
        [
            .. Rods.Select(t => new HtmlImageRectangleOverlay(t.ImageROI)),
            .. Rods.Select(t => new HtmlImageTextOverlay(t.ImageROI.Center, t.BitmapImageROIDrawable.Text))
        ]),
        Step0PMTImage = new HtmlImage(Step0PMTImageFilePath),
        Step1PMTImage = new HtmlImage(Step1PMTImageFilePath)
    };

    public void Dispose()
    {
        _step0FourierBitmapImageDrawable.Dispose();
        _step1FourierBitmapImageDrawable.Dispose();
        _step0PMTBitmapImageDrawable.Dispose();
        _step1PMTBitmapImageDrawable.Dispose();
    }
}
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
    private const string StepsComment = "Step0: Move rods home; Step1: Drop rods to the calibrated positions. Grab Fourier images during calibration and CIB images during verification";

    private readonly BitmapImageDrawable _step0FourierBitmapImageDrawable = new();
    private readonly BitmapImageDrawable _step1FourierBitmapImageDrawable = new();
    private readonly BitmapImageROIDrawable[] _step1FourierROIDrawables;

    private readonly BitmapImageDrawable _step0CIBBitmapImageDrawable = new();
    private readonly BitmapImageDrawable _step1CIBBitmapImageDrawable = new();

    [Newtonsoft.Json.JsonProperty]
    private readonly int _rodTotalCount;

    public int ChannelId { get; }

    [ObservableProperty]
    public partial string Step0FourierImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Step1FourierImageFilePath { get; set; } = string.Empty;

    public Rod[] Rods { get; }

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

        _step1FourierROIDrawables = [.. Rods.Select(_ => new BitmapImageROIDrawable(_step1FourierBitmapImageDrawable))];

        Document.RunDesign(() =>
        {
            Document.ImageModel.AddRange(
            [
                _step0FourierBitmapImageDrawable,
                _step1FourierBitmapImageDrawable
            ]);
            Document.ROIModel.AddRange(Rods.Select(t => t.BitmapImageROIDrawable));
            Document.ROIModel.AddRange(_step1FourierROIDrawables);
        });

        CIBDocument.RunDesign(() => CIBDocument.ImageModel.AddRange([_step0CIBBitmapImageDrawable, _step1CIBBitmapImageDrawable]));

        ResetDocument();
        ResetCIBDocument();
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

    private void ResetCIBDocument()
    {
        CIBDocument.Reset();

        foreach (var rod in Rods) _step1FourierROIDrawables[rod.Index].Text = $"{rod.Index + 1}";
    }

    #region Mapper

#pragma warning disable IDISP003

    public FourierSideChannelSpecularBlockerDTOItem Clone()
    {
        var item = new FourierSideChannelSpecularBlockerDTOItem(_rodTotalCount, ChannelId)
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

        foreach (var rod in Rods) rod.Reset();

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
                rod.ImageROI = rodResult.MinImageROI;
                if (rod.IsDeleted) rod.BitmapImageROIDrawable.Text = $"X {rod.BitmapImageROIDrawable.Text}";
                rod.BitmapImageROIDrawable.Rect = _step0FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(rod.ImageROI);
                rod.BitmapImageROIDrawable.IsVisible = rod.BitmapImageROIDrawable.Rect is { Width: > 0 };
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

            if (string.IsNullOrWhiteSpace(Step0FourierImageFilePath) || string.IsNullOrWhiteSpace(Step1FourierImageFilePath)) return;

            _step0FourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step0FourierImageFilePath);

            _step1FourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step1FourierImageFilePath);
            _step1FourierBitmapImageDrawable.Point = _step0FourierBitmapImageDrawable.Point - new Vector(0d, _step0FourierBitmapImageDrawable.BitmapImage.Height + 10d);

            foreach (var rod in Rods)
            {
                var step1ROI = _step1FourierROIDrawables[rod.Index];

                if (rod.IsDeleted) rod.BitmapImageROIDrawable.Text = step1ROI.Text = $"X {rod.BitmapImageROIDrawable.Text}";
                rod.BitmapImageROIDrawable.IsFixed = step1ROI.IsFixed = true;
                rod.BitmapImageROIDrawable.Rect = _step0FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(rod.ImageROI);
                rod.BitmapImageROIDrawable.IsVisible = step1ROI.IsVisible = rod.BitmapImageROIDrawable.Rect is { Width: > 0 };

                step1ROI.Rect = _step1FourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(rod.ImageROI);
            }
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

    #endregion

    public object ToImageHtmlAnonymous() => new
    {
        Comment = new HtmlComment(StepsComment),
        _rodTotalCount,
        ChannelId,
        Step0FourierImageFilePath,
        Step0FourierImage = new HtmlImage(Step0FourierImageFilePath)
    };

    public object ToHtmlAnonymous() => new
    {
        Comment = new HtmlComment(StepsComment),
        _rodTotalCount,
        ChannelId,
        Step0FourierImageFilePath,
        Step1FourierImageFilePath,
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
        ])
    };

    public object ToCIBHtmlAnonymous() => new
    {
        Comment = new HtmlComment(StepsComment),
        _rodTotalCount,
        ChannelId,
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
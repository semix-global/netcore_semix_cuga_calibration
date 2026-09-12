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
    private readonly BitmapImageDrawable _homeFourierBitmapImageDrawable = new();
    private readonly BitmapImageDrawable _blockedFourierBitmapImageDrawable = new();
    private readonly BitmapImageDrawable _homePMTBitmapImageDrawable = new();
    private readonly BitmapImageDrawable _blockedPMTBitmapImageDrawable = new();

    [Newtonsoft.Json.JsonProperty]
    private readonly int _rodTotalCount;

    [Newtonsoft.Json.JsonProperty]
    public int ChannelId { get; }

    [ObservableProperty]
    public partial string HomeFourierImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BlockedFourierImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HomePMTImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BlockedPMTImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double HomePMTImageAverageValue { get; set; }

    [ObservableProperty]
    public partial double BlockedPMTImageAverageValue { get; set; }

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
                .Select(i => new Rod(_homeFourierBitmapImageDrawable)
                {
                    Index = i,
                    BitmapImageROIDrawable = { ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.XCenterYMin }
                })
        ];

        Document.RunDesign(() =>
        {
            Document.ImageModel.AddRange(
            [
                _homeFourierBitmapImageDrawable,
                _blockedFourierBitmapImageDrawable,
                _homePMTBitmapImageDrawable,
                _blockedPMTBitmapImageDrawable
            ]);
            Document.ROIModel.AddRange(Rods.Select(t => t.BitmapImageROIDrawable));
        });

        ResetDocument();
    }

    public FourierSideChannelSpecularBlockerDTOItem() : this(FourierSideChannelFlexibleApertureDTO.DefaultRodTotalCount, 1)
    {
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
            HomeFourierImageFilePath = HomeFourierImageFilePath,
            BlockedFourierImageFilePath = BlockedFourierImageFilePath,
            HomePMTImageFilePath = HomePMTImageFilePath,
            BlockedPMTImageFilePath = BlockedPMTImageFilePath,
            HomePMTImageAverageValue = HomePMTImageAverageValue,
            BlockedPMTImageAverageValue = BlockedPMTImageAverageValue,
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
        HomeFourierImageFilePath = string.Empty;
        BlockedFourierImageFilePath = string.Empty;
        HomePMTImageFilePath = string.Empty;
        BlockedPMTImageFilePath = string.Empty;
        HomePMTImageAverageValue = 0d;
        BlockedPMTImageAverageValue = 0d;
        ExtinctionRatio = 0d;

        foreach (var rod in Rods) rod.Reset();

        ResetDocument();
    }

    public async Task CalibratingAsync(FourierSideChannelFlexibleApertureDTOItem fourierSideChannelFlexibleApertureItem, CancellationToken cancellationToken)
    {
        try
        {
            ResetDocument();

            Guard.IsNotNullOrWhiteSpace(HomeFourierImageFilePath);
            Guard.IsEqualTo(fourierSideChannelFlexibleApertureItem.RodResults.Length, _rodTotalCount);

            _homeFourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(HomeFourierImageFilePath);
            Document.View.ZoomToFit();

            foreach (var rod in Rods)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rodResult = fourierSideChannelFlexibleApertureItem.RodResults.Single(t => t.Index == rod.Index);

                rod.IsDeleted = rodResult.IsDeleted;
                rod.ImageROI = rodResult.MinImageROI
                    .ClampToBounds(new Rect(Point.Origin, _homeFourierBitmapImageDrawable.BitmapImage.Size));
                if (rod.IsDeleted) rod.BitmapImageROIDrawable.Text = $"X {rod.BitmapImageROIDrawable.Text}";
                rod.BitmapImageROIDrawable.IsFixed = false;
                rod.BitmapImageROIDrawable.Rect = _homeFourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(rod.ImageROI);
                rod.BitmapImageROIDrawable.IsVisible = rod.BitmapImageROIDrawable.Rect is { Width: > 0, Height: > 0 };
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var outputResult = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(_homeFourierBitmapImageDrawable)
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
                rod.ImageROI = _homeFourierBitmapImageDrawable.CartesianCoordinateToImageCoordinate(rod.BitmapImageROIDrawable.Rect);
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

            if (string.IsNullOrWhiteSpace(HomeFourierImageFilePath)
                || string.IsNullOrWhiteSpace(BlockedFourierImageFilePath)
                || string.IsNullOrWhiteSpace(HomePMTImageFilePath)
                || string.IsNullOrWhiteSpace(BlockedPMTImageFilePath)) return;

            _homeFourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(HomeFourierImageFilePath);

            _blockedFourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(BlockedFourierImageFilePath);
            _blockedFourierBitmapImageDrawable.Point = _homeFourierBitmapImageDrawable.Point - new Vector(0d, _homeFourierBitmapImageDrawable.BitmapImage.Height + 10d);

            _homePMTBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(HomePMTImageFilePath);
            _homePMTBitmapImageDrawable.Point = _homeFourierBitmapImageDrawable.Point + new Vector(_homeFourierBitmapImageDrawable.BitmapImage.Width + 10d, 0d);

            _blockedPMTBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(BlockedPMTImageFilePath);
            _blockedPMTBitmapImageDrawable.Point = _homePMTBitmapImageDrawable.Point - new Vector(0d, _homePMTBitmapImageDrawable.BitmapImage.Height + 10d);

            foreach (var rod in Rods)
            {
                if (rod.IsDeleted) rod.BitmapImageROIDrawable.Text = $"X {rod.BitmapImageROIDrawable.Text}";
                rod.BitmapImageROIDrawable.IsFixed = true;
                rod.BitmapImageROIDrawable.Rect = _homeFourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(rod.ImageROI);
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
        ChannelId,
        HomeFourierImageFilePath,
        HomePMTImageFilePath,
        HomeFourierImage = new HtmlImage(HomeFourierImageFilePath),
        HomePMTImage = new HtmlImage(HomePMTImageFilePath)
    };

    public object ToHtmlAnonymous() => new
    {
        ChannelId,
        HomePMTImageAverageValue,
        BlockedPMTImageAverageValue,
        ExtinctionRatio,
        HomeFourierImageFilePath,
        BlockedFourierImageFilePath,
        HomePMTImageFilePath,
        BlockedPMTImageFilePath,
        Rods = new HtmlTable([.. Rods.Select(t => new { t.Index, t.IsDeleted, t.ImageROI, t.MotorAbsoluteValue })]),
        HomeFourierImage = new HtmlImage(HomeFourierImageFilePath, htmlImageOverlays:
        [
            .. Rods.Select(t => new HtmlImageRectangleOverlay(t.ImageROI)),
            .. Rods.Select(t => new HtmlImageTextOverlay(t.ImageROI.Center, t.BitmapImageROIDrawable.Text))
        ]),
        BlockedFourierImage = new HtmlImage(BlockedFourierImageFilePath, htmlImageOverlays:
        [
            .. Rods.Select(t => new HtmlImageRectangleOverlay(t.ImageROI)),
            .. Rods.Select(t => new HtmlImageTextOverlay(t.ImageROI.Center, t.BitmapImageROIDrawable.Text))
        ]),
        HomePMTImage = new HtmlImage(HomePMTImageFilePath),
        BlockedPMTImage = new HtmlImage(BlockedPMTImageFilePath)
    };

    public void Dispose()
    {
        _homeFourierBitmapImageDrawable.Dispose();
        _blockedFourierBitmapImageDrawable.Dispose();
        _homePMTBitmapImageDrawable.Dispose();
        _blockedPMTBitmapImageDrawable.Dispose();
    }
}
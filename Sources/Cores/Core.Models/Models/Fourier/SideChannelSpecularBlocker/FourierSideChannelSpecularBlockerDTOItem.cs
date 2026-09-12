using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Fourier.SideChannelFlexibleAperture;
using MathNet.Numerics;
using Net.Utilities.Calibration;
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

    [Newtonsoft.Json.JsonProperty]
    public FourierSideChannelFlexibleApertureDTOItem.Rod[] Rods { get; }

    [Newtonsoft.Json.JsonProperty]
    public double[] MoveDownPercents { get; private set; }

    [ObservableProperty]
    public partial double ExtinctionRatio { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public OpticsFourierImageDocument Document { get; } = new();

    public FourierSideChannelSpecularBlockerDTOItem(int rodTotalCount, int channelId)
    {
        _rodTotalCount = rodTotalCount;
        ChannelId = channelId;
        MoveDownPercents = new double[rodTotalCount];
        Rods =
        [
            .. Generate.LinearRangeInt32(0, rodTotalCount - 1)
                .Select(i => new FourierSideChannelFlexibleApertureDTOItem.Rod(_homeFourierBitmapImageDrawable)
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

    public FourierSideChannelSpecularBlockerDTOItem() : this(FourierSideChannelSpecularBlockerDTO.DefaultRodTotalCount, 1)
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
            ExtinctionRatio = ExtinctionRatio,
            MoveDownPercents = [.. MoveDownPercents]
        };

        foreach (var (target, source) in item.Rods.Zip(Rods)) target.AdaptIn(source);

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
        ExtinctionRatio = 0d;
        MoveDownPercents = new double[_rodTotalCount];

        foreach (var rod in Rods) rod.Reset();

        ResetDocument();
    }

    public async Task CalibratingAsync(IReadOnlyList<FourierSideChannelFlexibleApertureDTOItem.RodResult> rodResults, CancellationToken cancellationToken)
    {
        try
        {
            ResetDocument();

            Guard.IsNotNullOrWhiteSpace(HomeFourierImageFilePath);
            Guard.IsEqualTo(rodResults.Count, _rodTotalCount);

            _homeFourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(HomeFourierImageFilePath);
            var homeFourierImage = Guard.IsNotNullAndReturn(_homeFourierBitmapImageDrawable.BitmapImage);
            LoadOptionalImages();
            Document.View.ZoomToFit();

            foreach (var rod in Rods)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rodResult = rodResults.Single(t => t.Index == rod.Index);
                Guard.IsTrue(rodResult.MinImageROI is { Width: > 0d, Height: > 0d });

                rod.IsDeleted = false;
                rod.ImageROI = rodResult.MinImageROI
                    .ImageCoordinateRound()
                    .ClampToBounds(new Rect(Point.Origin, homeFourierImage.Size));
                rod.BitmapImageROIDrawable.IsFixed = false;
                rod.BitmapImageROIDrawable.Rect = _homeFourierBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(rod.ImageROI);
                rod.BitmapImageROIDrawable.IsVisible = rod.BitmapImageROIDrawable.Rect is { Width: > 0, Height: > 0 };
            }

            Document.View.SetViewBounds(_homeFourierBitmapImageDrawable.GetExtents());

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var outputResult = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(_homeFourierBitmapImageDrawable)
                {
                    BitmapImageROIDragMoveTypeEnum = BitmapImageROIDragMoveTypeEnum.None,
                    IsDeleteEnabled = true,
                    CancellationToken = cancellationToken
                });

                switch (outputResult)
                {
                    case { OutputResultModeEnum: OutputResultModeEnum.Ok }:
                        try
                        {
                            FourierSideChannelFlexibleApertureDTOItem.Rod[] temps = [.. Rods.Where(t => t.BitmapImageROIDrawable.IsVisible).OrderBy(t => t.Index)];
                            Guard.IsGreaterThanOrEqualTo(temps.Length, 1);

                            foreach (var rod in temps)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                Guard.IsTrue(rod.BitmapImageROIDrawable.Rect is { Height: > 0d });
                            }

                            goto OuterLoop;
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            if (ShouldContinue(ex)) continue;

                            throw;
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

            var moveDownPercents = new double[_rodTotalCount];

            foreach (var rod in Rods)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rodResult = rodResults.Single(t => t.Index == rod.Index);

                rod.IsDeleted = rod.BitmapImageROIDrawable.IsVisible == false;
                if (rod.IsDeleted)
                {
                    rod.BitmapImageROIDrawable.Text = $"X {rod.BitmapImageROIDrawable.Text}";
                    moveDownPercents[rod.Index] = 0d;

                    continue;
                }

                rod.BitmapImageROIDrawable.IsFixed = true;
                rod.ImageROI = _homeFourierBitmapImageDrawable.CartesianCoordinateToImageCoordinate(rod.BitmapImageROIDrawable.Rect);
                moveDownPercents[rod.Index] = (rod.ImageROI.Height - rodResult.MinImageROI.Height) * rodResult.PixelSize;
            }

            MoveDownPercents = moveDownPercents;
        }
        finally
        {
            Document.View.ZoomToFit();
        }

        return;

        static bool ShouldContinue(Exception ex)
        {
            var dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();

            return dialogWindowProvider.TryShowDialog($"""
                                                       Error: {ex.Message}

                                                       Yes: continue to modify ROI.
                                                       No: abort calibration.
                                                       """, out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Warning) == true
                   && dialogResult == DialogResultEnum.Yes;
        }
    }

    public void Review()
    {
        try
        {
            ResetDocument();

            if (string.IsNullOrWhiteSpace(HomeFourierImageFilePath)) return;

            _homeFourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(HomeFourierImageFilePath);
            LoadOptionalImages();

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

    public void RefreshExtinctionRatio()
    {
        Guard.IsNotNullOrWhiteSpace(HomePMTImageFilePath);
        Guard.IsNotNullOrWhiteSpace(BlockedPMTImageFilePath);

        using var homePMTImage = BitmapHelper.OpenImage(HomePMTImageFilePath);
        using var blockedPMTImage = BitmapHelper.OpenImage(BlockedPMTImageFilePath);

        var homeMean = homePMTImage.GetIntensity().Average;
        Guard.IsNotEqualTo(homeMean, 0d);

        ExtinctionRatio = blockedPMTImage.GetIntensity().Average / homeMean;
    }

    public List<(int rodnumber, double rodpos)> ToRodPositions(double minMotorAbsoluteValue, double maxMotorAbsoluteValue) =>
    [
        .. Rods.Select(t => (t.Index, Math.Clamp(
            t.IsDeleted ? minMotorAbsoluteValue : minMotorAbsoluteValue + MoveDownPercents[t.Index],
            minMotorAbsoluteValue,
            maxMotorAbsoluteValue)))
    ];

    #endregion

    public object ToImageHtmlAnonymous() => new
    {
        HomeFourierImageFilePath,
        BlockedFourierImageFilePath,
        HomePMTImageFilePath,
        BlockedPMTImageFilePath,
        HomeFourierImage = new HtmlImage(HomeFourierImageFilePath),
        BlockedFourierImage = new HtmlImage(BlockedFourierImageFilePath),
        HomePMTImage = new HtmlImage(HomePMTImageFilePath),
        BlockedPMTImage = new HtmlImage(BlockedPMTImageFilePath)
    };

    public object ToHtmlAnonymous() => new
    {
        ChannelId,
        ExtinctionRatio,
        HomeFourierImageFilePath,
        BlockedFourierImageFilePath,
        HomePMTImageFilePath,
        BlockedPMTImageFilePath,
        Rods = new HtmlTable([.. Rods.Select(t => new { t.Index, t.IsDeleted, t.ImageROI, MoveDownPercent = MoveDownPercents.ElementAtOrDefault(t.Index) })]),
        HomeFourierImage = new HtmlImage(HomeFourierImageFilePath, htmlImageOverlays:
        [
            .. Rods.Select(t => new HtmlImageRectangleOverlay(t.ImageROI)),
            .. Rods.Select(t => new HtmlImageTextOverlay(t.ImageROI.Center, t.BitmapImageROIDrawable.Text))
        ]),
        BlockedFourierImage = new HtmlImage(BlockedFourierImageFilePath),
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

    private void LoadOptionalImages()
    {
        var current = _homeFourierBitmapImageDrawable;
        var currentImage = Guard.IsNotNullAndReturn(current.BitmapImage);

        if (string.IsNullOrWhiteSpace(BlockedFourierImageFilePath) == false)
        {
            _blockedFourierBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(BlockedFourierImageFilePath);
            _blockedFourierBitmapImageDrawable.Point = current.Point - new Vector(0d, currentImage.Height + 10d);
            current = _blockedFourierBitmapImageDrawable;
            currentImage = Guard.IsNotNullAndReturn(current.BitmapImage);
        }

        if (string.IsNullOrWhiteSpace(HomePMTImageFilePath) == false)
        {
            _homePMTBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(HomePMTImageFilePath);
            _homePMTBitmapImageDrawable.Point = current.Point - new Vector(0d, currentImage.Height + 10d);
            current = _homePMTBitmapImageDrawable;
            currentImage = Guard.IsNotNullAndReturn(current.BitmapImage);
        }

        if (string.IsNullOrWhiteSpace(BlockedPMTImageFilePath) == false)
        {
            _blockedPMTBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(BlockedPMTImageFilePath);
            _blockedPMTBitmapImageDrawable.Point = current.Point - new Vector(0d, currentImage.Height + 10d);
        }
    }
}

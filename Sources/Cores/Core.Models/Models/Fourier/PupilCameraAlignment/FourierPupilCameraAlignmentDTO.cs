using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Wcf.Models.Fourier;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Enums.Editors;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.OpticsFourierImageViewer.WPF;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Editors;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;
using System.IO;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;

namespace Core.Models.Models.Fourier.PupilCameraAlignment;

[CacheVersion("2.0.0")]
public sealed partial class FourierPupilCameraAlignmentDTO : CalibrationDTOBase<FourierPupilCameraAlignmentDTO>, IAdaptTo<CalibrationPupilCameraAlignment>, IDisposable
{
    [ObservableProperty]
    public partial FourierPupilCameraAlignmentDTOItem Channel1Item { get; set; } = new() { ChannelId = 1 };

    [ObservableProperty]
    public partial FourierPupilCameraAlignmentDTOItem Channel2Item { get; set; } = new() { ChannelId = 2 };

    [ObservableProperty]
    public partial FourierPupilCameraAlignmentDTOItem Channel3Item { get; set; } = new() { ChannelId = 3 };

    #region Mapper

    public override FourierPupilCameraAlignmentDTO Clone() => new()
    {
        Channel1Item = Channel1Item.Clone(),
        Channel2Item = Channel2Item.Clone(),
        Channel3Item = Channel3Item.Clone(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationPupilCameraAlignment AdaptTo() => new()
    {
        RectCh1 = Channel1Item.ImageROI.ToRectD(),
        RectCh2 = Channel2Item.ImageROI.ToRectD(),
        RectCh3 = Channel3Item.ImageROI.ToRectD(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper

    public void Dispose()
    {
        Channel1Item.Dispose();
        Channel2Item.Dispose();
        Channel3Item.Dispose();
    }
}

public sealed partial class FourierPupilCameraAlignmentDTOItem : ObservableObject, ICloneable<FourierPupilCameraAlignmentDTOItem>, IDisposable
{
    private readonly BitmapImageDrawable _originalBitmapImageDrawable = new();
    private readonly BitmapImageDrawable _roiBitmapImageDrawable = new();
    private readonly BitmapImageROIDrawable _bitmapImageROIDrawable;

    [ObservableProperty]
    public partial int ChannelId { get; set; }

    [ObservableProperty]
    public partial string ChannelImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ROIChannelImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Rect ImageROI { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public OpticsFourierImageDocument Document { get; } = new();

    public FourierPupilCameraAlignmentDTOItem()
    {
        _bitmapImageROIDrawable = new BitmapImageROIDrawable(_originalBitmapImageDrawable);

        Document.RunDesign(() =>
        {
            Document.ImageModel.AddRange([_originalBitmapImageDrawable, _roiBitmapImageDrawable]);
            Document.ROIModel.Add(_bitmapImageROIDrawable);
        });
    }

    #region Mapper

    public FourierPupilCameraAlignmentDTOItem Clone() => new()
    {
        ChannelId = ChannelId,
        ChannelImageFilePath = ChannelImageFilePath,
        ROIChannelImageFilePath = ROIChannelImageFilePath,
        ImageROI = ImageROI
    };

    public object ToImageHtmlAnonymous() => new
    {
        ChannelImageFilePath,
        Image = new HtmlImage(ChannelImageFilePath, htmlImageOverlays:
        [
            new HtmlImageRectangleOverlay(ImageROI)
        ])
    };

    public object ToHtmlAnonymous() => new
    {
        ChannelImageFilePath,
        ROIChannelImageFilePath,
        ImageROI,
        Image = new HtmlImage(ChannelImageFilePath, htmlImageOverlays:
        [
            new HtmlImageRectangleOverlay(ImageROI)
        ]),
        ROIImage = new HtmlImage(ROIChannelImageFilePath)
    };

    #endregion

    #region 校准

    public void Reset()
    {
        ChannelImageFilePath = string.Empty;
        ROIChannelImageFilePath = string.Empty;
        ImageROI = Rect.Empty;

        Document.ResetEditROI();
    }

    public async Task CalibratingAsync(CancellationToken cancellationToken)
    {
        try
        {
            Guard.IsNotNullOrWhiteSpace(ChannelImageFilePath);

            _originalBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ChannelImageFilePath);
            var roiSize = (Size)_originalBitmapImageDrawable.BitmapImage.Size / 2d;
            _bitmapImageROIDrawable.Rect = _originalBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(new Rect((Point)roiSize - (Vector)roiSize / 2d, roiSize)).ImageCoordinateRound();
            Document.View.ZoomToFit();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var outputResult = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(_originalBitmapImageDrawable)
                {
                    BitmapImageROIDragMoveTypeEnum = BitmapImageROIDragMoveTypeEnum.All,
                    IsDeleteEnabled = false,
                    CancellationToken = cancellationToken
                });
                switch (outputResult)
                {
                    case { OutputResultModeEnum: OutputResultModeEnum.Ok }:
                        try
                        {
                            Guard.IsTrue(_bitmapImageROIDrawable.Rect is { Width: > 0d, Height: > 0d });

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

            _bitmapImageROIDrawable.IsFixed = true;
            ImageROI = _originalBitmapImageDrawable.CartesianCoordinateToImageCoordinate(_bitmapImageROIDrawable.Rect);
            Guard.IsEqualTo(_originalBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(ImageROI), _bitmapImageROIDrawable.Rect);

            ROIChannelImageFilePath = Path.Combine(FileHelper.GetFileFullName(ChannelImageFilePath), $"ROI_{ImageROI}_{Path.GetFileName(ChannelImageFilePath)}");

            _roiBitmapImageDrawable.Point = _originalBitmapImageDrawable.Point + new Vector(_originalBitmapImageDrawable.BitmapImage.Size.Width + 10d, 0d);
            _roiBitmapImageDrawable.BitmapImage = _originalBitmapImageDrawable.BitmapImage.ToROI(ImageROI);
            _roiBitmapImageDrawable.BitmapImage.SaveImage(ROIChannelImageFilePath);
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
            Document.ResetEditROI();

            if (string.IsNullOrWhiteSpace(ChannelImageFilePath) || string.IsNullOrWhiteSpace(ROIChannelImageFilePath)) return;

            _originalBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ChannelImageFilePath);

            _roiBitmapImageDrawable.Point = _originalBitmapImageDrawable.Point + new Vector(_originalBitmapImageDrawable.BitmapImage.Size.Width + 10d, 0d);
            _roiBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ROIChannelImageFilePath);

            _bitmapImageROIDrawable.IsFixed = true;
            _bitmapImageROIDrawable.Rect = _originalBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(ImageROI);
        }
        finally
        {
            Document.View.ZoomToFit();
        }
    }

    #endregion

    public void Dispose()
    {
        _originalBitmapImageDrawable.Dispose();
        _roiBitmapImageDrawable.Dispose();
    }
}
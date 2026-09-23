using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
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
using System.IO;

namespace Core.Models.Models.Fourier.PupilCameraAlignment;

public sealed partial class FourierPupilCameraAlignmentDTOItem : ObservableObject, ICloneable<FourierPupilCameraAlignmentDTOItem>, IDisposable
{
    private readonly BitmapImageDrawable _originalBitmapImageDrawable = new();
    private readonly BitmapImageDrawable _roiBitmapImageDrawable = new();

    public int ChannelId { get; }

    [ObservableProperty]
    public partial string ChannelImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ROIChannelImageFilePath { get; set; } = string.Empty;

    public FourierPupilCameraAlignmentDTO.ROI ROI { get; }

    [Newtonsoft.Json.JsonIgnore]
    public OpticsFourierImageDocument Document { get; } = new();

    public FourierPupilCameraAlignmentDTOItem(int channelId)
    {
        ChannelId = channelId;

        ROI = new FourierPupilCameraAlignmentDTO.ROI(_originalBitmapImageDrawable)
        {
            BitmapImageROIDrawable = { ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.All }
        };

        Document.RunDesign(() =>
        {
            Document.ImageModel.AddRange([_originalBitmapImageDrawable, _roiBitmapImageDrawable]);
            Document.ROIModel.Add(ROI.BitmapImageROIDrawable);
        });

        Document.Reset();
    }

    [Newtonsoft.Json.JsonConstructor]
    private FourierPupilCameraAlignmentDTOItem(
        int channelId,
        [Newtonsoft.Json.JsonProperty(nameof(ROI))]
        FourierPupilCameraAlignmentDTO.ROI roi)
        : this(channelId)
    {
        ROI.AdaptIn(roi);
    }

    #region Mapper

    public FourierPupilCameraAlignmentDTOItem Clone()
    {
        var item = new FourierPupilCameraAlignmentDTOItem(ChannelId)
        {
            ChannelImageFilePath = ChannelImageFilePath,
            ROIChannelImageFilePath = ROIChannelImageFilePath,
        };
        item.ROI.AdaptIn(ROI);

        return item;
    }

    #endregion Mapper

    #region 校准

    public void Reset()
    {
        ChannelImageFilePath = string.Empty;
        ROIChannelImageFilePath = string.Empty;

        ROI.Reset();

        Document.Reset();
    }

    public async Task CalibratingAsync(CancellationToken cancellationToken)
    {
        try
        {
            Document.Reset();

            Guard.IsNotNullOrWhiteSpace(ChannelImageFilePath);

            _originalBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ChannelImageFilePath);

        InnerLoop:

            var roiSize = (Size)_originalBitmapImageDrawable.BitmapImage.Size / 2d;
            ROI.BitmapImageROIDrawable.Rect = _originalBitmapImageDrawable
                .ImageCoordinateToCartesianCoordinate(new Rect((Point)roiSize - (Vector)roiSize / 2d, roiSize))
                .ImageCoordinateRound();
            Document.View.ZoomToFit();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var outputResult = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(_originalBitmapImageDrawable)
                {
                    Title = $"Select the Channel{ChannelId} pupil camera ROI.",
                    BitmapImageROIDragMoveTypeEnum = BitmapImageROIDragMoveTypeEnum.All,
                    IsDeleteEnabled = false,
                    CancellationToken = cancellationToken
                });

                switch (outputResult)
                {
                    case { OutputResultModeEnum: OutputResultModeEnum.Ok }:
                        try
                        {
                            Guard.IsTrue(ROI.BitmapImageROIDrawable.IsVisible);

                            Guard.IsTrue(ROI.BitmapImageROIDrawable.Rect is { Width: > 0d, Height: > 0d });

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

            ROI.ImageROI = _originalBitmapImageDrawable.CartesianCoordinateToImageCoordinate(ROI.BitmapImageROIDrawable.Rect);
            ROI.BitmapImageROIDrawable.IsFixed = true;
            Guard.IsEqualTo(_originalBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(ROI.ImageROI), ROI.BitmapImageROIDrawable.Rect);

            ROIChannelImageFilePath = Path.Combine(Path.GetDirectoryName(ChannelImageFilePath) ?? string.Empty, $"{Path.GetFileNameWithoutExtension(ChannelImageFilePath)}_ROI_{ROI.ImageROI}{Path.GetExtension(ChannelImageFilePath)}");

            _roiBitmapImageDrawable.Point = _originalBitmapImageDrawable.Point + new Vector(_originalBitmapImageDrawable.BitmapImage.Size.Width + 10d, 0d);
            _roiBitmapImageDrawable.BitmapImage = _originalBitmapImageDrawable.BitmapImage.ToROI(ROI.ImageROI);
            _roiBitmapImageDrawable.BitmapImage.SaveImage(ROIChannelImageFilePath);
        }
        finally
        {
            Document.View.ZoomToFit();
        }

        return;

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

            if (string.IsNullOrWhiteSpace(ChannelImageFilePath) || string.IsNullOrWhiteSpace(ROIChannelImageFilePath)) return;

            _originalBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ChannelImageFilePath);

            _roiBitmapImageDrawable.Point = _originalBitmapImageDrawable.Point + new Vector(_originalBitmapImageDrawable.BitmapImage.Size.Width + 10d, 0d);
            _roiBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ROIChannelImageFilePath);

            ROI.BitmapImageROIDrawable.IsFixed = true;
            ROI.BitmapImageROIDrawable.Rect = _originalBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(ROI.ImageROI);
        }
        finally
        {
            Document.View.ZoomToFit();
        }
    }

    #endregion 校准

    public object ToImageHtmlAnonymous() => new
    {
        ChannelId,
        ChannelImageFilePath,
        Image = new HtmlImage(ChannelImageFilePath)
    };

    public object ToHtmlAnonymous() => new
    {
        ChannelId,
        ChannelImageFilePath,
        ROIChannelImageFilePath,
        ROI.ImageROI,
        Image = new HtmlImage(ChannelImageFilePath, htmlImageOverlays:
        [
            new HtmlImageRectangleOverlay(ROI.ImageROI)
        ]),
        ROIImage = new HtmlImage(ROIChannelImageFilePath)
    };

    public void Dispose()
    {
        _originalBitmapImageDrawable.Dispose();
        _roiBitmapImageDrawable.Dispose();
    }
}
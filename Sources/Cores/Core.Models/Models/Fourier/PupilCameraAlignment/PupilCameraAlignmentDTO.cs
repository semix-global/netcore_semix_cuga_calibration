using System.IO;
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
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

namespace Core.Models.Models.Fourier.PupilCameraAlignment;

[CacheVersion("2.0.0")]
public sealed partial class PupilCameraAlignmentDTO : CalibrationDTOBase<PupilCameraAlignmentDTO>, IAdaptTo<CalibrationPupilCameraAlignment>, IDisposable
{
    [ObservableProperty]
    public partial PupilCameraAlignmentDTOItem Channel1Item { get; set; } = new() { ChannelId = 1 };

    [ObservableProperty]
    public partial PupilCameraAlignmentDTOItem Channel2Item { get; set; } = new() { ChannelId = 2 };

    [ObservableProperty]
    public partial PupilCameraAlignmentDTOItem Channel3Item { get; set; } = new() { ChannelId = 3 };

    #region Mapper

    public override PupilCameraAlignmentDTO Clone() => new()
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

public sealed partial class PupilCameraAlignmentDTOItem : ObservableObject, ICloneable<PupilCameraAlignmentDTOItem>, IDisposable
{
    private readonly BitmapImageDrawable _bitmapImageDrawable;
    private readonly BitmapImageDrawable _roiBitmapImageDrawable;
    private readonly BitmapImageROIDrawable _bitmapImageROIDrawable;

    [ObservableProperty]
    public partial int ChannelId { get; set; }

    [ObservableProperty]
    public partial string ChannelImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ROIChannelImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Rect ImageROI { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial OpticsFourierImageDocument Document { get; set; }

    public PupilCameraAlignmentDTOItem()
    {
        _bitmapImageDrawable = new BitmapImageDrawable();
        _roiBitmapImageDrawable = new BitmapImageDrawable();
        _bitmapImageROIDrawable = new BitmapImageROIDrawable(_bitmapImageDrawable)
        {
            ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.All
        };

        Document = new OpticsFourierImageDocument();
        Document.RunDesign(() =>
        {
            Document.ImageModel.AddRange([_bitmapImageDrawable, _roiBitmapImageDrawable]);
            Document.ROIModel.Add(_bitmapImageROIDrawable);
        });
    }

    public async Task CalibratingAsync(CancellationToken cancellationToken)
    {
        try
        {
            Guard.IsNotNullOrWhiteSpace(ChannelImageFilePath);

            _bitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ChannelImageFilePath);
            var roiSize = (Size)_bitmapImageDrawable.BitmapImage.Size / 2d;
            _bitmapImageROIDrawable.Rect = _bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(new Rect((Point)roiSize - (Vector)roiSize / 2d, roiSize));
            Document.View.ZoomToFit();

            var options = new ModifyBitmapImageROIDrawableInputOptions(_bitmapImageDrawable)
            {
                BitmapImageROIDragMoveTypeEnum = BitmapImageROIDragMoveTypeEnum.All,
                CancellationToken = cancellationToken
            };

            var outputResult = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, options);
            Guard.IsTrue(outputResult.OutputResultModeEnum == OutputResultModeEnum.Ok, outputResult.ErrorMessage);
            Guard.IsTrue(_bitmapImageROIDrawable.Rect is { Width: > 0d, Height: > 0d });

            ImageROI = _bitmapImageDrawable.CartesianCoordinateToImageCoordinate(_bitmapImageROIDrawable.Rect);

            ROIChannelImageFilePath = Path.Combine(FileHelper.GetFileFullName(ChannelImageFilePath), $"ROI_{ImageROI}_{Path.GetFileName(ChannelImageFilePath)}");

            _roiBitmapImageDrawable.Point += new Vector(_bitmapImageDrawable.BitmapImage.Size.Width + 10d, 0d);
            _roiBitmapImageDrawable.BitmapImage = _bitmapImageDrawable.BitmapImage.ToROI(ImageROI);
            _roiBitmapImageDrawable.BitmapImage.SaveImage(ROIChannelImageFilePath);
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
            if (string.IsNullOrWhiteSpace(ChannelImageFilePath) || string.IsNullOrWhiteSpace(ROIChannelImageFilePath)) return;

            _bitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ChannelImageFilePath);
            _bitmapImageROIDrawable.Rect = _bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(ImageROI).ClampToBounds(new Rect(_bitmapImageDrawable.Point, _bitmapImageDrawable.BitmapImage.Size));

            _roiBitmapImageDrawable.Point += new Vector(_bitmapImageDrawable.BitmapImage.Size.Width + 10d, 0d);
            _roiBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ROIChannelImageFilePath);
        }
        finally
        {
            Document.View.ZoomToFit();
        }
    }

    public PupilCameraAlignmentDTOItem Clone() => new()
    {
        ChannelId = ChannelId,
        ChannelImageFilePath = ChannelImageFilePath,
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

    public void Dispose()
    {
        _bitmapImageDrawable.Dispose();
        _roiBitmapImageDrawable.Dispose();
    }
}
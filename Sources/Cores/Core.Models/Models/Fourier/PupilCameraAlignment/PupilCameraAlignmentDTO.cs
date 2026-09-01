using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Wcf.Models.Fourier;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Enums.Editors;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Editors;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

namespace Core.Models.Models.Fourier.PupilCameraAlignment;

[CacheVersion("2.0.0")]
public sealed partial class PupilCameraAlignmentDTO : CalibrationDTOBase<PupilCameraAlignmentDTO>, IAdaptTo<CalibrationPupilCameraAlignment>
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
}

public sealed partial class PupilCameraAlignmentDTOItem : ObservableObject, ICloneable<PupilCameraAlignmentDTOItem>
{
    private readonly BitmapImageDrawable _bitmapImageDrawable;
    private readonly BitmapImageROIDrawable _bitmapImageROIDrawable;

    [ObservableProperty]
    public partial int ChannelId { get; set; }

    [ObservableProperty]
    public partial string ChannelImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Rect ImageROI { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial OpticsFourierImageDocument Document { get; set; }

    public PupilCameraAlignmentDTOItem()
    {
        _bitmapImageDrawable = new BitmapImageDrawable();
        _bitmapImageROIDrawable = new BitmapImageROIDrawable(_bitmapImageDrawable)
        {
            ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.All
        };

        Document = new OpticsFourierImageDocument();
        using var scope = Document.View.Sync.EnterScope();

        Document.ImageModel.Add(_bitmapImageDrawable);
        Document.ROIModel.Add(_bitmapImageROIDrawable);
    }

    public async Task CalibratingAsync(string channelImageFilePath, CancellationToken cancellationToken)
    {
        ChannelImageFilePath = channelImageFilePath;
        _bitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ChannelImageFilePath);
        _bitmapImageROIDrawable.Rect = _bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(
            new Rect((Point)(PointI)_bitmapImageDrawable.BitmapImage.Size / 2d, (Size)_bitmapImageDrawable.BitmapImage.Size / 2d));
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
    }

    public void Review()
    {
        if (string.IsNullOrWhiteSpace(ChannelImageFilePath)) return;

        _bitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ChannelImageFilePath);
        _bitmapImageROIDrawable.Rect = _bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(ImageROI).ClampToBounds(new Rect(_bitmapImageDrawable.Point, _bitmapImageDrawable.BitmapImage.Size));

        Document.View.ZoomToFit();
    }

    public PupilCameraAlignmentDTOItem Clone() => new()
    {
        ChannelId = ChannelId,
        ChannelImageFilePath = ChannelImageFilePath,
        ImageROI = ImageROI
    };
}
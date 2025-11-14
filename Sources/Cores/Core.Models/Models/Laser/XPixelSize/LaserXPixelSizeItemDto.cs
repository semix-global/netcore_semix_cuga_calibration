using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.XPixelSize;

public sealed partial class LaserXPixelSizeItemDto : CalibrationDtoBase, ICloneable<LaserXPixelSizeItemDto>, IAdaptTo<CalibrationLaserXPixelSizeItem>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private int _pMTId;

    [ObservableProperty]
    private int _channelId;

    [ObservableProperty]
    private double _xPixelSize;

    [ObservableProperty]
    private string _rawImageFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<(Point MatchPoint, double Score, bool IsMatchOk)> _slideItems = [];

    [ObservableProperty]
    private IReadOnlyList<(Point MatchPoint, double Score, string ImageFilePath)> _verifyItems = [];

    #region Mapper

    public LaserXPixelSizeItemDto Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation,
        ProductivityInformation = ProductivityInformation.Clone(),
        XPixelSize = XPixelSize,
        RawImageFilePath = RawImageFilePath,
        SlideItems = [.. SlideItems],
        VerifyItems = [.. VerifyItems],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserXPixelSizeItem AdaptTo() => new()
    {
        CgMagTypeEnum = ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum(),
        Speed = ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType(),
        XPixelSize = XPixelSize,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified
    };

    #endregion Mapper
}

public sealed record Item(long StartPixel, byte[] Buffer, SizeI Size)
{
    public Point MatchPoint { get; set; }

    public double Score { get; set; }

    public bool IsOk { get; set; }
}
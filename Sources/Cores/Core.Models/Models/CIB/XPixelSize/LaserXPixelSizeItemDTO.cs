using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.XPixelSize;

public sealed partial class LaserXPixelSizeItemDTO : CalibrationDtoBase, ICloneable<LaserXPixelSizeItemDTO>, IAdaptTo<CalibrationLaserXPixelSizeItem>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private double _xPixelSize;

    [ObservableProperty]
    private string _rawImageFilePath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SlideItemPoints))]
    private IReadOnlyList<LaserXPixelSizeItemDTOSlideItem> _slideItems = [];

    [ObservableProperty]
    private IReadOnlyList<double> _slideSplitDifferences = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VerifyItemPoints))]
    private IReadOnlyList<LaserXPixelSizeItemDTOSlideItem> _verifyItems = [];

    [ObservableProperty]
    private string _verifyRawImageFilePath = string.Empty;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public IReadOnlyList<Point> SlideItemPoints => [.. SlideItems.Select(t => new Point(t.MatchPoint.X, t.Score))];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public IReadOnlyList<Point> VerifyItemPoints => [.. VerifyItems.Select(t => new Point(t.MatchPoint.X, t.Score))];

    [ObservableProperty]
    private IReadOnlyList<double> _verifySplitDifferences = [];

    #region Mapper

    public LaserXPixelSizeItemDTO Clone() => new()
    {
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        ProductivityInformation = ProductivityInformation.Clone(),
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        CIBInformation = CIBInformation.Clone(),
        XPixelSize = XPixelSize,
        RawImageFilePath = RawImageFilePath,
        SlideItems = [.. SlideItems],
        SlideSplitDifferences = [.. SlideSplitDifferences],
        VerifyItems = [.. VerifyItems],
        VerifySplitDifferences = [.. VerifySplitDifferences],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserXPixelSizeItem AdaptTo() => new()
    {
        CgNIOITypeEnum = OpticsIlluminationModeEnum.ToCgNIOITypeEnum(),
        CgMagTypeEnum = ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum(),
        Speed = ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType(),
        XPixelSize = XPixelSize,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified
    };

    #endregion Mapper
}

public sealed class LaserXPixelSizeItemDTOSlideItem
{
    public long StartPixel { get; init; }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public byte[] Buffer { get; init; } = [];

    public SizeI SizeI { get; init; }

    public Point MatchPoint { get; set; }

    public double Score { get; set; }

    public string ImageFilePath { get; set; } = string.Empty;

    public bool IsMatchOk { get; set; }
}
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.CIB.XPixelSize;

public sealed partial class CIBXPixelSizeDTO : CalibrationDtoBase, ICloneable<CIBXPixelSizeDTO>, IAdaptTo<CalibrationLaserXPixelSizeItem>
{
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
    private IReadOnlyList<CIBXPixelSizeDTOItem> _slideItems = [];

    [ObservableProperty]
    private IReadOnlyList<double> _slideSplitDifferences = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VerifyItemPoints))]
    private IReadOnlyList<CIBXPixelSizeDTOItem> _verifyItems = [];

    [ObservableProperty]
    private string _verifyRawImageFilePath = string.Empty;

    [ObservableProperty]
    private double _xPixelSizeDelta;

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

    public CIBXPixelSizeDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        CIBInformation = CIBInformation.Clone(),
        XPixelSize = XPixelSize,
        XPixelSizeDelta = XPixelSizeDelta,
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
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Speed = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,
        XPixelSize = XPixelSize,
        XPixelSizeDelta = XPixelSizeDelta,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified
    };

    #endregion Mapper
}

public sealed class CIBXPixelSizeDTOItem
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
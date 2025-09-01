using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.XPixelSize;

public sealed partial class LaserXPixelSizeItemDto : CalibrationDtoBase, ICloneable<LaserXPixelSizeItemDto>, IAdaptTo<CalibrationLaserXPixelSizeItem>
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation =  MicroscopeLensInformation.Default;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private StageSpeedEnum _xStageSpeedEnum;

    [ObservableProperty]
    private int _pmtId;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private Point _findStartPosition;

    [ObservableProperty]
    private Point _findEndPosition;

    [ObservableProperty]
    private double _xPixelSize;

    [ObservableProperty]
    private string _fileTemplatePath = String.Empty;

    [ObservableProperty]
    private string _filePath = String.Empty;

    [ObservableProperty]
    private string _originalFilePath = String.Empty;

    [ObservableProperty]
    private List<DarkFieldXPixelSizeICropImage> _darkFieldCropImageList = [];

    #region Mapper

    public LaserXPixelSizeItemDto Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation,
        OpticsMagTypeEnum = OpticsMagTypeEnum,
        XStageSpeedEnum = XStageSpeedEnum,
        PmtId = PmtId,
        FindPosition = FindPosition,
        FindStartPosition = FindStartPosition,
        FindEndPosition = FindEndPosition,
        XPixelSize = XPixelSize,
        FilePath = FilePath,
        FileTemplatePath = FileTemplatePath,
        DarkFieldCropImageList = [.. DarkFieldCropImageList.Select(x => x.Clone())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserXPixelSizeItem AdaptTo() => new()
    {
        CgMagTypeEnum = OpticsMagTypeEnum.ToCgMagTypeEnum(),
        Speed = XStageSpeedEnum.ToAdsSpeedEnum(),
        XPixelSize = XPixelSize,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified
    };

    #endregion Mapper
}

public sealed partial class DarkFieldXPixelSizeICropImage : ObservableObject, ICloneable<DarkFieldXPixelSizeICropImage>
{
    [ObservableProperty]
    private Point _position;

    [ObservableProperty]
    private byte[] _byteArray = [];

    [ObservableProperty]
    private double _width;

    [ObservableProperty]
    private double _height;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private List<double> _darkFieldImageList = [];

    public DarkFieldXPixelSizeICropImage Clone() => new()
    {
        Position = Position,
        ByteArray = ByteArray,
        Width = Width,
        Height = Height,
        FilePath = FilePath,
        DarkFieldImageList = DarkFieldImageList
    };
}
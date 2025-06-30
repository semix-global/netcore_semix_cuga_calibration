using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Extensions;
using Core.Wcf.Models.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Microscope.PixelSize;

public sealed partial class MicroscopePixelSizeItemDto : CalibrationDtoBase, ICloneable<MicroscopePixelSizeItemDto>, IAdaptTo<CalibrationMicroscopePixelSizeItem>
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private Size _pixelSize;

    [ObservableProperty]
    private string _originFilePath = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    #region Mapper

    public MicroscopePixelSizeItemDto Clone() => new()
    {
        MicroscopeMagnificationEnum = MicroscopeMagnificationEnum,
        FindPosition = FindPosition,
        PixelSize = PixelSize,
        OriginFilePath = OriginFilePath,
        FilePath = FilePath,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        Id = Id,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Expiration = Expiration
    };

    public CalibrationMicroscopePixelSizeItem AdaptTo() => new()
    {
        CgMicroscopeLens = CustomerAdaptToMapper.Mapper<MicroscopeMagnificationEnum, CgMicroscopeLens>(MicroscopeMagnificationEnum),
        PixelSize = PixelSize.ToCgSize(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}
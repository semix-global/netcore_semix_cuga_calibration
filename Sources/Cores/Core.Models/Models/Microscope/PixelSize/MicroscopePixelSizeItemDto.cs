using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Microscope.PixelSize;

[CacheVersion("1.0.0")]
public sealed partial class MicroscopePixelSizeItemDto : CalibrationDTOBase<MicroscopePixelSizeItemDto>, IAdaptTo<CalibrationMicroscopePixelSizeItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation LensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial Point FindPosition { get; set; }

    [ObservableProperty]
    public partial Size PixelSize { get; set; }

    [ObservableProperty]
    public partial string OriginFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    #region Mapper

    public override MicroscopePixelSizeItemDto Clone() => new()
    {
        LensInformation = LensInformation,
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
        CgMicroscopeLens = LensInformation != MicroscopeLensInformation.Default ? LensInformation.AdaptTo().LensCode : CgMicroscopeLens.None,
        PixelSize = PixelSize.ToCgSize(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Microscope.Centricity;

[CacheVersion("1.0.0")]
public sealed partial class MicroscopeCentricityItemDto : CalibrationDTOBase<MicroscopeCentricityItemDto>, IAdaptTo<CalibrationMicroscopeCentricityItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation LensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial Point CentricityPosition { get; set; }

    [ObservableProperty]
    public partial Point Offset { get; set; }

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double TemplateScore { get; set; }

    [ObservableProperty]
    public partial double TemplateAngle { get; set; }

    #region Mapper

    public override MicroscopeCentricityItemDto Clone() => new()
    {
        LensInformation = LensInformation,
        CentricityPosition = CentricityPosition,
        Offset = Offset,
        FilePath = FilePath,
        TemplateFilePath = TemplateFilePath,
        TemplateImageFilePath = TemplateImageFilePath,
        TemplateScore = TemplateScore,
        TemplateAngle = TemplateAngle,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationMicroscopeCentricityItem AdaptTo() => new()
    {
        CgMicroscopeLens = LensInformation != MicroscopeLensInformation.Default ? LensInformation.AdaptTo().LensCode : CgMicroscopeLens.None,
        Offset = Offset.ToCgPoint(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}
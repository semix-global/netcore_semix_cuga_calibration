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
public sealed partial class MicroscopeCentricityItemDto : CalibrationDTOBase, ICloneable<MicroscopeCentricityItemDto>, IAdaptTo<CalibrationMicroscopeCentricityItem>
{
    [ObservableProperty]
    private MicroscopeLensInformation _lensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private Point _centricityPosition;

    [ObservableProperty]
    private Point _offset;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _templateFilePath = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath = string.Empty;

    [ObservableProperty]
    private double _templateScore;

    [ObservableProperty]
    private double _templateAngle;

    #region Mapper

    public MicroscopeCentricityItemDto Clone() => new()
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
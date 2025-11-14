using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Microscope.Centricity;

public sealed partial class MicroscopeCentricityItemDto : CalibrationDtoBase, ICloneable<MicroscopeCentricityItemDto>, IAdaptTo<CalibrationMicroscopeCentricityItem>, IAdaptIn<CalibrationMicroscopeCentricityItem, MicroscopeCentricityItemDto>
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
        CgMicroscopeLens = LensInformation.LensCode == -1 ? 0 : CustomerAdaptToMapper.Mapper<MicroscopeLensInformation, CgMicroscopeLens>(LensInformation),
        Offset = Offset.ToCgPoint(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    public MicroscopeCentricityItemDto AdaptIn(CalibrationMicroscopeCentricityItem obj) => new()
    {
        LensInformation = CustomerAdaptToMapper.Mapper<CgMicroscopeLens, MicroscopeLensInformation>(obj.CgMicroscopeLens),
        Offset = obj.Offset.ToPoint(),
        IsCalibrated = obj.IsCalibrated,
        IsVerified = obj.IsVerified,
        IsRequiredSelfCheck = obj.IsRequiredCalibrate
    };

    #endregion Mapper
}
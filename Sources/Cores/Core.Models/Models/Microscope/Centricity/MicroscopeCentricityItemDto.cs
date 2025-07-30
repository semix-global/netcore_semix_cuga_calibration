using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Pattern;
using Core.Wcf.Models.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Microscope.Centricity;

public sealed partial class MicroscopeCentricityItemDto : CalibrationDtoBase, ICloneable<MicroscopeCentricityItemDto>, IAdaptTo<CalibrationMicroscopeCentricityItem>, IAdaptIn<CalibrationMicroscopeCentricityItem, MicroscopeCentricityItemDto>
{
    [ObservableProperty]
    private MicroscopeMagnificationInfo _magnificationInfo = new();

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
        MagnificationInfo = MagnificationInfo,
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
        CgMicroscopeLens = CustomerAdaptToMapper.Mapper<MicroscopeMagnificationInfo, CgMicroscopeLens>(MagnificationInfo),
        Offset = Offset.ToCgPoint(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    public MicroscopeCentricityItemDto AdaptIn(CalibrationMicroscopeCentricityItem obj) => new()
    {
        MagnificationInfo = CustomerAdaptToMapper.Mapper<CgMicroscopeLens, MicroscopeMagnificationInfo>(obj.CgMicroscopeLens),
        Offset = obj.Offset.ToPoint(),
        IsCalibrated = obj.IsCalibrated,
        IsVerified = obj.IsVerified,
        IsRequiredSelfCheck = obj.IsRequiredSelfCheck
    };

    #endregion Mapper
}
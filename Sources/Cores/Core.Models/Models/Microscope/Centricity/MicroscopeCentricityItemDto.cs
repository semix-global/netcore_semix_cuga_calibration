using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Extensions;
using Core.Wcf.Models.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

namespace Core.Models.Models.Microscope.Centricity;

public sealed partial class MicroscopeCentricityItemDto : CalibrationDtoBase, ICloneable<MicroscopeCentricityItemDto>, IAdaptTo<CalibrationMicroscopeCentricityItem>
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum;

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
        MicroscopeMagnificationEnum = MicroscopeMagnificationEnum,
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
        CgMicroscopeLens = CustomerAdaptToMapper.Mapper<MicroscopeMagnificationEnum, CgMicroscopeLens>(MicroscopeMagnificationEnum),
        Offset = Offset.ToCgPoint(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}
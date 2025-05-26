using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Wcf.Models.Chuck;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

namespace Core.Models.Models.Chuck.Gantry;

public sealed partial class ChuckGantryDto : CalibrationDtoBase, ICloneable<ChuckGantryDto>, IAdaptTo<CalibrationChuckGantry>
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _lowMicroscopeMagnificationEnum;

    [ObservableProperty]
    private MicroscopeMagnificationEnum _highMicroscopeMagnificationEnum;

    [ObservableProperty]
    private Point _position1;

    [ObservableProperty]
    private string _filePath1 = string.Empty;

    [ObservableProperty]
    private string _lowTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _lowTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private double _templateScore1;

    [ObservableProperty]
    private double _templateAngle1;

    [ObservableProperty]
    private Point _position2;

    [ObservableProperty]
    private string _filePath2 = string.Empty;

    [ObservableProperty]
    private string _highTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _highTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private double _templateScore2;

    [ObservableProperty]
    private double _templateAngle2;

    [ObservableProperty]
    private double _offset;

    [ObservableProperty]
    private double _h;

    public double Slope => Math.Atan((Position2.X - Position1.X) / (Position2.Y - Position1.Y));

    #region Mapper

    public ChuckGantryDto Clone() => new()
    {
        LowMicroscopeMagnificationEnum = LowMicroscopeMagnificationEnum,
        HighMicroscopeMagnificationEnum = HighMicroscopeMagnificationEnum,
        Position1 = Position1,
        FilePath1 = FilePath1,
        LowTemplateFilePath = LowTemplateFilePath,
        LowTemplateImageFilePath = LowTemplateImageFilePath,
        TemplateScore1 = TemplateScore1,
        TemplateAngle1 = TemplateAngle1,
        Position2 = Position2,
        FilePath2 = FilePath2,
        HighTemplateFilePath = HighTemplateFilePath,
        HighTemplateImageFilePath = HighTemplateImageFilePath,
        TemplateScore2 = TemplateScore2,
        TemplateAngle2 = TemplateAngle2,
        Offset = Offset,
        H = H,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationChuckGantry AdaptTo() => new()
    {
        CgMicroscopeLens = CustomerAdaptToMapper.Mapper<MicroscopeMagnificationEnum, CgMicroscopeLens>(HighMicroscopeMagnificationEnum),
        Offset = Offset,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}
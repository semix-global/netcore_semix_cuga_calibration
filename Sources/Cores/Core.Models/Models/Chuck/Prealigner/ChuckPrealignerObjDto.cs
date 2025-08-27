using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Chuck;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.Prealigner;

public sealed partial class ChuckPrealignerObjDto : CalibrationDtoBase, ICloneable<ChuckPrealignerObjDto>, IAdaptTo<CalibrationPrealignerObj>
{
    [ObservableProperty]
    private MicroscopeLensInformation _lowMicroscopeLensInformation = new();

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = new();

    [ObservableProperty]
    private Point _offsetPosition;

    [ObservableProperty]
    private double _offsetAngle;

    [ObservableProperty]
    private Point _efemLoadWaferStagePosition;

    [ObservableProperty]
    private Point _newEfemLoadWaferStagePosition;

    [ObservableProperty]
    private double _efemLoadWaferChuckAngle;

    [ObservableProperty]
    private double _newEfemLoadWaferChuckAngle;

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

    #region Mapper

    public ChuckPrealignerObjDto Clone() => new()
    {
        LowMicroscopeLensInformation = LowMicroscopeLensInformation,
        HighMicroscopeLensInformation = HighMicroscopeLensInformation,
        OffsetPosition = OffsetPosition,
        OffsetAngle = OffsetAngle,
        EfemLoadWaferStagePosition = EfemLoadWaferStagePosition,
        NewEfemLoadWaferStagePosition = NewEfemLoadWaferStagePosition,
        EfemLoadWaferChuckAngle = EfemLoadWaferChuckAngle,
        NewEfemLoadWaferChuckAngle = NewEfemLoadWaferChuckAngle,
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
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationPrealignerObj AdaptTo() => new()
    {
        CgMicroscopeLens = HighMicroscopeLensInformation.LensCode == -1 ? 0 : CustomerAdaptToMapper.Mapper<MicroscopeLensInformation, CgMicroscopeLens>(HighMicroscopeLensInformation),
        NewEfemLoadWaferStagePosition = NewEfemLoadWaferStagePosition.ToCgPoint(),
        NewEfemLoadWaferChuckAngle = NewEfemLoadWaferChuckAngle,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}
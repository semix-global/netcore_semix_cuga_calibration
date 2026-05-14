using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Chuck;
using Cuga.Data.DataStruct.Microscope.Enums;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.Gantry;

[CacheVersion("1.0.0")]
public sealed partial class ChuckGantryDto : CalibrationDTOBase<ChuckGantryDto>, IAdaptTo<CalibrationChuckGantry>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation LowMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial MicroscopeLensInformation HighMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial Point Position1 { get; set; }

    [ObservableProperty]
    public partial string FilePath1 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double TemplateScore1 { get; set; }

    [ObservableProperty]
    public partial double TemplateAngle1 { get; set; }

    [ObservableProperty]
    public partial Point Position2 { get; set; }

    [ObservableProperty]
    public partial string FilePath2 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double TemplateScore2 { get; set; }

    [ObservableProperty]
    public partial double TemplateAngle2 { get; set; }

    [ObservableProperty]
    public partial double Offset { get; set; }

    [ObservableProperty]
    public partial double H { get; set; }

    public double Slope => Math.Atan((Position2.X - Position1.X) / (Position2.Y - Position1.Y));

    #region Mapper

    public override ChuckGantryDto Clone() => new()
    {
        LowMicroscopeLensInformation = LowMicroscopeLensInformation.Clone(),
        HighMicroscopeLensInformation = HighMicroscopeLensInformation.Clone(),
        Position1 = Position1,
        FilePath1 = FilePath1,
        TemplateScore1 = TemplateScore1,
        TemplateAngle1 = TemplateAngle1,
        Position2 = Position2,
        FilePath2 = FilePath2,
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
        CgMicroscopeLens = HighMicroscopeLensInformation != MicroscopeLensInformation.Default ? HighMicroscopeLensInformation.AdaptTo().LensCode : CgMicroscopeLens.None,
        Offset = Offset,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}
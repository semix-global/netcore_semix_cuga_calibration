using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Microscope.Focus;

[CacheVersion("1.0.0")]
public sealed partial class MicroscopeFocusItemDto : CalibrationDTOBase<MicroscopeFocusItemDto>, IAdaptTo<CalibrationMicroscopeFocusItem>
{
    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    public partial MicroscopeLensInformation LensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial Point FindPosition { get; set; }

    [ObservableProperty]
    public partial double EcsValue { get; set; }

    [ObservableProperty]
    public partial double TransBufferAfErrorValue { get; set; }

    [ObservableProperty]
    public partial double MicroscopeVoltage { get; set; }

    [ObservableProperty]
    public partial double Quality { get; set; }

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    #region Mapper

    public override MicroscopeFocusItemDto Clone() => new()
    {
        Index = Index,
        LensInformation = LensInformation.Clone(),
        FindPosition = FindPosition,
        EcsValue = EcsValue,
        TransBufferAfErrorValue = TransBufferAfErrorValue,
        MicroscopeVoltage = MicroscopeVoltage,
        Quality = Quality,
        FilePath = FilePath,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationMicroscopeFocusItem AdaptTo() => new()
    {
        CgMicroscopeLens = LensInformation != MicroscopeLensInformation.Default ? LensInformation.AdaptTo().LensCode : CgMicroscopeLens.None,
        EcsValue = EcsValue,
        MicroscopeVoltage = MicroscopeVoltage,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}
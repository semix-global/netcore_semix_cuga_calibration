using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Microscope.Focus;

public sealed partial class MicroscopeFocusItemDto : CalibrationDtoBase, ICloneable<MicroscopeFocusItemDto>, IAdaptTo<CalibrationMicroscopeFocusItem>, IAdaptIn<CalibrationMicroscopeFocusItem, MicroscopeFocusItemDto>
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private MicroscopeLensInformation _lensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private double _ecsValue;

    [ObservableProperty]
    private double _transBufferAfErrorValue;

    [ObservableProperty]
    private double _microscopeVoltage;

    [ObservableProperty]
    private double _quality;

    [ObservableProperty]
    private string _filePath = string.Empty;

    #region Mapper

    public MicroscopeFocusItemDto Clone() => new()
    {
        Index = Index,
        LensInformation = LensInformation,
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
        CgMicroscopeLens = LensInformation.LensCode == -1 ? 0 : CustomerAdaptToMapper.Mapper<MicroscopeLensInformation, CgMicroscopeLens>(LensInformation),
        EcsValue = EcsValue,
        MicroscopeVoltage = MicroscopeVoltage,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    public MicroscopeFocusItemDto AdaptIn(CalibrationMicroscopeFocusItem obj) => new()
    {
        LensInformation = CustomerAdaptToMapper.Mapper<CgMicroscopeLens, MicroscopeLensInformation>(obj.CgMicroscopeLens),
        EcsValue = obj.EcsValue,
        MicroscopeVoltage = obj.MicroscopeVoltage,
        IsCalibrated = obj.IsCalibrated,
        IsVerified = obj.IsVerified,
        IsRequiredSelfCheck = obj.IsRequiredCalibrate
    };

    #endregion Mapper
}
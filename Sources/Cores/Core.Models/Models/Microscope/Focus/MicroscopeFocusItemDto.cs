using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Pattern;
using Core.Wcf.Models.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Microscope.Focus;

public sealed partial class MicroscopeFocusItemDto : CalibrationDtoBase, ICloneable<MicroscopeFocusItemDto>, IAdaptTo<CalibrationMicroscopeFocusItem>
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private MicroscopeMagnificationInfo _magnificationInfo = new();

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
        MagnificationInfo = MagnificationInfo,
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
        CgMicroscopeLens = CustomerAdaptToMapper.Mapper<MicroscopeMagnificationInfo, CgMicroscopeLens>(MagnificationInfo),
        EcsValue = EcsValue,
        MicroscopeVoltage = MicroscopeVoltage,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}
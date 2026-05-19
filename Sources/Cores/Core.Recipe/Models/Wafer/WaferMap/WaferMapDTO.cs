using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Recipe.Models.Wafer.WaferMap;

public sealed partial class WaferMapDTO : ObservableObject, ICloneable<WaferMapDTO>
{
    [ObservableProperty]
    private WaferMapDataDTO _waferMapDataDTO = new();

    [ObservableProperty]
    private WaferMapDieDTO _originDieDTO = new();

    [ObservableProperty]
    private WaferMapDieDTO _originReticleDTO = new();

    public WaferMapDTO Clone() => new()
    {
        WaferMapDataDTO = WaferMapDataDTO.Clone(),
        OriginDieDTO = OriginDieDTO.Clone(),
        OriginReticleDTO = OriginReticleDTO.Clone()
    };
}
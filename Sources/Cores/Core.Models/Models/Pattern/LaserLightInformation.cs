using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Cuga.Data.DataStruct.PMT;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Pattern;

public partial class LaserLightInformation : ObservableObject, IAdaptIn<CgLightConfig, LaserLightInformation>, ICloneable<LaserLightInformation>
{
    [ObservableProperty]
    private double _level;

    [ObservableProperty]
    private double _coefficient;

    public LaserLightInformation AdaptIn(CgLightConfig obj)
    {
        Guard.IsNotNull(obj);

        Level = obj.LightProp;
        Coefficient = obj.LightCoeff;

        return this;
    }

    public LaserLightInformation Clone() => new()
    {
        Level = Level,
        Coefficient = Coefficient
    };
}
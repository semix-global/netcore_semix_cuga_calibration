using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.Pattern;

public sealed partial class OpticsConfiguration : ObservableObject, ICloneable<OpticsConfiguration>, IAdaptIn<OpticsConfiguration, OpticsConfiguration>
{
    [ObservableProperty]
    private OpticsApodizationModeEnum _opticsApodizationModeEnum = OpticsApodizationModeEnum.None;

    [ObservableProperty]
    private OpticsPolarizationModeEnum _opticsPolarizationModeEnum = OpticsPolarizationModeEnum.P;

    [ObservableProperty]
    private OpticsCollectorPolarizationModeEnum _opticsCollectorPolarizationModeEnum = OpticsCollectorPolarizationModeEnum.N;

    public OpticsConfiguration AdaptIn(OpticsConfiguration obj)
    {
        OpticsApodizationModeEnum = obj.OpticsApodizationModeEnum;
        OpticsPolarizationModeEnum = obj.OpticsPolarizationModeEnum;
        OpticsCollectorPolarizationModeEnum = obj.OpticsCollectorPolarizationModeEnum;

        return this;
    }

    public OpticsConfiguration Clone() => new()
    {
        OpticsApodizationModeEnum = OpticsApodizationModeEnum,
        OpticsPolarizationModeEnum = OpticsPolarizationModeEnum,
        OpticsCollectorPolarizationModeEnum = OpticsCollectorPolarizationModeEnum
    };

    public object ToHtmlAnonymous() => new
    {
        OpticsApodizationModeEnum,
        OpticsPolarizationModeEnum,
        OpticsCollectorPolarizationModeEnum
    };
}
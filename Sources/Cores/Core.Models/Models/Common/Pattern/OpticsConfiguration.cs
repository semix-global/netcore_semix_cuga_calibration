using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.Pattern;

public sealed partial class OpticsConfiguration : ObservableObject, ICloneable<OpticsConfiguration>
{
    [ObservableProperty]
    public partial OpticsApodizationModeEnum OpticsApodizationModeEnum { get; set; } = OpticsApodizationModeEnum.None;

    [ObservableProperty]
    public partial OpticsPolarizationModeEnum OpticsPolarizationModeEnum { get; set; } = OpticsPolarizationModeEnum.P;

    [ObservableProperty]
    public partial OpticsCollectorPolarizationModeEnum OpticsCollectorPolarizationModeEnum { get; set; } = OpticsCollectorPolarizationModeEnum.N;

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
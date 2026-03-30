using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.Pattern;

public sealed partial class OpticsConfiguration : ObservableObject, ICloneable<OpticsConfiguration>
{
    public OpticsConfiguration Clone() => new()
    {
    };

    public object ToHtmlAnonymous() => new
    {
    };
}
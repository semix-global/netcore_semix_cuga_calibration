using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Utilities.WPF.Assembly.Model;

public partial class TypeInfo : ObservableObject
{
    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    public string AssemblyQualifiedName { get; set; } = string.Empty;

    [Newtonsoft.Json.JsonIgnore]
    public Type? TypeInstance => Type.GetType(AssemblyQualifiedName);
}
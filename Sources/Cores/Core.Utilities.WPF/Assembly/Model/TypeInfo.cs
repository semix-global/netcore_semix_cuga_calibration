using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Utilities.WPF.Assembly.Model;

public partial class TypeInfo : ObservableObject
{
    [ObservableProperty]
    private string _description = string.Empty;

    public string AssemblyQualifiedName { get; set; } = string.Empty;

    [LiteDB.BsonIgnore]
    public Type? TypeInstance => Type.GetType(AssemblyQualifiedName);
}
using CommunityToolkit.Mvvm.ComponentModel;
using Mapster;

namespace CugaScript.Core.Models;

public sealed partial class ScriptFileMenu : ObservableObject, IEquatable<ScriptFileMenu>
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _directoryPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirectory), nameof(IsFile))]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private List<ScriptFileMenu> _childList = [];

    public bool IsDirectory => string.IsNullOrWhiteSpace(FilePath);

    public bool IsFile => IsDirectory == false;

    public ScriptFileMenu()
    {
    }

    public ScriptFileMenu(ScriptFileMenu item)
    {
        item.Adapt(this);
    }

    public bool Equals(ScriptFileMenu? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name && DirectoryPath == other.DirectoryPath && FilePath == other.FilePath;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ScriptFileMenu other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, DirectoryPath, FilePath);
    }
}
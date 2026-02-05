using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Common.DarkField;

public partial class DarkFieldRTFCDto : ObservableObject
{
    [ObservableProperty]
    private long _id;
    
    [ObservableProperty]
    private int _pmtId;

    [ObservableProperty]
    private Point _position;

    [ObservableProperty]
    private double _afEcs;

    [ObservableProperty]
    private double _afOffset;

    [ObservableProperty]
    private string _afImageFilePath = string.Empty;
}
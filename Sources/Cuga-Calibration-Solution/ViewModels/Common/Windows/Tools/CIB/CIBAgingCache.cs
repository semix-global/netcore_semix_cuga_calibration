using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.CIB.MMD;
using Local.SQL.Cache.Providers.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.CIB;

public sealed partial class CIBAgingCache : ObservableCacheBase
{
    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial CIBMMDCache CIBMMDCache { get; set; } = new();

    [ObservableProperty]
    public partial double CoefficientStep { get; set; } = 0.1;

    [ObservableProperty]
    public partial int FindCoefficientRetryTimes { get; set; } = 7;

    [ObservableProperty]
    public partial double MeasurePowerRatioThreshold { get; set; } = 0.1;

    [ObservableProperty]
    public partial int SampleCount { get; set; } = 10;

    [ObservableProperty]
    public partial double AgingThreshold { get; set; } = 0.1;

    [ObservableProperty]
    public partial IReadOnlyList<CIBAgingSelectItem> Agings { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CIBAgingSelectItem> SelectedAgings { get; set; } = [];
}

public sealed record CIBAgingSelectItem(double Coefficient, double MeasurePower)
{
    public override string ToString() => $"{Coefficient:0.###}: {MeasurePower:0.###}mW";
}
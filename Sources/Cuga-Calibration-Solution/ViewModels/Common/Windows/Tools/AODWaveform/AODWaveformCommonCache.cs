using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.Collections;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformCommonCache<TResult> : ObservableCacheBase
    where TResult : AODWaveformCommonResult, new()
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial double DefaultAmplitude { get; set; } = 1;

    [ObservableProperty]
    public partial double TotalMeasurePower { get; set; } = 20d;

    [ObservableProperty]
    public partial int TotalMeasurePowerCount { get; set; } = 20;

    [ObservableProperty]
    public partial GeneratePrescanAODWaveformParam FlatnessGeneratePrescanAODWaveformParam { get; set; } = new() { FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Flatness };

    [ObservableProperty]
    public partial GenerateChirpAODWaveformParam FlatnessGenerateChirpAODWaveformParam { get; set; } = new() { FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Flatness };

    [ObservableProperty]
    public partial Point MeasureMaxPowerMachinePosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial double WaitTime { get; set; } = 5;

    #region Result

    [ObservableProperty]
    public partial TResult[] Results { get; set; } = [];

    #endregion Result

    [RelayCommand]
    private void AddResult() => Results = [.. Results, new TResult()];

    [RelayCommand]
    private void RemoveResults(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var resultList = Results.ToList();

        foreach (TResult selectItem in selectItems) resultList.Remove(selectItem);

        Results = [.. resultList];
    }

    public virtual object ToHtmlAnonymous() => new
    {
        ProductivityInformation,
        DefaultAmplitude,
        TotalMeasurePower,
        TotalMeasurePowerCount,
        MeasureMaxPowerMachinePosition,
        WaitTime
    };
}

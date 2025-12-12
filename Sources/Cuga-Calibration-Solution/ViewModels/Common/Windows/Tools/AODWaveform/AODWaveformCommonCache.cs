using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.Collections;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformCommonCache<TResult> : ObservableCacheBase
    where TResult : AODWaveformCommonResult, new()
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _defaultAmplitude = 1;

    [ObservableProperty]
    private GeneratePrescanAODWaveformParam _flatnessGeneratePrescanAODWaveformParam = new() { FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Flatness };

    [ObservableProperty]
    private GenerateChirpAODWaveformParam _flatnessGenerateChirpAODWaveformParam = new() { FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Flatness };

    [ObservableProperty]
    private GeneratePrescanAODWaveformParam _scanGeneratePrescanAODWaveformParam = new();

    [ObservableProperty]
    private GenerateChirpAODWaveformParam _scanGenerateChirpAODWaveformParam = new();

    [ObservableProperty]
    private Point _measureMaxPowerMachinePosition = Point.Origin;

    [ObservableProperty]
    private double _waitTime = 5;

    #region Result

    [ObservableProperty]
    private IReadOnlyList<TResult> _results = [];

    #endregion Result

    [RelayCommand]
    private void AddResult() => Results = [.. Results, new TResult()];

    [RelayCommand]
    private void RemoveResults(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var resultList = Results.ToList();
        foreach (TResult selectItem in selectItems) resultList.Remove(selectItem);

        Results = resultList;
    }

    public virtual object ToHtmlAnonymous() => new
    {
        OpticsIlluminationModeEnum,
        ProductivityInformation,
        DefaultAmplitude,
        MeasureMaxPowerMachinePosition,
        WaitTime
    };
}
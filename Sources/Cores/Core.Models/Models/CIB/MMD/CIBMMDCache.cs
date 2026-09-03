using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using Net.Utilities.ScottPlot.Interfaces;
using ScottPlot;

namespace Core.Models.Models.CIB.MMD;

public sealed partial class CIBMMDCache : CalibrationCacheBase<CIBMMDCache>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<CIBInformation> CIBInformations { get; set; } = [];

    [ObservableProperty]
    public partial Point HazeFindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial double PrescanFrequency { get; set; }

    [ObservableProperty]
    public partial GeneratePrescanAODWaveformParam GeneratePrescanAODWaveformParam { get; set; } = new() { FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Flatness };

    [ObservableProperty]
    public partial double ChirpFrequency { get; set; }

    [ObservableProperty]
    public partial GenerateChirpAODWaveformParam GenerateChirpAODWaveformParam { get; set; } = new() { FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Flatness };

    [ObservableProperty]
    public partial int MeasurePowerNoisesCount { get; set; } = 10;

    [ObservableProperty]
    public partial double MeasurePowerWaitTime { get; set; } = 5d;

    [ObservableProperty]
    public partial double PMTValueWaitTime { get; set; } = 0.5d;

    [ObservableProperty]
    public partial double StartCoefficient { get; set; } = 0.01;

    [ObservableProperty]
    public partial double StepCoefficient { get; set; } = 0.01;

    [ObservableProperty]
    public partial double StopCoefficient { get; set; } = 1d;

    [ObservableProperty]
    public partial double MeasurePowerSequenceCommonRatio { get; set; } = 0.5;

    [ObservableProperty]
    public partial double MeasurePowerNotUseODFilterMinValue { get; set; } = 1d;

    [ObservableProperty]
    public partial double MMDMeasurePowerRangeRatio { get; set; } = 50000d;

    [ObservableProperty]
    public partial double StartGain { get; set; } = -10;

    [ObservableProperty]
    public partial double StepGain { get; set; } = 0.2;

    [ObservableProperty]
    public partial double StopGain { get; set; } = 10;

    [ObservableProperty]
    public partial double ProtectedPMTValue { get; set; } = 409.6;

    [ObservableProperty]
    public partial int ProtectedOverflowProtectedPMTValueCount { get; set; } = 3;

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 2048;

    [ObservableProperty]
    public partial double DarkCurrent { get; set; }

    [ObservableProperty]
    public partial double Denominator { get; set; } = 4096d;

    [ObservableProperty]
    public partial double ScaleFactor { get; set; } = 2000000d;

    [ObservableProperty]
    public partial double MinValidFraction { get; set; }

    [ObservableProperty]
    public partial double MaxValidFraction { get; set; } = 250000d;

    [ObservableProperty]
    public partial double VerifyMinLogGain { get; set; } = 0d;

    [ObservableProperty]
    public partial double VerifyMaxLogGain { get; set; } = 14d;

    [ObservableProperty]
    public partial int SmoothLogGainMul128U12BitWindowSize { get; set; } = 201;

    [ObservableProperty]
    public partial int SmoothGainS16BitWindowSize { get; set; } = 51;

    [ObservableProperty]
    public partial IReadOnlyList<MMDConfiguration> MMDConfigurations { get; set; } = [];

    /********** 缓存的结果 **********/

    [ObservableProperty]
    public partial string PrescanAODWaveformResultFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<PrescanAODWaveformProfile> PrescanAODWaveformProfiles { get; set; } = [];

    [ObservableProperty]
    public partial string ChirpAODWaveformResultFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<ChirpAODWaveformProfile> ChirpAODWaveformProfiles { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Point> OriginMeasurePowerPoints { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Point> MeasurePowerPoints { get; set; } = [];

    [ObservableProperty]
    public partial double P0 { get; set; }

    [ObservableProperty]
    public partial double P1 { get; set; }

    [ObservableProperty]
    public partial double P2 { get; set; }

    [ObservableProperty]
    public partial double P3 { get; set; }

    [ObservableProperty]
    public partial double RSquared { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Point> FitMeasurePowerPoints { get; set; } = [];

    [ObservableProperty]
    public partial double ODFilterRatio { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Point> NotUseODFilterMeasurePowerPoints { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Point> UseODFilterMeasurePowerPoints { get; set; } = [];

    /********** 缓存的结果 **********/

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnOriginMeasurePowerPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnMeasurePowerPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnODFilterRatioChanged(double value) => RefreshPlot();

    partial void OnP0Changed(double value) => RefreshPlot();

    partial void OnP1Changed(double value) => RefreshPlot();

    partial void OnP2Changed(double value) => RefreshPlot();

    partial void OnP3Changed(double value) => RefreshPlot();

    partial void OnRSquaredChanged(double value) => RefreshPlot();

    partial void OnFitMeasurePowerPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnNotUseODFilterMeasurePowerPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnUseODFilterMeasurePowerPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public CIBMMDCache()
    {
        PlotDataSource.SetTitle("Measure Power(Y: mW X: Coefficient)");
    }

    private void RefreshPlot()
    {
        try
        {
            PlotDataSource.Clear();

            if (OriginMeasurePowerPoints.Count > 0) PlotDataSource.GetOrAddScatterLine("Origin", OriginMeasurePowerPoints, Constants.Category10.GetColor(0));
            if (MeasurePowerPoints.Count > 0) PlotDataSource.GetOrAddScatterLine($"Filter OD = {ODFilterRatio:0.######}", MeasurePowerPoints, Constants.Category10.GetColor(1));
            if (FitMeasurePowerPoints.Count > 0) PlotDataSource.GetOrAddScatterLine($"Fit Curve: y = {P0:0.######} + {P1:0.######}x + {P2:0.######}x^2 + {P3:0.######}x^3 r^2 = {RSquared:0.######}", FitMeasurePowerPoints, Constants.Category10.GetColor(2));
            if (NotUseODFilterMeasurePowerPoints.Count > 0) PlotDataSource.GetOrAddScatterMarkers("Not Use OD", NotUseODFilterMeasurePowerPoints, Constants.Category10.GetColor(3), MarkerShape.FilledDiamond);
            if (UseODFilterMeasurePowerPoints.Count > 0) PlotDataSource.GetOrAddScatterMarkers("Use OD", UseODFilterMeasurePowerPoints, Constants.Category10.GetColor(4), MarkerShape.OpenDiamond);
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    public override CIBMMDCache Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBInformations = [.. CIBInformations.Select(t => t.Clone())],
        HazeFindBFMachinePosition = HazeFindBFMachinePosition,
        ProductivityInformation = ProductivityInformation.Clone(),
        PrescanFrequency = PrescanFrequency,
        GeneratePrescanAODWaveformParam = GeneratePrescanAODWaveformParam.Clone(),
        ChirpFrequency = ChirpFrequency,
        GenerateChirpAODWaveformParam = GenerateChirpAODWaveformParam.Clone(),
        MeasurePowerNoisesCount = MeasurePowerNoisesCount,
        MeasurePowerWaitTime = MeasurePowerWaitTime,
        PMTValueWaitTime = PMTValueWaitTime,
        StartCoefficient = StartCoefficient,
        StepCoefficient = StepCoefficient,
        StopCoefficient = StopCoefficient,
        MeasurePowerSequenceCommonRatio = MeasurePowerSequenceCommonRatio,
        MeasurePowerNotUseODFilterMinValue = MeasurePowerNotUseODFilterMinValue,
        MMDMeasurePowerRangeRatio = MMDMeasurePowerRangeRatio,
        StartGain = StartGain,
        StepGain = StepGain,
        StopGain = StopGain,
        ProtectedPMTValue = ProtectedPMTValue,
        ProtectedOverflowProtectedPMTValueCount = ProtectedOverflowProtectedPMTValueCount,
        ImageWidth = ImageWidth,
        DarkCurrent = DarkCurrent,
        Denominator = Denominator,
        ScaleFactor = ScaleFactor,
        MinValidFraction = MinValidFraction,
        MaxValidFraction = MaxValidFraction,
        VerifyMinLogGain = VerifyMinLogGain,
        VerifyMaxLogGain = VerifyMaxLogGain,
        SmoothLogGainMul128U12BitWindowSize = SmoothLogGainMul128U12BitWindowSize,
        SmoothGainS16BitWindowSize = SmoothGainS16BitWindowSize,
        MMDConfigurations = [.. MMDConfigurations.Select(t => t.Clone())],
        PrescanAODWaveformResultFilePath = PrescanAODWaveformResultFilePath,
        PrescanAODWaveformProfiles = [.. PrescanAODWaveformProfiles.Select(t => t.Clone())],
        ChirpAODWaveformResultFilePath = ChirpAODWaveformResultFilePath,
        ChirpAODWaveformProfiles = [.. ChirpAODWaveformProfiles.Select(t => t.Clone())],
        OriginMeasurePowerPoints = [.. OriginMeasurePowerPoints],
        MeasurePowerPoints = [.. MeasurePowerPoints],
        P0 = P0,
        P1 = P1,
        P2 = P2,
        P3 = P3,
        RSquared = RSquared,
        FitMeasurePowerPoints = [.. FitMeasurePowerPoints],
        ODFilterRatio = ODFilterRatio,
        NotUseODFilterMeasurePowerPoints = [.. NotUseODFilterMeasurePowerPoints],
        UseODFilterMeasurePowerPoints = [.. UseODFilterMeasurePowerPoints],
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };

    public sealed partial class MMDConfiguration : ObservableObject, ICloneable<MMDConfiguration>
    {
        [ObservableProperty]
        public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

        [ObservableProperty]
        public partial double FilterMinGain { get; set; } = -10d;

        [ObservableProperty]
        public partial double PowerRate { get; set; } = 1d;

        public MMDConfiguration Clone() => new()
        {
            CIBInformation = CIBInformation.Clone(),
            FilterMinGain = FilterMinGain,
            PowerRate = PowerRate
        };
    }
}
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Models.Enums.Maths;
using System.IO;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public abstract partial class AbstractGenerateAODWaveformParam : ObservableCacheBase
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.High;

    [ObservableProperty]
    private bool _isHeaderAndFooter;

    [ObservableProperty]
    private double _headerFrequency;

    [ObservableProperty]
    private double _footerFrequency;

    [ObservableProperty]
    private double _bandWidth = 100;

    [ObservableProperty]
    private double _centerFrequency = 150;

    [ObservableProperty]
    private FunctionMonotonicTypeEnum _functionMonotonicTypeEnum;

    [ObservableProperty]
    private double _sampleRate = 1064d;

    [ObservableProperty]
    private double _amplitude = 1d;

    [ObservableProperty]
    private string _directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), nameof(AODWaveform));

    [ObservableProperty]
    private int _zeroSampleCount;

    [ObservableProperty]
    private int _endpointSampleCount;

    [ObservableProperty]
    private int _generateRetryTimes = 1000;

    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> _electrodeConfigurations = [];

    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformUniformityConfiguration> _uniformityConfigurations = [];

    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformSlopeDeltaKConfiguration> _slopeDeltaKConfigurations = [];

    [ObservableProperty]
    private double _sincCoefficient;

    [ObservableProperty]
    private double _astigmatismCompensationCoefficient;

    [ObservableProperty]
    private double _sphericalAberrationCompensationCoefficient;

    [ObservableProperty]
    private double _secondaryAstigmatismCompensationCoefficient;

    [ObservableProperty]
    private double _comaCompensationCoefficient;

    [ObservableProperty]
    private double _trefoilCompensationCoefficient;

    [ObservableProperty]
    private double _quadrafoilCompensationCoefficient;

    [ObservableProperty]
    private double _alphaOrder;

    [ObservableProperty]
    private double _alphaOrderCoefficient;

    partial void OnHeaderFrequencyChanged(double value)
    {
        if (IsHeaderAndFooter == false) return;

        BandWidth = Math.Abs(FooterFrequency - value);
        CenterFrequency = (FooterFrequency + value) / 2d;
        FunctionMonotonicTypeEnum = FooterFrequency - value == 0
            ? FunctionMonotonicTypeEnum.Flatness
            : FooterFrequency > value
                ? FunctionMonotonicTypeEnum.Increasing
                : FunctionMonotonicTypeEnum.Deceasing;
    }

    partial void OnFooterFrequencyChanged(double value)
    {
        if (IsHeaderAndFooter == false) return;

        BandWidth = Math.Abs(value - HeaderFrequency);
        CenterFrequency = (value + HeaderFrequency) / 2d;
        FunctionMonotonicTypeEnum = value - HeaderFrequency == 0
            ? FunctionMonotonicTypeEnum.Flatness
            : value > HeaderFrequency
                ? FunctionMonotonicTypeEnum.Increasing
                : FunctionMonotonicTypeEnum.Deceasing;
    }

    partial void OnBandWidthChanged(double value)
    {
        if (IsHeaderAndFooter) return;

        HeaderFrequency = FunctionMonotonicTypeEnum switch
        {
            FunctionMonotonicTypeEnum.Increasing => CenterFrequency - value / 2d,
            FunctionMonotonicTypeEnum.Deceasing => CenterFrequency + value / 2d,
            FunctionMonotonicTypeEnum.Flatness => CenterFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(FunctionMonotonicTypeEnum))
        };
        FooterFrequency = FunctionMonotonicTypeEnum switch
        {
            FunctionMonotonicTypeEnum.Increasing => CenterFrequency + value / 2d,
            FunctionMonotonicTypeEnum.Deceasing => CenterFrequency - value / 2d,
            FunctionMonotonicTypeEnum.Flatness => CenterFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(FunctionMonotonicTypeEnum))
        };
    }

    partial void OnCenterFrequencyChanged(double value)
    {
        if (IsHeaderAndFooter) return;

        HeaderFrequency = FunctionMonotonicTypeEnum switch
        {
            FunctionMonotonicTypeEnum.Increasing => value - BandWidth / 2d,
            FunctionMonotonicTypeEnum.Deceasing => value + BandWidth / 2d,
            FunctionMonotonicTypeEnum.Flatness => value,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(FunctionMonotonicTypeEnum))
        };
        FooterFrequency = FunctionMonotonicTypeEnum switch
        {
            FunctionMonotonicTypeEnum.Increasing => value + BandWidth / 2d,
            FunctionMonotonicTypeEnum.Deceasing => value - BandWidth / 2d,
            FunctionMonotonicTypeEnum.Flatness => value,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(FunctionMonotonicTypeEnum))
        };
    }

    partial void OnFunctionMonotonicTypeEnumChanged(FunctionMonotonicTypeEnum value)
    {
        if (IsHeaderAndFooter) return;

        HeaderFrequency = value switch
        {
            FunctionMonotonicTypeEnum.Increasing => CenterFrequency - BandWidth / 2d,
            FunctionMonotonicTypeEnum.Deceasing => CenterFrequency + BandWidth / 2d,
            FunctionMonotonicTypeEnum.Flatness => CenterFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(FunctionMonotonicTypeEnum))
        };
        FooterFrequency = FunctionMonotonicTypeEnum switch
        {
            FunctionMonotonicTypeEnum.Increasing => CenterFrequency + BandWidth / 2d,
            FunctionMonotonicTypeEnum.Deceasing => CenterFrequency - BandWidth / 2d,
            FunctionMonotonicTypeEnum.Flatness => CenterFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(FunctionMonotonicTypeEnum))
        };
        BandWidth = value == FunctionMonotonicTypeEnum.Flatness ? 0d : BandWidth;
    }

    protected T CopyPropertiesTo<T>(T obj) where T : AODWaveformGenerator.AbstractAODWaveformParam
    {
        obj.BandWidth = BandWidth;
        obj.CenterFrequency = CenterFrequency;
        obj.FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum;
        obj.SampleRate = SampleRate;
        obj.Amplitude = Amplitude;
        obj.DirectoryPath = DirectoryPath;
        obj.FileNameSuffix = OpticsMagTypeEnum.ToCgMagTypeEnum().ToString();
        obj.ZeroSampleCount = ZeroSampleCount;
        obj.EndpointSampleCount = EndpointSampleCount;
        obj.GenerateRetryTimes = GenerateRetryTimes;
        obj.OffsetConfigurations = [.. ElectrodeConfigurations.Select(t => t.AdaptTo())];
        obj.UniformityConfigurations = [.. UniformityConfigurations.Select(t => t.AdaptTo())];
        obj.SlopeDeltaKConfigurations = [.. SlopeDeltaKConfigurations.Select(t => t.AdaptTo())];
        obj.SincCoefficient = SincCoefficient;
        obj.AstigmatismCompensationCoefficient = AstigmatismCompensationCoefficient;
        obj.SphericalAberrationCompensationCoefficient = SphericalAberrationCompensationCoefficient;
        obj.SecondaryAstigmatismCompensationCoefficient = SecondaryAstigmatismCompensationCoefficient;
        obj.ComaCompensationCoefficient = ComaCompensationCoefficient;
        obj.TrefoilCompensationCoefficient = TrefoilCompensationCoefficient;
        obj.QuadrafoilCompensationCoefficient = QuadrafoilCompensationCoefficient;
        obj.AlphaOrder = AlphaOrder;
        obj.AlphaOrderCoefficient = AlphaOrderCoefficient;

        return obj;
    }
}
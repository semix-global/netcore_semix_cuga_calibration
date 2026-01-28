using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Nlog.Entities.HtmlElements;
using System.IO;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public abstract partial class AbstractGenerateAODWaveformParam :
    ObservableCacheBase,
    IAdaptIn<AbstractGenerateAODWaveformParam, AbstractGenerateAODWaveformParam>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private bool _isHeaderAndFooter;

    [ObservableProperty]
    private double _headerFrequency;

    [ObservableProperty]
    private double _footerFrequency;

    [ObservableProperty]
    private double _bandWidth;

    [ObservableProperty]
    private double _centerFrequency;

    [ObservableProperty]
    private FunctionMonotonicTypeEnum _functionMonotonicTypeEnum;

    [ObservableProperty]
    private double _sampleRate = 1064d;

    [ObservableProperty]
    private string _directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), nameof(AODWaveform));

    [ObservableProperty]
    private int _zeroSampleCount;

    [ObservableProperty]
    private int _endpointSampleCount;

    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> _electrodeConfigurations = [];

    [ObservableProperty]
    private double _p3CompensationCoefficient;

    [ObservableProperty]
    private double _p4CompensationCoefficient;

    [ObservableProperty]
    private double _p5CompensationCoefficient;

    [ObservableProperty]
    private double _p6CompensationCoefficient;

    [ObservableProperty]
    private double _p7CompensationCoefficient;

    [ObservableProperty]
    private double _p8CompensationCoefficient;

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
        OnBandWidthChanged();

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

    protected AbstractGenerateAODWaveformParam()
    {
        BandWidth = 100;
        CenterFrequency = 150;
        FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Deceasing;
    }

    protected virtual void OnBandWidthChanged()
    {
    }

    public void WithFrequencyFlatness(double frequency)
    {
        FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Flatness;
        IsHeaderAndFooter = false;
        BandWidth = 0d;
        CenterFrequency = frequency;
        foreach (var electrodeConfiguration in ElectrodeConfigurations) electrodeConfiguration.UniformityConfigurations = [];
        P3CompensationCoefficient = 0d;
        P4CompensationCoefficient = 0d;
        P5CompensationCoefficient = 0d;
        P6CompensationCoefficient = 0d;
        P7CompensationCoefficient = 0d;
        P8CompensationCoefficient = 0d;
    }

    public AbstractGenerateAODWaveformParam AdaptIn(AbstractGenerateAODWaveformParam obj)
    {
        ProductivityInformation = obj.ProductivityInformation.Clone();
        IsHeaderAndFooter = obj.IsHeaderAndFooter;
        HeaderFrequency = obj.HeaderFrequency;
        FooterFrequency = obj.FooterFrequency;
        BandWidth = obj.BandWidth;
        CenterFrequency = obj.CenterFrequency;
        FunctionMonotonicTypeEnum = obj.FunctionMonotonicTypeEnum;
        SampleRate = obj.SampleRate;
        DirectoryPath = obj.DirectoryPath;
        ZeroSampleCount = obj.ZeroSampleCount;
        EndpointSampleCount = obj.EndpointSampleCount;
        ElectrodeConfigurations = [.. obj.ElectrodeConfigurations.Select(t => t.Clone())];
        P3CompensationCoefficient = obj.P3CompensationCoefficient;
        P4CompensationCoefficient = obj.P4CompensationCoefficient;
        P5CompensationCoefficient = obj.P5CompensationCoefficient;
        P6CompensationCoefficient = obj.P6CompensationCoefficient;
        P7CompensationCoefficient = obj.P7CompensationCoefficient;
        P8CompensationCoefficient = obj.P8CompensationCoefficient;

        return this;
    }

    public virtual object ToFlatnessHtmlAnonymous() => new
    {
        ProductivityInformation,
        IsHeaderAndFooter,
        HeaderFrequency,
        FooterFrequency,
        BandWidth,
        CenterFrequency,
        FunctionMonotonicTypeEnum,
        SampleRate,
        DirectoryPath,
        ZeroSampleCount,
        EndpointSampleCount,
        ElectrodeConfigurations = new HtmlTable([.. ElectrodeConfigurations.Select(t => t.ToFlatnessHtmlAnonymous())])
    };

    public virtual object ToHtmlAnonymous() => new
    {
        ProductivityInformation,
        IsHeaderAndFooter,
        HeaderFrequency,
        FooterFrequency,
        BandWidth,
        CenterFrequency,
        FunctionMonotonicTypeEnum,
        SampleRate,
        DirectoryPath,
        ZeroSampleCount,
        EndpointSampleCount,
        ElectrodeConfigurations = new HtmlTable([.. ElectrodeConfigurations.Select(t => t.ToHtmlAnonymous())]),
        P3CompensationCoefficient,
        P4CompensationCoefficient,
        P5CompensationCoefficient,
        P6CompensationCoefficient,
        P7CompensationCoefficient,
        P8CompensationCoefficient
    };
}
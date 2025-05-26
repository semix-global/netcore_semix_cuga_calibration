using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Enums.Maths;

namespace Core.Models.Models.Common.DarkField;

public partial class GenerateAodWaveParamBase : ObservableCacheBase
{
    [ObservableProperty]
    private bool _isHeaderAndFooter = true;

    [ObservableProperty]
    private double _bandWidth;

    [ObservableProperty]
    private double _centerFrequency;

    [ObservableProperty]
    private double _headerFrequency;

    [ObservableProperty]
    private double _footerFrequency;

    [ObservableProperty]
    private MonotonicTypeEnum _monotonicTypeEnum;

    [ObservableProperty]
    private double _sampleRate = 1064d;

    [ObservableProperty]
    private double _amplitude = 1d;

    [ObservableProperty]
    private string _aodWaveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    [ObservableProperty]
    private int _zeroSampleCount;

    [ObservableProperty]
    private int _endpointSampleCount;

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

    [ObservableProperty]
    private int _generateRetryTimes = 1;

    partial void OnBandWidthChanged(double value)
    {
        if (IsHeaderAndFooter) return;

        HeaderFrequency = MonotonicTypeEnum switch
        {
            MonotonicTypeEnum.Increasing => CenterFrequency - value / 2d,
            MonotonicTypeEnum.Deceasing => CenterFrequency + value / 2d,
            MonotonicTypeEnum.Flatness => CenterFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(MonotonicTypeEnum))
        };
        FooterFrequency = MonotonicTypeEnum switch
        {
            MonotonicTypeEnum.Increasing => CenterFrequency + value / 2d,
            MonotonicTypeEnum.Deceasing => CenterFrequency - value / 2d,
            MonotonicTypeEnum.Flatness => CenterFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(MonotonicTypeEnum))
        };
    }

    partial void OnCenterFrequencyChanged(double value)
    {
        if (IsHeaderAndFooter) return;

        HeaderFrequency = MonotonicTypeEnum switch
        {
            MonotonicTypeEnum.Increasing => value - BandWidth / 2d,
            MonotonicTypeEnum.Deceasing => value + BandWidth / 2d,
            MonotonicTypeEnum.Flatness => value,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(MonotonicTypeEnum))
        };
        FooterFrequency = MonotonicTypeEnum switch
        {
            MonotonicTypeEnum.Increasing => value + BandWidth / 2d,
            MonotonicTypeEnum.Deceasing => value - BandWidth / 2d,
            MonotonicTypeEnum.Flatness => value,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(MonotonicTypeEnum))
        };
    }

    partial void OnMonotonicTypeEnumChanged(MonotonicTypeEnum value)
    {
        if (IsHeaderAndFooter) return;

        HeaderFrequency = value switch
        {
            MonotonicTypeEnum.Increasing => CenterFrequency - BandWidth / 2d,
            MonotonicTypeEnum.Deceasing => CenterFrequency + BandWidth / 2d,
            MonotonicTypeEnum.Flatness => CenterFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(MonotonicTypeEnum))
        };
        FooterFrequency = MonotonicTypeEnum switch
        {
            MonotonicTypeEnum.Increasing => CenterFrequency + BandWidth / 2d,
            MonotonicTypeEnum.Deceasing => CenterFrequency - BandWidth / 2d,
            MonotonicTypeEnum.Flatness => CenterFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(MonotonicTypeEnum))
        };
        BandWidth = value == MonotonicTypeEnum.Flatness ? 0d : BandWidth;
    }

    partial void OnHeaderFrequencyChanged(double value)
    {
        if (IsHeaderAndFooter == false) return;

        BandWidth = Math.Abs(FooterFrequency - value);
        CenterFrequency = (FooterFrequency + value) / 2d;
        MonotonicTypeEnum = FooterFrequency - value == 0
            ? MonotonicTypeEnum.Flatness
            : FooterFrequency > value
                ? MonotonicTypeEnum.Increasing
                : MonotonicTypeEnum.Deceasing;
    }

    partial void OnFooterFrequencyChanged(double value)
    {
        if (IsHeaderAndFooter == false) return;

        BandWidth = Math.Abs(value - HeaderFrequency);
        CenterFrequency = (value + HeaderFrequency) / 2d;
        MonotonicTypeEnum = value - HeaderFrequency == 0
            ? MonotonicTypeEnum.Flatness
            : value > HeaderFrequency
                ? MonotonicTypeEnum.Increasing
                : MonotonicTypeEnum.Deceasing;
    }
}
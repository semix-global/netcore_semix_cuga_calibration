using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using System.Collections;
using System.IO;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public abstract partial class AbstractGenerateAODWaveformParam :
    ObservableObject,
    IAdaptIn<AbstractGenerateAODWaveformParam, AbstractGenerateAODWaveformParam>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial bool IsHeaderAndFooter { get; set; }

    [ObservableProperty]
    public partial double HeaderFrequency { get; set; }

    [ObservableProperty]
    public partial double FooterFrequency { get; set; }

    [ObservableProperty]
    public partial double BandWidth { get; set; }

    [ObservableProperty]
    public partial double CenterFrequency { get; set; }

    [ObservableProperty]
    public partial FunctionMonotonicTypeEnum FunctionMonotonicTypeEnum { get; set; }

    [ObservableProperty]
    public partial double SampleRate { get; set; } = 1064d;

    [ObservableProperty]
    public partial string DirectoryPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), nameof(AODWaveform));

    [ObservableProperty]
    public partial int ZeroSampleCount { get; set; }

    [ObservableProperty]
    public partial int EndpointSampleCount { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> ElectrodeConfigurations { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<GenerateAODWaveformSlopeConfiguration> SlopeConfigurations { get; set; } = [];

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


    [RelayCommand]
    private void ChangeDirectoryPath()
    {
        var dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();

        var dialog = dialogWindowProvider.TryShowSelectDirectoryPathDialog(out var directoryPath);
        if (dialog == false) return;

        DirectoryPath = directoryPath;
    }

    [RelayCommand]
    private void AddElectrodeConfiguration()
    {
        var electrodeEnums = EnumHelper.Enums<OpticsAODElectrodeEnum>();

        if (ElectrodeConfigurations.Count > electrodeEnums.Length) return;

        GenerateAODWaveformElectrodeConfiguration[] electrodeConfigurations = [.. ElectrodeConfigurations, new()];

        foreach (var (index, item) in electrodeConfigurations.Index()) item.OpticsAODElectrodeEnum = electrodeEnums[index];

        ElectrodeConfigurations = electrodeConfigurations;
    }

    [RelayCommand]
    private void RemoveElectrodeConfiguration(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var electrodeEnums = EnumHelper.Enums<OpticsAODElectrodeEnum>();

        var configurationList = ElectrodeConfigurations.ToList();

        foreach (GenerateAODWaveformElectrodeConfiguration selectItem in selectItems)
        {
            if (selectItem.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode1) continue;

            configurationList.Remove(selectItem);
        }

        foreach (var (index, item) in configurationList.Index()) item.OpticsAODElectrodeEnum = electrodeEnums[index];

        ElectrodeConfigurations = [.. configurationList];
    }

    [RelayCommand]
    private void AddSlopeConfiguration() => SlopeConfigurations = [.. SlopeConfigurations, new GenerateAODWaveformSlopeConfiguration()];

    [RelayCommand]
    private void RemoveSlopeConfiguration(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var configurationList = SlopeConfigurations.ToList();

        foreach (GenerateAODWaveformSlopeConfiguration selectItem in selectItems) configurationList.Remove(selectItem);

        SlopeConfigurations = [.. configurationList];
    }

    public void WithFrequencyFlatness(double frequency)
    {
        FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Flatness;
        IsHeaderAndFooter = false;
        BandWidth = 0d;
        CenterFrequency = frequency;
        foreach (var electrodeConfiguration in ElectrodeConfigurations) electrodeConfiguration.UniformityConfigurations = [];
        SlopeConfigurations = [];
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
        SlopeConfigurations = [.. obj.SlopeConfigurations.Select(t => t.Clone())];

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
        SlopeConfigurations = new HtmlTable([.. SlopeConfigurations.Select(t => t.ToHtmlAnonymous())])
    };
}
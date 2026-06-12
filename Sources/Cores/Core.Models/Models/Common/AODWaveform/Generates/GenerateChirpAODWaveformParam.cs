using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GenerateChirpAODWaveformParam :
    AbstractGenerateAODWaveformParam,
    IAdaptTo<AODWaveformGenerator1.ChirpAODWaveformParam>,
    ICloneable<GenerateChirpAODWaveformParam>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpectralDensity))]
    public partial double SoundPacketLength { get; set; } = 3.2;

    [ObservableProperty]
    public partial double SoundSpeed { get; set; } = 5.742;

    public double SpectralDensity => SoundPacketLength != 0 ? BandWidth / SoundPacketLength : 0;

    protected override void OnBandWidthChanged()
    {
        base.OnBandWidthChanged();

        OnPropertyChanged(nameof(SpectralDensity));
    }

    public AODWaveformGenerator1.ChirpAODWaveformParam AdaptTo() => new(SoundPacketLength, SoundSpeed)
    {
        BandWidth = BandWidth,
        CenterFrequency = CenterFrequency,
        FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum,
        SampleRate = SampleRate,
        DirectoryPath = DirectoryPath,
        FileNameSuffix = $"{ProductivityInformation.OpticsIlluminationModeEnum}_{ProductivityInformation.AdaptTo().Mag}",
        ZeroSampleCount = ZeroSampleCount,
        EndpointSampleCount = EndpointSampleCount,
        OffsetConfigurations = [.. ElectrodeConfigurations.Select(t => t.AdaptTo())],
        SlopeDeltaKConfigurations = [.. SlopeDeltaKConfigurations.Select(t => t.AdaptTo())]
    };

    public GenerateChirpAODWaveformParam Clone()
    {
        var param = (GenerateChirpAODWaveformParam)new GenerateChirpAODWaveformParam().AdaptIn(this);

        param.SoundPacketLength = SoundPacketLength;
        param.SoundSpeed = SoundSpeed;

        return param;
    }

    public override object ToFlatnessHtmlAnonymous() => new
    {
        SoundPacketLength,
        SoundSpeed,
        SpectralDensity,
        Base = new HtmlQuote(base.ToFlatnessHtmlAnonymous())
    };

    public override object ToHtmlAnonymous() => new
    {
        SoundPacketLength,
        SoundSpeed,
        SpectralDensity,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}
using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GenerateChirpAODWaveformParam :
    AbstractGenerateAODWaveformParam,
    IAdaptTo<AODWaveformGenerator.ChirpAODWaveformParam>,
    ICloneable<GenerateChirpAODWaveformParam>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpectralDensity))]
    private double _soundPacketLength = 3.2;

    [ObservableProperty]
    private double _soundSpeed = 5.742;

    public double SpectralDensity => SoundPacketLength != 0 ? BandWidth / SoundPacketLength : 0;

    protected override void OnBandWidthChanged()
    {
        base.OnBandWidthChanged();

        OnPropertyChanged(nameof(SpectralDensity));
    }

    public AODWaveformGenerator.ChirpAODWaveformParam AdaptTo() => new(SoundPacketLength, SoundSpeed)
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
        P2CompensationCoefficient = P2CompensationCoefficient,
        P3CompensationCoefficient = P3CompensationCoefficient,
        P4CompensationCoefficient = P4CompensationCoefficient,
        P5CompensationCoefficient = P5CompensationCoefficient,
        P6CompensationCoefficient = P6CompensationCoefficient,
        P7CompensationCoefficient = P7CompensationCoefficient,
        P8CompensationCoefficient = P8CompensationCoefficient
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
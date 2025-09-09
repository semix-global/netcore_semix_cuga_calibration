namespace Core.Models.Models.Common.AODWaveform.UI.Generates;

public sealed partial class GenerateChirpAODWaveformParamUserControl
{
    protected override GenerateAODWaveformParamUserControl InnerControl => GenerateAODWaveformParamUserControl;

    public GenerateChirpAODWaveformParamUserControl()
    {
        InitializeComponent();
        InitializeTransparentProperties();
    }
}
using Core.Models.Models.Common.Cookies;
using Net.Utilities.WPF.MVVM;

namespace Core.Models.Models.Common.AODWaveform.UI.Generates;

public sealed partial class GenerateAODWaveformParamUserControl
{
    public ApplicationCookie ApplicationCookie => HostApplication.GetRequiredService<ApplicationCookie>();

    protected override GenerateAODWaveformParamUserControl InnerControl => this;

    public GenerateAODWaveformParamUserControl()
    {
        InitializeComponent();

        InitializeTransparentProperties();
    }
}
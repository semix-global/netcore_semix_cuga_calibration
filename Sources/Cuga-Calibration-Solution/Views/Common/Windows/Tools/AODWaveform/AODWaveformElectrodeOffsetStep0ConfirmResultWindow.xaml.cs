using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(AODWaveformElectrodeOffsetStep0ConfirmResultWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class AODWaveformElectrodeOffsetStep0ConfirmResultWindow
{
    public AODWaveformElectrodeOffsetStep0ConfirmResultWindow()
    {
        InitializeComponent();
    }
}
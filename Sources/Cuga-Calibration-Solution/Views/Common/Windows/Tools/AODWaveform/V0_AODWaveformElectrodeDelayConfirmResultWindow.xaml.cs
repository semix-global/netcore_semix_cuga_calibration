using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(V0AODWaveformElectrodeDelayConfirmResultWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class V0AODWaveformElectrodeDelayConfirmResultWindow
{
    public V0AODWaveformElectrodeDelayConfirmResultWindow()
    {
        InitializeComponent();
    }
}
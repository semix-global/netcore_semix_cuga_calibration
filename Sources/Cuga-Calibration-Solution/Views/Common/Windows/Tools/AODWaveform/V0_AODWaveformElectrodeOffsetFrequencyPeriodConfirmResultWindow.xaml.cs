using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(V0AODWaveformElectrodeOffsetFrequencyPeriodConfirmResultWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class V0AODWaveformElectrodeOffsetFrequencyPeriodConfirmResultWindow
{
    public V0AODWaveformElectrodeOffsetFrequencyPeriodConfirmResultWindow()
    {
        InitializeComponent();
    }
}
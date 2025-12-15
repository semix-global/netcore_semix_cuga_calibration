using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(AODWaveformElectrodeOffsetFrequencyPeriodConfirmResultWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class AODWaveformElectrodeOffsetFrequencyPeriodConfirmResultWindow
{
    public AODWaveformElectrodeOffsetFrequencyPeriodConfirmResultWindow()
    {
        InitializeComponent();
    }
}
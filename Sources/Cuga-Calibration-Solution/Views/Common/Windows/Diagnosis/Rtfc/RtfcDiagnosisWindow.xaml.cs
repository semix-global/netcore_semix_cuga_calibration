using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Diagnosis.Rtfc;

[IOCAppService(ServiceType = typeof(RtfcDiagnosisWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class RtfcDiagnosisWindow
{
    public RtfcDiagnosisWindow()
    {
        InitializeComponent();
    }
}
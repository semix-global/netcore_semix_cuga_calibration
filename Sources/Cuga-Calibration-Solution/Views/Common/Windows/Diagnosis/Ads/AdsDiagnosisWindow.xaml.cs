using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Diagnosis.Ads;

[IOCAppService(ServiceType = typeof(AdsDiagnosisWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class AdsDiagnosisWindow
{
    public AdsDiagnosisWindow()
    {
        InitializeComponent();
    }
}
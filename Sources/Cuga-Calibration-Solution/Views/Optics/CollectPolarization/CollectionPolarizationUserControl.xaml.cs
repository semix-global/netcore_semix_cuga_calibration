using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Optics.CollectPolarization;

[Permission]
[IOCAppService(ServiceType = typeof(CollectionPolarizationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class CollectionPolarizationUserControl
{
    public CollectionPolarizationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}
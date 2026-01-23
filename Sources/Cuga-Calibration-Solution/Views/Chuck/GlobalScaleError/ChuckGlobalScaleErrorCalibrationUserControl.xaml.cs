using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Chuck.GlobalScaleError;

[PermissionControl]
[IOCAppService(ServiceType = typeof(ChuckGlobalScaleErrorCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class ChuckGlobalScaleErrorCalibrationUserControl
{
    public ChuckGlobalScaleErrorCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}
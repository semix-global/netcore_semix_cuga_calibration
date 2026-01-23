using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Chuck.Prealigner;

[Permission]
[IOCAppService(ServiceType = typeof(ChuckPrealignerCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckPrealignerCalibrationUserControl
{
    public ChuckPrealignerCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}
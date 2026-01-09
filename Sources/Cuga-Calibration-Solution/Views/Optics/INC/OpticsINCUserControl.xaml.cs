using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Optics.INC;

[IOCAppService(ServiceType = typeof(OpticsINCUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsINCUserControl
{
    [Permission]
    public OpticsINCUserControl()
    {
        InitializeComponent();
    }
}
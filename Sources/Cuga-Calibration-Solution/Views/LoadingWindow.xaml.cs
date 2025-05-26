using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views;

[IOCAppService(ServiceType = typeof(LoadingWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class LoadingWindow
{
    public LoadingWindow()
    {
        InitializeComponent();
    }
}
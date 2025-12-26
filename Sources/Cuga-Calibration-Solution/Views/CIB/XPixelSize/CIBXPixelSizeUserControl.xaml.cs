using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.CIB.XPixelSize;

[IOCAppService(ServiceType = typeof(CIBXPixelSizeUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBXPixelSizeUserControl
{
    [Permission]
    public CIBXPixelSizeUserControl()
    {
        InitializeComponent();
    }
}
using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.CIB.XPixelSize;

[IOCAppService(ServiceType = typeof(CIBXPixelSizeCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBXPixelSizeCalibrationUserControl
{
    [Permission]
    public CIBXPixelSizeCalibrationUserControl()
    {
        InitializeComponent();
    }
}
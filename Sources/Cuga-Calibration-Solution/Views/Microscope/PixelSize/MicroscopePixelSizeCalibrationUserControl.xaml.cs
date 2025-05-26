using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Microscope.PixelSize;

[IOCAppService(ServiceType = typeof(MicroscopePixelSizeCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopePixelSizeCalibrationUserControl
{
    [Permission]
    public MicroscopePixelSizeCalibrationUserControl()
    {
        InitializeComponent();
    }
}
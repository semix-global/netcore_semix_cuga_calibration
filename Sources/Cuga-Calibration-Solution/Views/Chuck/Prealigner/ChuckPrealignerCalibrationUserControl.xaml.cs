using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Chuck.Prealigner;

[IOCAppService(ServiceType = typeof(ChuckPrealignerCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckPrealignerCalibrationUserControl
{
    [Permission]
    public ChuckPrealignerCalibrationUserControl()
    {
        InitializeComponent();
    }
}
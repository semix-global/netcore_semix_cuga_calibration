using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(AodGenerateWaveFileWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class AodGenerateWaveFileWindow
{
    public AodGenerateWaveFileWindow()
    {
        InitializeComponent();
    }
}
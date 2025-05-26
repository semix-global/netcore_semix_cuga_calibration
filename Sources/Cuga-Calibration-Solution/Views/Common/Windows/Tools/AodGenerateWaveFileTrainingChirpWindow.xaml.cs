using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(AodGenerateWaveFileTrainingChirpWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class AodGenerateWaveFileTrainingChirpWindow
{
    public AodGenerateWaveFileTrainingChirpWindow()
    {
        InitializeComponent();
    }
}
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(AodGenerateWaveFileTrainingChirp2Window), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class AodGenerateWaveFileTrainingChirp2Window
{
    public AodGenerateWaveFileTrainingChirp2Window()
    {
        InitializeComponent();
    }
}
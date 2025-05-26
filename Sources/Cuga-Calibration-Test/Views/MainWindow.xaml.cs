using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibrationTest.Views;

[IOCAppService(ServiceType = typeof(MainWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibrationTest.Views;

[ObservableObject]
[IOCAppService(ServiceType = typeof(SplitImageWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class SplitImageWindow
{
    public SplitImageWindow()
    {
        InitializeComponent();
    }
}
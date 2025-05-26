using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools;

// ReSharper disable InconsistentNaming
[IOCAppService(ServiceType = typeof(EFEMWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class EFEMWindow
{
    public EFEMWindow()
    {
        InitializeComponent();
    }
}
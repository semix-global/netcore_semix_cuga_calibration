using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace Core.Utilities.WPF.ApplicationAbout.View;

[IOCAppService(ServiceType = typeof(ShowMarkDownWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class ShowMarkDownWindow
{
    public ShowMarkDownWindow()
    {
        InitializeComponent();
    }
}
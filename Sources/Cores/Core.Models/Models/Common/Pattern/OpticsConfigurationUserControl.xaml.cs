using Core.Models.Models.Common.Cookies;
using Net.Utilities.WPF.MVVM;

namespace Core.Models.Models.Common.Pattern;

public partial class OpticsConfigurationUserControl
{
    public ApplicationCookie ApplicationCookie => HostApplication.GetRequiredService<ApplicationCookie>();
    
    public OpticsConfigurationUserControl()
    {
        InitializeComponent();
    }
}
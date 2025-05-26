using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting;

public abstract class SettingWindowViewModelBase : ViewModelBase
{
    public virtual Task<bool> SavingAsync() => Task.FromResult(true);

    public virtual bool Closing()
    {
        return true;
    }
}
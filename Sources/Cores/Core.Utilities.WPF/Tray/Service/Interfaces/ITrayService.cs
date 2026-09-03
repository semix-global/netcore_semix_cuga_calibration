using Core.Utilities.WPF.Tray.Model;

namespace Core.Utilities.WPF.Tray.Service.Interfaces;

public interface ITrayService
{
    void Initialize(TrayOptions options);

    void Dispose();
}
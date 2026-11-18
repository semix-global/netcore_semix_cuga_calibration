using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Cookies;
using Microsoft.Extensions.Hosting;

namespace CugaCalibration.ViewModels;

public partial class CalibrationViewModelBase
{
    public bool IsDevelopment { get; private set; }

    [ObservableProperty]
    public partial ApplicationCookie.SimulatorDirectoryInformation SimulatorDirectoryInformation { get; set; }

    private void InitializeSimulator()
    {
        IsDevelopment = HostEnvironment.IsDevelopment();
        if (IsDevelopment) SimulatorDirectoryInformation = ApplicationCookie.SimulatorDirectoryInformations[0];
    }
}
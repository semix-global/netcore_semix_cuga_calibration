using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Cookies;
using Microsoft.Extensions.Hosting;

namespace CugaCalibration.ViewModels;

public partial class CalibrationViewModelBase
{
    [ObservableProperty]
    public partial ApplicationCookie.SimulatorDirectoryInformation SimulatorDirectoryInformation { get; set; }

    private void InitializeSimulator()
    {
        if (HostEnvironment.IsDevelopment()) SimulatorDirectoryInformation = ApplicationCookie.SimulatorDirectoryInformations[0];
    }
}
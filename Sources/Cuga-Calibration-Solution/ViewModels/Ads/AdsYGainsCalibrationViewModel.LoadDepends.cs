using Core.Models.Models.Ads.PressureGains;
using Local.NoSQL.DB.Providers.Interfaces;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CugaCalibration.ViewModels.Ads;

public sealed partial class AdsYGainsCalibrationViewModel
{
    protected async Task<bool> LoadDependsAsync(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<AdsPressureGainsDto>(out _, out var errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }
        return true;
    }
}

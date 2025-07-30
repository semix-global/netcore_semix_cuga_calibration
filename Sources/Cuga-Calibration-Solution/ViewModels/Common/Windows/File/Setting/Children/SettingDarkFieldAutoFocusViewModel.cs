using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Setting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(SettingDarkFieldAutoFocusViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SettingDarkFieldAutoFocusViewModel(
    ILogger<SettingDarkFieldAutoFocusViewModel> logger,
    LaserViewModel laserViewModel,
    StageViewModel stageViewModel) : SettingWindowViewModelBase
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private SettingDarkFieldAutoFocusParam _settingDarkFieldAutoFocusParam = new();

    [RelayCommand]
    private Task ChuckAutoRtfcAsync() => Task.Run(action: () => ChuckAfAutoRtfc(Point.Origin, Guid.NewGuid(), true));

    public (bool IsSuccess, (double Ecs, double Height) Result) ChuckAfAutoRtfc(Point position, Guid htmlLogUniqueId, bool isContainsEnd = false)
    {
        var isSuccess = false;
        try
        {
            try
            {
                logger.LogHtmlInformation("RTFC Start", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

                logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
                {
                    OpticsMagTypeEnum,
                    position
                }), htmlLogUniqueId.LoggingHtml());

                stageViewModel.SetBrightFieldAbsoluteStageXy(position);

                var (ecs, afMotor) = laserViewModel.RuntimeAfCalibration(position);

                SettingDarkFieldAutoFocusParam.ChuckEcsValue = ecs;
                SettingDarkFieldAutoFocusParam.ChuckMotorValue = afMotor;
                SettingDarkFieldAutoFocusParam.IsEnableChuck = true;

                logger.LogHtmlInformation("Ok", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    SettingDarkFieldAutoFocusParam.ChuckEcsValue,
                    SettingDarkFieldAutoFocusParam.ChuckMotorValue
                }), htmlLogUniqueId.LoggingHtml());

                isSuccess = true;
                return (isSuccess, (ecs, afMotor));
            }
            finally
            {
                stageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);
            }
        }
        catch (Exception ex)
        {
            logger.LogHtmlCritical(ex, "Critical", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());
            return (false, (0, 0));
        }
        finally
        {
            if (isContainsEnd)
            {
                logger.LogHtmlInformation(htmlLogUniqueId.LoggedEndHtml(
                    $"ChuckAfAutoRtfc_Mag({EnumHelper.ToDescriptionString(OpticsMagTypeEnum)})_Position({position})_{(isSuccess ? "OK" : "Failed")}"));
            }
        }
    }
}
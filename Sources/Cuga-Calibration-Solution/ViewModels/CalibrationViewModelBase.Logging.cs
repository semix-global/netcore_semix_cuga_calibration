using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;

namespace CugaCalibration.ViewModels;

public partial class CalibrationViewModelBase
{
    protected void LogFailure(string reason, HtmlHeaderLevelEnum headerLevel, string handle)
    {
        Logger.LogHtmlHeaderIsError(headerLevel, new HtmlBullet(new
        {
            Reason = reason,
            Lens = CalibrateFileName,
            Step = IsCalibrating ? $"{CalibrationStepIndex + 1}. {CalibrationSteps[CalibrationStepIndex].StepName}" : "Verify",
            Handle = handle
        }
        ), HtmlLogUniqueId.LoggingHtml());
    }
}
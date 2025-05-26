using Net.Utilities.Enums;
using Net.Utilities.Helper.File;
using Net.Utilities.Nlog.Entities;
using NLog;
using NLog.LayoutRenderers;
using System.Text;

namespace Net.Utilities.Nlog.Layouts;

[LayoutRenderer("htmlCalibrationLayout")]
public sealed class HtmlCalibrationLayoutRender : LayoutRenderer
{
    protected override void Append(StringBuilder builder, LogEventInfo logEvent)
    {
        if (logEvent.Parameters?.Last() is not HtmlLogUnique htmlLogUnique) return;
        if (htmlLogUnique.HtmlLogUniqueTypeEnum is HtmlLogUniqueTypeEnum.LoggingPeek
                or HtmlLogUniqueTypeEnum.LoggedEnd
            && string.IsNullOrWhiteSpace(htmlLogUnique.FileName) == false)
            if (htmlLogUnique.FileName.StartsWith(CalibrationTypeEnum.HandleCalibration.ToString()))
                builder.Append(FileHelper.RemoveInvalidFileName(CalibrationTypeEnum.HandleCalibration.ToString()));
        if (htmlLogUnique.FileName.StartsWith(CalibrationTypeEnum.HandleVerify.ToString())) builder.Append(FileHelper.RemoveInvalidFileName(CalibrationTypeEnum.HandleVerify.ToString()));
        if (htmlLogUnique.FileName.StartsWith(CalibrationTypeEnum.AutoCalibration.ToString())) builder.Append(FileHelper.RemoveInvalidFileName(CalibrationTypeEnum.AutoCalibration.ToString()));
        if (htmlLogUnique.FileName.StartsWith(CalibrationTypeEnum.AutoVerify.ToString())) builder.Append(FileHelper.RemoveInvalidFileName(CalibrationTypeEnum.AutoVerify.ToString()));
    }
}
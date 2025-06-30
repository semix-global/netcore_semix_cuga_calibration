using System.Text;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Nlog.Entities;
using NLog;
using NLog.LayoutRenderers;

namespace Core.Utilities;

[LayoutRenderer("htmlCalibrationLayout")]
public sealed class HtmlCalibrationLayoutRender : LayoutRenderer
{
    protected override void Append(StringBuilder builder, LogEventInfo logEvent)
    {
        if (logEvent.Parameters?.Last() is not HtmlLogUnique htmlLogUnique) return;
        if (htmlLogUnique.HtmlLogUniqueTypeEnum is HtmlLogUniqueTypeEnum.LoggingPeek
                or HtmlLogUniqueTypeEnum.LoggedEnd
            && string.IsNullOrWhiteSpace(htmlLogUnique.FileName) == false)
            if (htmlLogUnique.FileName.StartsWith(nameof(CalibrationTypeEnum.HandleCalibration)))
                builder.Append(FileHelper.RemoveInvalidFileName(nameof(CalibrationTypeEnum.HandleCalibration)));
        if (htmlLogUnique.FileName.StartsWith(nameof(CalibrationTypeEnum.HandleVerify))) builder.Append(FileHelper.RemoveInvalidFileName(nameof(CalibrationTypeEnum.HandleVerify)));
        if (htmlLogUnique.FileName.StartsWith(nameof(CalibrationTypeEnum.AutoCalibration))) builder.Append(FileHelper.RemoveInvalidFileName(nameof(CalibrationTypeEnum.AutoCalibration)));
        if (htmlLogUnique.FileName.StartsWith(nameof(CalibrationTypeEnum.AutoVerify))) builder.Append(FileHelper.RemoveInvalidFileName(nameof(CalibrationTypeEnum.AutoVerify)));
    }
}
using Net.Utilities.Helper.File;
using Net.Utilities.Nlog.Entities;
using NLog;
using NLog.LayoutRenderers;
using System.Text;

namespace Net.Utilities.Nlog.Layouts;

[LayoutRenderer("htmlFileLayout")]
public sealed class HtmlFileLayoutRender : LayoutRenderer
{
    protected override void Append(StringBuilder builder, LogEventInfo logEvent)
    {
        if (logEvent.Parameters?.Last() is not HtmlLogUnique htmlLogUnique) return;
        if (htmlLogUnique.HtmlLogUniqueTypeEnum is HtmlLogUniqueTypeEnum.LoggingPeek or HtmlLogUniqueTypeEnum.LoggedEnd && string.IsNullOrWhiteSpace(htmlLogUnique.FileName) == false)
            builder.Append(FileHelper.RemoveInvalidFileName(htmlLogUnique.FileName));
    }
}
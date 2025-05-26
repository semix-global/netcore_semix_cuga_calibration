using Net.Utilities.Nlog.Entities;
using Net.Utilities.Nlog.Entities.HtmlElements;
using NLog;
using NLog.LayoutRenderers;
using System.Text;

namespace Net.Utilities.Nlog.Layouts;

[LayoutRenderer("htmlViewLayout")]
public sealed class HtmlViewLayoutRender : LayoutRenderer
{
    protected override void Append(StringBuilder builder, LogEventInfo logEventInfo)
    {
        if (logEventInfo.Parameters?.LastOrDefault() is not HtmlLogUnique)
        {
            builder.Append(logEventInfo.FormattedMessage);
            return;
        }

        switch (logEventInfo.Parameters)
        {
            case [AbstractHtmlElement htmlElement, HtmlLogUnique]:
                builder.Append(htmlElement.ToViewString());

                break;

            case [string header, HtmlHeaderLevelEnum, HtmlLogUnique]:
                builder.Append(header);
                break;

            case [string header, HtmlHeaderLevelEnum, AbstractHtmlElement htmlElement, HtmlLogUnique]:
                builder.Append(header);
                builder.Append(Environment.NewLine);
                builder.Append(htmlElement.ToViewString());

                break;
        }
    }
}
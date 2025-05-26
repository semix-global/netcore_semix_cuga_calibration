using Net.Utilities.Constants;
using Net.Utilities.Nlog.Entities;

namespace Net.Utilities.Nlog.Extensions;

public static class HtmlLogUniqueExtension
{
    public static HtmlLogUnique LoggingHtml(this Guid guid) => new(guid, HtmlLogUniqueTypeEnum.Logging);

    public static HtmlLogUnique LoggingPeekHtml(this Guid guid, string fileName = ConstantHelper.EmptyString) => new(guid, HtmlLogUniqueTypeEnum.LoggingPeek, fileName);

    public static HtmlLogUnique LoggingClearHtml(this Guid guid) => new(guid, HtmlLogUniqueTypeEnum.LoggingClear);

    public static HtmlLogUnique LoggedEndHtml(this Guid guid, string fileName = ConstantHelper.EmptyString) => new(guid, HtmlLogUniqueTypeEnum.LoggedEnd, fileName);
}
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace Net.Utilities.Nlog.Extensions;

internal static class HtmlHeaderLevelEnumExtension
{
    internal static string ToMark(this HtmlHeaderLevelEnum htmlHeaderLevelEnum) => htmlHeaderLevelEnum switch
    {
        HtmlHeaderLevelEnum.Header1 => "h1",
        HtmlHeaderLevelEnum.Header2 => "h2",
        HtmlHeaderLevelEnum.Header3 => "h3",
        HtmlHeaderLevelEnum.Header4 => "h4",
        HtmlHeaderLevelEnum.Header5 => "h5",
        HtmlHeaderLevelEnum.Header6 => "h6",
        HtmlHeaderLevelEnum.Header7 => "p",
        _ => throw new ArgumentOutOfRangeException(nameof(htmlHeaderLevelEnum), htmlHeaderLevelEnum, null)
    };

    internal static string ToClassOfTextSize(this HtmlHeaderLevelEnum htmlHeaderLevelEnum) => htmlHeaderLevelEnum switch
    {
        HtmlHeaderLevelEnum.Header1 => "text-3xl",
        HtmlHeaderLevelEnum.Header2 => "text-2xl",
        HtmlHeaderLevelEnum.Header3 => "text-xl",
        HtmlHeaderLevelEnum.Header4 => "text-lg",
        HtmlHeaderLevelEnum.Header5 => "text-base",
        HtmlHeaderLevelEnum.Header6 => "text-sm",
        HtmlHeaderLevelEnum.Header7 => "text-xs",
        _ => throw new ArgumentOutOfRangeException(nameof(htmlHeaderLevelEnum), htmlHeaderLevelEnum, null)
    };

    internal static string ToClassOfMarginLeft(this HtmlHeaderLevelEnum htmlHeaderLevelEnum) => htmlHeaderLevelEnum switch
    {
        HtmlHeaderLevelEnum.Header1 => "ml-0",
        HtmlHeaderLevelEnum.Header2 => "ml-1",
        HtmlHeaderLevelEnum.Header3 => "ml-2",
        HtmlHeaderLevelEnum.Header4 => "ml-3",
        HtmlHeaderLevelEnum.Header5 => "ml-4",
        HtmlHeaderLevelEnum.Header6 => "ml-5",
        HtmlHeaderLevelEnum.Header7 => "ml-6",
        _ => throw new ArgumentOutOfRangeException(nameof(htmlHeaderLevelEnum), htmlHeaderLevelEnum, null)
    };

    internal static bool ToIsExpand(this HtmlHeaderLevelEnum htmlHeaderLevelEnum) => htmlHeaderLevelEnum <= HtmlHeaderLevelEnum.Header5;
}
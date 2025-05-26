using Microsoft.Extensions.Logging;
using Net.Utilities.Nlog.Entities;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace Net.Utilities.Nlog.Extensions;

public static partial class HtmlLogExtension
{
    #region Invoke

    public static void LogHtmlHeaderIsOk(this ILogger logger, HtmlHeaderLevelEnum htmlHeaderLevelEnum, AbstractHtmlElement htmlElement, HtmlLogUnique htmlLogUnique)
        => logger.LogHtmlInformation("OK", htmlHeaderLevelEnum, htmlElement, htmlLogUnique);

    public static void LogHtmlHeaderIsError(this ILogger logger, HtmlHeaderLevelEnum htmlHeaderLevelEnum, AbstractHtmlElement htmlElement, HtmlLogUnique htmlLogUnique)
        => logger.LogHtmlError("Error", htmlHeaderLevelEnum, htmlElement, htmlLogUnique);

    #endregion Invoke

    #region Empty

    [LoggerMessage(LogLevel.Debug, Message = $$"""{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlDebug(this ILogger logger, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Information, Message = $$"""{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlInformation(this ILogger logger, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Trace, Message = $$"""{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlTrace(this ILogger logger, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Warning, Message = $$"""{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlWarning(this ILogger logger, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Error, Message = $$"""{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlError(this ILogger logger, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Critical, Message = $$"""{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlCritical(this ILogger logger, HtmlLogUnique htmlLogUnique);

    #endregion Empty

    #region string, HtmlHeaderLevelEnum

    [LoggerMessage(LogLevel.Trace, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlTrace(this ILogger logger, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Debug, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlDebug(this ILogger logger, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Information, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlInformation(this ILogger logger, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Warning, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlWarning(this ILogger logger, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Error, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlError(this ILogger logger, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Critical, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlCritical(this ILogger logger, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, HtmlLogUnique htmlLogUnique);

    #endregion string, HtmlHeaderLevelEnum

    #region Exception, string, HtmlHeaderLevelEnum

    [LoggerMessage(LogLevel.Trace, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlTrace(this ILogger logger, Exception? exception, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Debug, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlDebug(this ILogger logger, Exception? exception, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Information, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlInformation(this ILogger logger, Exception? exception, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Warning, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlWarning(this ILogger logger, Exception? exception, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Error, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlError(this ILogger logger, Exception? exception, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Critical, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlCritical(this ILogger logger, Exception? exception, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, HtmlLogUnique htmlLogUnique);

    #endregion Exception, string, HtmlHeaderLevelEnum

    #region string, HtmlHeaderLevelEnum, AbstractHtmlElement

    [LoggerMessage(LogLevel.Trace, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlElement)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlTrace(this ILogger logger, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, AbstractHtmlElement htmlElement, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Debug, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlElement)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlDebug(this ILogger logger, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, AbstractHtmlElement htmlElement, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Information, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlElement)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlInformation(this ILogger logger, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, AbstractHtmlElement htmlElement, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Warning, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlElement)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlWarning(this ILogger logger, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, AbstractHtmlElement htmlElement, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Error, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlElement)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlError(this ILogger logger, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, AbstractHtmlElement htmlElement, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Critical, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlElement)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlCritical(this ILogger logger, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, AbstractHtmlElement htmlElement, HtmlLogUnique htmlLogUnique);

    #endregion string, HtmlHeaderLevelEnum, AbstractHtmlElement

    #region Exception, string, HtmlHeaderLevelEnum, AbstractHtmlElement

    [LoggerMessage(LogLevel.Trace, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlElement)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlTrace(this ILogger logger, Exception? exception, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, AbstractHtmlElement htmlElement, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Debug, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlElement)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlDebug(this ILogger logger, Exception? exception, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, AbstractHtmlElement htmlElement, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Information, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlElement)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlInformation(this ILogger logger, Exception? exception, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, AbstractHtmlElement htmlElement, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Warning, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlElement)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlWarning(this ILogger logger, Exception? exception, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, AbstractHtmlElement htmlElement, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Error, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlElement)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlError(this ILogger logger, Exception? exception, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, AbstractHtmlElement htmlElement, HtmlLogUnique htmlLogUnique);

    [LoggerMessage(LogLevel.Critical, Message = $$"""{@{{nameof(header)}}}{@{{nameof(htmlHeaderLevelEnum)}}}{@{{nameof(htmlElement)}}}{@{{nameof(htmlLogUnique)}}}""")]
    public static partial void LogHtmlCritical(this ILogger logger, Exception? exception, string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, AbstractHtmlElement htmlElement, HtmlLogUnique htmlLogUnique);

    #endregion Exception, string, HtmlHeaderLevelEnum, AbstractHtmlElement
}
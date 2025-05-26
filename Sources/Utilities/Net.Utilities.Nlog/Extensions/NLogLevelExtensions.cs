using Net.Utilities.Enums;

namespace Net.Utilities.Nlog.Extensions;

public static class NLogLevelExtensions
{
    public static NLog.LogLevel ToNLogLevel(this LogLevelEnum logLevelEnum)
    {
        return logLevelEnum switch
        {
            LogLevelEnum.Trace => NLog.LogLevel.Trace,
            LogLevelEnum.Debug => NLog.LogLevel.Debug,
            LogLevelEnum.Info => NLog.LogLevel.Info,
            LogLevelEnum.Warn => NLog.LogLevel.Warn,
            LogLevelEnum.Error => NLog.LogLevel.Error,
            LogLevelEnum.Fatal => NLog.LogLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevelEnum), logLevelEnum, null)
        };
    }

    public static LogLevelEnum ToLogLevelEnum(this NLog.LogLevel logLevel)
    {
        return logLevel.Name switch
        {
            { } s when s == NLog.LogLevel.Trace.Name => LogLevelEnum.Trace,
            { } s when s == NLog.LogLevel.Debug.Name => LogLevelEnum.Debug,
            { } s when s == NLog.LogLevel.Info.Name => LogLevelEnum.Info,
            { } s when s == NLog.LogLevel.Warn.Name => LogLevelEnum.Warn,
            { } s when s == NLog.LogLevel.Error.Name => LogLevelEnum.Error,
            { } s when s == NLog.LogLevel.Fatal.Name => LogLevelEnum.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
        };
    }
}
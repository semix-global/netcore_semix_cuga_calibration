using CommunityToolkit.Diagnostics;
using Net.Utilities.Enums;

namespace Net.Utilities.Nlog.Extensions;

internal static class LogLevelEnumExtensions
{
    internal static string ToColor(this LogLevelEnum logLevelEnum) => logLevelEnum switch
    {
        LogLevelEnum.Trace => "gray",
        LogLevelEnum.Debug => "blue",
        LogLevelEnum.Info => "green",
        LogLevelEnum.Warn => "orange",
        LogLevelEnum.Error => "red",
        LogLevelEnum.Fatal => "red",
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(logLevelEnum))
    };

    internal static string ToSvg(this LogLevelEnum logLevelEnum) => logLevelEnum switch
    {
        LogLevelEnum.Trace => """
                              <circle cx="12" cy="12" r="10"></circle>
                              <path d="M12 16v-4"></path>
                              <path d="M12 8h.01"></path>
                              """,
        LogLevelEnum.Debug => """
                              <circle cx="12" cy="12" r="10"></circle>
                              <path d="M12 16v-4"></path>
                              <path d="M12 8h.01"></path>
                              """,
        LogLevelEnum.Info => """
                             <path d="M12 22c5.523 0 10-4.477 10-10S17.523 2 12 2 2 6.477 2 12s4.477 10 10 10z"></path>
                             <path d="m9 12 2 2 4-4"></path>
                             """,
        LogLevelEnum.Warn => """
                             <path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"></path>
                             <path d="M12 9v4"></path>
                             <path d="M12 17h.01"></path>
                             """,
        LogLevelEnum.Error => """
                              <path d="M18 6 6 18"></path>
                              <path d="m6 6 12 12"></path>
                              """,
        LogLevelEnum.Fatal => """
                              <path d="M18 6 6 18"></path>
                              <path d="m6 6 12 12"></path>
                              """,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(logLevelEnum))
    };
}
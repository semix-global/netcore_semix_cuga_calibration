using NLog;
using NLog.Config;
using NLog.Targets;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Net.Utilities.WPF.UI.Nlog;

[Target("NLogViewer")]
public sealed class NLogViewerTarget : TargetWithLayout
{
    public static (NLogViewerTarget? NLogViewerTarget, LoggingRule? LoggingRule) GetInstance(string targetName)
    {
        var loggingConfiguration = LogManager.Configuration;
        var nLogViewerTarget = loggingConfiguration.AllTargets
            .OfType<NLogViewerTarget>()
            .SingleOrDefault(t => t.Name == targetName || t.Name == $"{targetName}_wrapped");

        var richTextBoxRule = loggingConfiguration.LoggingRules
            .SingleOrDefault(rule => rule.Targets.Any(t => t.Name == targetName));

        return (nLogViewerTarget, richTextBoxRule);
    }

    /// <summary>
    /// 缓存日志最大数量(最大值64)
    /// </summary>
    public int MaxCount { get; set; } = 64;

    public IObservable<LogEventInfo> Cache => _cacheSubject.AsObservable();

    private readonly ReplaySubject<LogEventInfo> _cacheSubject;

    public NLogViewerTarget()
    {
        _cacheSubject = new ReplaySubject<LogEventInfo>(MaxCount);
    }

    protected override void Write(LogEventInfo logEvent)
    {
        _cacheSubject.OnNext(logEvent);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing) _cacheSubject.Dispose();
    }
}
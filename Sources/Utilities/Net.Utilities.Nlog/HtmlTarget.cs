using Net.Utilities.Helper.File;
using Net.Utilities.Nlog.Entities;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;

namespace Net.Utilities.Nlog;

[Target("Html")]
public sealed class HtmlTarget : AsyncTaskTarget
{
    static HtmlTarget()
    {
        LogManager.LogFactory.Setup().SetupLogFactory(ext => ext.AddCallSiteHiddenAssembly(typeof(HtmlTarget).Assembly));
    }

    private readonly ConcurrentDictionary<Guid, Html> _cacheDictionary = [];
    private readonly Channel<(HtmlLogUnique HtmlLogUnique, LogEventInfo LogEventInfo)> _channel = Channel.CreateUnbounded<(HtmlLogUnique, LogEventInfo)>();

    [RequiredParameter]
    public required Layout DateTime { get; set; }

    [RequiredParameter]
    public required Layout Version { get; set; }

    [RequiredParameter]
    public required Layout Logger { get; set; }

    [RequiredParameter]
    public required Layout Thread { get; set; }

    [RequiredParameter]
    public required Layout CallSite { get; set; }

    [RequiredParameter]
    public required Layout StackTrace { get; set; }

    [RequiredParameter]
    public required Layout Exception { get; set; }

    [RequiredParameter]
    public required Layout FileName { get; set; }

    protected override void InitializeTarget()
    {
        base.InitializeTarget();
        _ = Task.Run(async () =>
        {
            while (_channel.Reader.Completion.IsCompleted == false)
            {
                try
                {
                    var (htmlLogUnique, logEventInfo) = await _channel.Reader.ReadAsync().ConfigureAwait(false);
                    Write(htmlLogUnique, logEventInfo);
                }
                catch (Exception ex)
                {
                    if (ex is ChannelClosedException) return;
                    InternalLogger.Log(ex, LogLevel.Fatal, nameof(HtmlTarget));
                }
            }
        });
    }

    protected override async Task WriteAsyncTask(LogEventInfo logEvent, CancellationToken cancellationToken)
    {
        try
        {
            if (logEvent.Parameters?.LastOrDefault() is not HtmlLogUnique htmlLogUnique) return;

            await _channel.Writer.WriteAsync((HtmlLogUnique: htmlLogUnique, LogEventInfo: logEvent), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            InternalLogger.Log(ex, LogLevel.Fatal, nameof(HtmlTarget));
        }
    }

    protected override void CloseTarget()
    {
        base.CloseTarget();
        _channel.Writer.Complete();
        foreach (var html in _cacheDictionary.Select(t => t.Value)) html.ElementList.Clear();

        _cacheDictionary.Clear();
    }

    private Html Get(Guid guid)
    {
        return _cacheDictionary.GetOrAdd(guid, new Html());
    }

    private void Remove(Guid guid)
    {
        if (_cacheDictionary.TryRemove(guid, out var html)) html.ElementList.Clear();
    }

    private void Write(HtmlLogUnique htmlLogUnique, LogEventInfo logEventInfo)
    {
        try
        {
            switch (htmlLogUnique.HtmlLogUniqueTypeEnum)
            {
                case HtmlLogUniqueTypeEnum.Logging:
                    Logging();
                    break;

                case HtmlLogUniqueTypeEnum.LoggingPeek:
                    Save();
                    break;

                case HtmlLogUniqueTypeEnum.LoggingClear:
                    Remove(htmlLogUnique.Guid);
                    break;

                case HtmlLogUniqueTypeEnum.LoggedEnd:
                    Save();
                    Remove(htmlLogUnique.Guid);
                    break;
            }

            return;

            void Logging()
            {
                var html = Get(htmlLogUnique.Guid);
                var datetime = DateTime.Render(logEventInfo);
                var version = Version.Render(logEventInfo);
                var htmlNLog = logEventInfo.Exception is null
                    ? new HtmlLog(logEventInfo.Level.ToLogLevelEnum(), datetime, version, [])
                    : new HtmlLog(logEventInfo.Level.ToLogLevelEnum(), datetime, version, [
                        new HtmlQuote(new
                        {
                            Logger = Logger.Render(logEventInfo),
                            Thread = Thread.Render(logEventInfo),
                            CallSite = CallSite.Render(logEventInfo),
                            StackTrace = StackTrace.Render(logEventInfo),
                            Exception = Exception.Render(logEventInfo)
                        })
                    ]);

                switch (logEventInfo.Parameters)
                {
                    case [string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, HtmlLogUnique]:
                        html.Add(header, htmlHeaderLevelEnum, logEventInfo.Level.ToLogLevelEnum(), htmlNLog.ContentList.Count > 0 ? htmlNLog : null);

                        break;

                    case [string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, AbstractHtmlElement htmlElement, HtmlLogUnique]:
                        html.Add(header, htmlHeaderLevelEnum, logEventInfo.Level.ToLogLevelEnum(), htmlElement, htmlNLog);

                        break;
                }
            }

            void Save()
            {
                var html = Get(htmlLogUnique.Guid);
                var filePath = FileName.Render(logEventInfo);
                DirectoryHelper.CreateFileDirectoryIfNotExists(filePath);

                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write); // 如果存在就覆盖
                using var writer = new StreamWriter(fs, Encoding.UTF8);

                html.WriteToHtml(writer);
            }
        }
        catch (Exception ex)
        {
            InternalLogger.Error(ex, "{0}: Failed to append html", this);
        }
    }
}
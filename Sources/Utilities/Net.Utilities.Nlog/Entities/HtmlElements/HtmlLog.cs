using Humanizer;
using Net.Utilities.Enums;
using Net.Utilities.Nlog.Extensions;

namespace Net.Utilities.Nlog.Entities.HtmlElements;

public sealed record HtmlLog(
    LogLevelEnum LogLevelEnum,
    string DateTime,
    string Version,
    List<AbstractHtmlElement> ContentList) : AbstractHtmlElement
{
    internal override void WriteToHtml(TextWriter writer)
    {
        var color = LogLevelEnum.ToColor();
        var svg = LogLevelEnum.ToSvg();

        writer.WriteLine($"""
                          <div class="w-full bg-{color}-50 border-t-2 border-{color}-500 rounded shadow p-1 text-{color}-800">
                              <div @click="toggleLogVisibility($event, $el)" class="flex items-center cursor-pointer select-none">
                                  <div class="shrink-0 mr-2">
                                      <span class="inline-flex justify-center items-center size-8 rounded-full border-4 border-{color}-100 bg-{color}-200 text-{color}-800">
                                          <svg class="shrink-0 size-4" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                              {svg}
                                          </svg>
                                      </span>
                                  </div>
                                  <h3 class="flex-1 text-base font-medium">
                                      <span class="mr-2"><span class="font-bold">{LogLevelEnum.Humanize(LetterCasing.Title)}</span>,</span>
                                      <span class="mr-2"><span class="font-bold">{nameof(Version)}: </span>{((HtmlBlank)Version).ToContentHtml()},</span>
                                      <span class="mr-2"><span class="font-bold">{nameof(DateTime)}: </span>{((HtmlBlank)DateTime).ToContentHtml()}</span>
                                  </h3>
                                  <svg data-log-svg-rotate="true" class="size-4 mr-2" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="4" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                      <path d="M19 9l-7 7-7-7"/>
                                  </svg>
                              </div>
                              <div class="space-y-1" data-log-visible="true">
                          """);

        foreach (var content in ContentList)
        {
            content.WriteToHtml(writer);
        }

        writer.WriteLine("""
                             </div>
                         </div>
                         """);
    }

    internal override string ToViewString() => string.Join(Environment.NewLine,
        from c in ContentList
        select c.ToViewString());
}
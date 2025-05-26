using Net.Utilities.Enums;
using Net.Utilities.Nlog.Extensions;
using Newtonsoft.Json;

namespace Net.Utilities.Nlog.Entities.HtmlElements;

public enum HtmlHeaderLevelEnum
{
    Header1 = 1,
    Header2 = 2,
    Header3 = 3,
    Header4 = 4,
    Header5 = 5,
    Header6 = 6,
    Header7 = 7
}

internal sealed record HtmlHeader : AbstractHtmlElement
{
    [JsonProperty]
    private readonly string _id = Guid.NewGuid().ToString("N");

    public string Title { get; }

    public HtmlHeaderLevelEnum HtmlHeaderLevelEnum { get; }

    public LogLevelEnum LogLevelEnum { get; internal set; }

    public HtmlLog? HtmlLog { get; }

    internal HtmlHeader(string title, HtmlHeaderLevelEnum htmlHeaderLevelEnum, LogLevelEnum logLevelEnum, HtmlLog? htmlLog = null)
    {
        Title = title;
        HtmlHeaderLevelEnum = htmlHeaderLevelEnum;
        LogLevelEnum = logLevelEnum;
        HtmlLog = htmlLog;
    }

    internal override void WriteToHtml(TextWriter writer)
    {
        var mark = HtmlHeaderLevelEnum.ToMark();
        var contentHtml = HtmlLog?.ToContentHtml() ?? string.Empty;
        var textSize = HtmlHeaderLevelEnum.ToClassOfTextSize();

        writer.WriteLine($"""
                          <div id="{_id}" class="menu-link-section">
                              <{mark} class="{textSize} font-bold text-{LogLevelEnum.ToColor()}-600">{((HtmlBlank)Title).ToContentHtml()}</{mark}>
                              <hr class="border-gray-500 border-1 -mx-[0.1rem] mb-2">
                              {contentHtml}
                          </div>
                          """);
    }

    public string ToNavigationHtml(bool isHasChildren) => isHasChildren
        ? $"""
           <a class="menu-link-color flex items-center menu-link text-{LogLevelEnum.ToColor()}-600" href="#{_id}">
               <span @click="toggleMenuVisibility($event, $el)">
                   <svg class="menu-link-svg" data-menu-svg-rotate="true" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                       <path d="M9 5l7 7-7 7"/>
                   </svg>
               </span>
               <span class="menu-link-text">{((HtmlBlank)Title).ToContentHtml()}</span>
           </a>
           """
        : $"""<a class="menu-link-color block menu-link menu-link-text text-{LogLevelEnum.ToColor()}-600" href="#{_id}">{((HtmlBlank)Title).ToContentHtml()}</a>""";

    internal override string ToViewString() => Title;
}
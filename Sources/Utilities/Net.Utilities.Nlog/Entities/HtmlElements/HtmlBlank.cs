using System.Net;

namespace Net.Utilities.Nlog.Entities.HtmlElements;

internal sealed record HtmlBlank : AbstractHtmlElement
{
    internal static readonly HtmlBlank Empty = new(string.Empty);

    public string Content { get; }

    public HtmlBlank(string? content)
    {
        Content = content ?? "null";
    }

    internal override void WriteToHtml(TextWriter writer)
    {
        writer.Write(WebUtility.HtmlEncode(Content).Replace(Environment.NewLine, "<br>"));
    }

    internal override string ToViewString() => Content;

    public static explicit operator HtmlBlank(string str) => new(str);
}
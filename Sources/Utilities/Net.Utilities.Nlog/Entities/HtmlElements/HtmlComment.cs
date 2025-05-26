namespace Net.Utilities.Nlog.Entities.HtmlElements;

public sealed record HtmlComment(string Comment) : AbstractHtmlElement
{
    internal override void WriteToHtml(TextWriter writer)
    {
        writer.WriteLine($"""
                          <div class="w-full bg-gray-50 border border-gray-300 rounded shadow">
                              <blockquote class="p-2 space-y-1 text-sm text-gray-900 border-l-4 border-y-0 border-r-0 border-red-500 rounded shadow">
                                  <p><span class="font-bold">{((HtmlBlank)Comment).ToContentHtml()}</span></p>
                              </blockquote>
                          </div>
                          """);
    }

    internal override string ToViewString() => Comment;
}
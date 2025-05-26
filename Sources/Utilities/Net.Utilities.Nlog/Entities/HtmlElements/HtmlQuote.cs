namespace Net.Utilities.Nlog.Entities.HtmlElements;

public sealed record HtmlQuote : HtmlBullet
{
    public HtmlQuote(object item) : base(item)
    {
    }

    internal override void WriteToHtml(TextWriter writer)
    {
        writer.WriteLine("""
                         <div class="w-full bg-gray-50 border border-gray-300 rounded shadow">
                             <blockquote class="p-2 space-y-1 text-sm text-gray-900 border-l-4 border-y-0 border-r-0 border-red-500 rounded shadow break-all">
                         """);

        foreach (var kvp in Item)
        {
            writer.WriteLine($"""<p><span class="font-bold">{((HtmlBlank)kvp.Key).ToContentHtml()}: </span>{kvp.Value.ToContentHtml()}</p>""");
        }

        writer.WriteLine("""
                             </blockquote>
                         </div>
                         """);
    }
}
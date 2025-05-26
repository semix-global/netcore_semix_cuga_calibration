using Newtonsoft.Json;

namespace Net.Utilities.Nlog.Entities.HtmlElements;

public abstract record AbstractHtmlElement : IFormattable
{
    internal abstract void WriteToHtml(TextWriter writer);

    internal abstract string ToViewString();

    internal virtual string ToContentHtml()
    {
        using var writer = new StringWriter();
        WriteToHtml(writer);

        return writer.ToString();
    }

    public virtual string ToString(string? format, IFormatProvider? formatProvider) => JsonConvert.SerializeObject(this);
}
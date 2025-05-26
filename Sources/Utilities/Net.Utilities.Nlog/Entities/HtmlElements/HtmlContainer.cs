namespace Net.Utilities.Nlog.Entities.HtmlElements;

public sealed record HtmlContainer(AbstractHtmlElement[] Elements) : AbstractHtmlElement
{
    internal override void WriteToHtml(TextWriter writer)
    {
        foreach (var htmlElement in Elements)
        {
            htmlElement.WriteToHtml(writer);
        }
    }

    internal override string ToViewString() => string.Join(
        Environment.NewLine,
        from item in Elements
        select item.ToViewString()
    );
}
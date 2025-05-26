using Humanizer;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Net.Utilities.Nlog.Entities.HtmlElements;

public record HtmlBullet : AbstractHtmlElement
{
    public Dictionary<string, AbstractHtmlElement> Item { get; }

    public HtmlBullet(object item)
    {
        Item = item.GetType().GetProperties().ToDictionary(
            propertyInfo => (propertyInfo.GetCustomAttribute<DescriptionAttribute>()?.Description ?? propertyInfo.Name).Humanize(LetterCasing.Title),
            propertyInfo =>
            {
                var value = propertyInfo.GetValue(item);

                return value switch
                {
                    AbstractHtmlElement abstractElement => abstractElement,
                    _ => new HtmlBlank(value switch
                    {
                        string str => str,
                        IFormattable formatter => formatter.ToString(null, CultureInfo.CurrentCulture),
                        _ => JsonConvert.SerializeObject(value)
                    })
                };
            });
    }

    internal override void WriteToHtml(TextWriter writer)
    {
        writer.WriteLine("""
                         <div class="w-full">
                             <ul class="marker:text-black list-disc pl-5 space-y-1 text-sm text-gray-900 break-all">
                         """);

        foreach (var kvp in Item)
        {
            writer.WriteLine($"""<li><span class="font-bold">{((HtmlBlank)kvp.Key).ToContentHtml()}: </span>{kvp.Value.ToContentHtml()}</li>""");
        }

        writer.WriteLine("""
                             </ul>
                         </div>
                         """);
    }

    internal override string ToViewString() => string.Join("; ",
        from kvp in Item
        select $"{kvp.Key}: {kvp.Value.ToViewString()}"
    );
}
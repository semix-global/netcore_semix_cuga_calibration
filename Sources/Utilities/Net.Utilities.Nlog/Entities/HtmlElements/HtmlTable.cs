using Humanizer;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Net.Utilities.Nlog.Entities.HtmlElements;

public sealed record HtmlTable : AbstractHtmlElement
{
    public List<Dictionary<string, AbstractHtmlElement>> ItemList { get; }

    public HtmlTable(List<object> itemList)
    {
        ItemList =
        [
            .. (from item in itemList
                select item.GetType().GetProperties().ToDictionary(
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
                    })
            )
        ];
    }

    internal override void WriteToHtml(TextWriter writer)
    {
        var titleList = (
            from item in ItemList
            from key in item.Keys
            select key
        ).Distinct().ToList();

        var rowList = (
            from item in ItemList
            select (
                from title in titleList
                select item.TryGetValue(title, out var value) ? value : new HtmlBlank(string.Empty)
            ).ToList()
        ).ToList();

        var titleHtml = string.Join(
            Environment.NewLine,
            from title in titleList
            select $"""<th class="table-thread-th">{((HtmlBlank)title).ToContentHtml()}</th>"""
        );

        writer.WriteLine($"""
                          <div class="w-full bg-gray-50 border border-gray-300 rounded shadow">
                              <div class="overflow-auto">
                                  <table class="w-full">
                                      <thead class="table-thread">
                                      <tr class="table-thread-tr">
                                          {titleHtml}
                                      </tr>
                                      </thead>
                                      <tbody class="table-tbody">
                          """);

        foreach (var row in rowList)
        {
            writer.WriteLine("""<tr class="table-tbody-tr">""");

            foreach (var cell in row)
            {
                writer.WriteLine($"""<td class="table-tbody-td">{cell.ToContentHtml()}</td>""");
            }

            writer.WriteLine("</tr>");
        }

        writer.WriteLine($"""
                                      </tbody>
                                  </table>
                              </div>
                          </div>
                          """);
    }

    internal override string ToViewString() => string.Join(
        Environment.NewLine,
        from item in ItemList
        select $"{string.Join(
            "; ",
            from kvp in item
            select $"{kvp.Key}: {kvp.Value.ToViewString()}"
        )}"
    );
}
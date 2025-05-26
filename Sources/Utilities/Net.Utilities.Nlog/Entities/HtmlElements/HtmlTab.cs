namespace Net.Utilities.Nlog.Entities.HtmlElements;

public sealed record HtmlTab : HtmlBullet
{
    public HtmlTab(object item) : base(item)
    {
    }

    internal override void WriteToHtml(TextWriter writer)
    {
        var tabHtml = string.Join(
            Environment.NewLine,
            from kvp in Item.Select((t, i) => (Index: i, t.Key))
            let index = kvp.Index
            let title = kvp.Key
            select $"""
                    <button :class="activeTab === {index} ? 'border-b-2 border-blue-500 text-blue-500' : 'text-gray-500'"
                            @click="activeTab = {index}"
                            class="pb-1 px-2 min-w-[200px] text-base font-semibold transition-colors cursor-pointer">
                        {((HtmlBlank)title).ToContentHtml()}
                    </button>
                    """
        );

        writer.WriteLine($$"""
                           <div class="w-full bg-gray-50 border border-gray-300 rounded shadow" x-data="{ activeTab: 0 }">
                               <div class="my-2">
                                   <div class="flex overflow-x-auto px-2 pt-2 space-x-2 border-b border-gray-300">
                                       {{tabHtml}}
                                   </div>
                                   <div class="mt-2 mx-2">
                           """);

        foreach (var (index, element) in Item.Select((t, i) => (Index: i, t.Value)))
        {
            writer.WriteLine($"""<div x-show="activeTab === {index}">{element.ToContentHtml()}</div>""");
        }

        writer.WriteLine("""
                                 </div>
                             </div>
                         </div>
                         """);
    }
}
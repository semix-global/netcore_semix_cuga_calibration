namespace Net.Utilities.Nlog.Entities.HtmlElements;

public sealed record HtmlExpand(
    AbstractHtmlElement Element,
    string Title) : AbstractHtmlElement
{
    internal override void WriteToHtml(TextWriter writer)
    {
        writer.WriteLine($$"""
                           <div class="w-full bg-gray-50 border border-gray-300 rounded-xl shadow" x-data="{toggle2DData: false}">
                               <div :class="{ 'rounded-b-xl': !toggle2DData }" @click="$event.preventDefault(); toggle2DData= !toggle2DData;" class="flex items-center cursor-pointer select-none bg-gray-200 rounded-t-xl">
                                   <div class="shrink-0 m-2 text-gray-500">
                                       <svg class="shrink-0 size-5" fill="currentColor" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="0.5" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                           <path d="M12 11q3.75 0 6.375-1.175T21 7t-2.625-2.825T12 3T5.625 4.175T3 7t2.625 2.825T12 11m0 2.5q1.025 0 2.563-.213t2.962-.687t2.45-1.237T21 9.5V12q0 1.1-1.025 1.863t-2.45 1.237t-2.962.688T12 16t-2.562-.213t-2.963-.687t-2.45-1.237T3 12V9.5q0 1.1 1.025 1.863t2.45 1.237t2.963.688T12 13.5m0 5q1.025 0 2.563-.213t2.962-.687t2.45-1.237T21 14.5V17q0 1.1-1.025 1.863t-2.45 1.237t-2.962.688T12 21t-2.562-.213t-2.963-.687t-2.45-1.237T3 17v-2.5q0 1.1 1.025 1.863t2.45 1.237t2.963.688T12 18.5"/>
                                       </svg>
                                   </div>
                                   <h3 class="flex-1 text-base font-medium">Expand: {{((HtmlBlank)Title).ToContentHtml()}}</h3>
                                   <svg :class="{ 'rotate-180': !toggle2DData }" class="size-4 m-2" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="4" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                       <path d="M19 9l-7 7-7-7"/>
                                   </svg>
                               </div>
                               <div class="p-2" x-show="toggle2DData">
                                  {{Element.ToContentHtml()}}
                               </div>
                           </div>
                           """);
    }

    internal override string ToViewString() => $"{Title}: {Element.ToViewString()}";
}
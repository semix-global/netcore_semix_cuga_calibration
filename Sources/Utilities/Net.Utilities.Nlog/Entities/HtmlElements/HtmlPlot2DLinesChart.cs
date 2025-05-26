using Humanizer;
using MiniExcelLibs;
using Net.Utilities.Helper.File;
using Net.Utilities.Models;
using Newtonsoft.Json;
using System.Text;

namespace Net.Utilities.Nlog.Entities.HtmlElements;

public sealed record HtmlPlot2DLinesChart : AbstractHtmlElement
{
    [JsonProperty]
    private readonly string _id = Guid.NewGuid().ToString("N");

    public string Title { get; } = string.Empty;

    public (string Name, Point[] Points, double Position)[] Plots { get; } = [];

    public HtmlPlot2DLinesChart((string Name, Point[] Points, double Position)[] plots, string title)
    {
        Title = title;
        Plots = plots;
    }

    public HtmlPlot2DLinesChart((string Name, Point[] Points)[] plots, string title)
    {
        Plots = [.. plots.Select(t => (t.Name, t.Points, 0d))];
        Title = title;
    }

    internal override void WriteToHtml(TextWriter writer)
    {
        var bytes = GenerateExcelBytes();
        var htmlDownload = new HtmlDownload(bytes, $"{FileHelper.RemoveInvalidFileName(Title.Humanize(LetterCasing.AllCaps))}.xlsx");

        var maxValue = Plots.Max(t => t.Position);
        var minValue = Plots.Min(t => t.Position);
        var isRandomColor = maxValue == 0d && minValue == 0d;
        var traces = string.Join(Environment.NewLine,
            from tuple in Plots.Select((t, i) => (Index: i, Plot: t))
            let index = tuple.Index
            let position = tuple.Plot.Position
            let plot = tuple.Plot
            select $$"""
                     const trace{{_id}}{{index}} = {
                         x: [{{string.Join(", ", plot.Points.Select(x => x.X.ToString("f20")))}}],
                         y: [{{string.Join(", ", plot.Points.Select(x => x.Y.ToString("f20")))}}],
                         name: "{{((HtmlBlank)plot.Name).ToContentHtml()}}",
                         mode: "lines+markers",
                         {{(isRandomColor ? string.Empty : $$"""line: {color: Heat2DColorMap({{maxValue}}, {{minValue}}, {{position}})},""")}}
                         type: "scattergl"
                     };
                     """
        );

        var data = string.Join(", ", Plots.Select((_, i) => $"trace{_id}{i}"));

        writer.WriteLine($$"""
                           <div class="w-full bg-gray-50 border border-gray-300 rounded-xl shadow" x-data="{toggle2DPlot: false}">
                               <div :class="{ 'rounded-b-xl': !toggle2DPlot }" @click="$event.preventDefault(); toggle2DPlot= !toggle2DPlot;toggle2DPlot{{_id}}(toggle2DPlot);" class="flex items-center cursor-pointer select-none bg-gray-200 rounded-t-xl">
                                   <div class="shrink-0 mx-2 my-1 text-gray-500">
                                       <svg class="shrink-0 size-5" fill="currentColor" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="0.5" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                           <path d="M3.5 20.73L2.77 20l6.807-6.808l4 4l6.985-8l.707.67l-7.653 8.83l-4.039-4.038zm0-6L2.77 14l6.807-6.808l4 4l6.985-8l.707.67l-7.653 8.83l-4.039-4.038z"/>
                                       </svg>
                                   </div>
                                   <h3 class="flex-1 text-base font-medium">Plot2D: {{((HtmlBlank)Title).ToContentHtml()}}</h3>
                           """);

        htmlDownload.WriteToHtml(writer);

        writer.WriteLine($$"""
                                   <svg :class="{ 'rotate-180': !toggle2DPlot }" class="size-4 mx-2 my-1" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="4" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                       <path d="M19 9l-7 7-7-7"/>
                                   </svg>
                               </div>
                               <div class="min-w-[1200px] p-2" style="height: 600px" x-show="toggle2DPlot">
                                   <div class="m-auto p-1 bg-white" id="{{_id}}" style="height: 100%"></div>
                               </div>
                               <script>
                                   function toggle2DPlot{{_id}}(toggle) {
                                       if (toggle) {
                                           setTimeout(function () {
                                              {{traces}}

                                              // 设置布局
                                              let layout = {
                                                  title: "{{((HtmlBlank)Title).ToContentHtml()}}",
                                                  showlegend: true,
                                                  legend: {
                                                      orientation: "h"
                                                  },
                                                  xaxis: {
                                                      showgrid: true,
                                                      zeroline: true,
                                                      showticklabels: true,
                                                      tickformat: "f20"
                                                  },
                                                  yaxis: {
                                                      showgrid: true,
                                                      zeroline: true,
                                                      showticklabels: true,
                                                      tickformat: "f20"
                                                  }
                                              };

                                              Plotly.newPlot("{{_id}}", [{{data}}], layout,
                                               {
                                                   responsive: true,
                                                   displaylogo: false
                                               });
                                               Plotly.Plots.resize("{{_id}}");
                                           }, 100);
                                       } else {
                                           Plotly.purge("{{_id}}"); // 释放图表资源
                                       }
                                   }
                               </script>
                           </div>
                           """);
    }

    internal override string ToViewString() => $"{nameof(HtmlPlot2DLinesChart).Humanize(LetterCasing.Title)}: {Title}[{string.Join("; ",
        from plot in Plots
        select plot.Name
    )}]";

    private byte[] GenerateExcelBytes()
    {
        try
        {
            var sheets = new Dictionary<string, object>();

            var values = new List<Dictionary<string, object?>>();

            var maxLength = Plots.Max(t => t.Points.Length);
            for (var i = 0; i < maxLength; i++)
            {
                var dic = new Dictionary<string, object?>();
                foreach (var (name, points, _) in Plots)
                {
                    var x = string.IsNullOrWhiteSpace(name) ? nameof(Point.X) : $"{name} - {nameof(Point.X)}";
                    var y = string.IsNullOrWhiteSpace(name) ? nameof(Point.Y) : $"{name} - {nameof(Point.Y)}";

                    dic[x] = i < points.Length ? points[i].X : null;
                    dic[y] = i < points.Length ? points[i].Y : null;
                }

                values.Add(dic);
            }

            sheets.Add("ALL", values);

            foreach (var (name, points, _) in Plots)
            {
                sheets[name] = points.Select(t => new
                {
                    t.X,
                    t.Y
                }).ToList();
            }

            using var memoryStream = new MemoryStream();
            memoryStream.SaveAs(sheets);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            return Encoding.UTF8.GetBytes(ex.ToString());
        }
    }
}
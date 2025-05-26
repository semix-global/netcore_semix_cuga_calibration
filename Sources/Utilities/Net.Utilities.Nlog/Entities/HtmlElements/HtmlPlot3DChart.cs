using CommunityToolkit.Diagnostics;
using Humanizer;
using MiniExcelLibs;
using Net.Utilities.Helper.File;
using Net.Utilities.Models;
using Newtonsoft.Json;
using System.Text;

namespace Net.Utilities.Nlog.Entities.HtmlElements;

public enum HtmlPlot3DType
{
    Bar3D,
    Surface
}

public sealed record HtmlPlot3DChart(
    Point3D[] Point3Ds,
    string Title,
    HtmlPlot3DType HtmlPlot3DType) : AbstractHtmlElement
{
    [JsonProperty]
    private readonly string _id = Guid.NewGuid().ToString("N");

    internal override void WriteToHtml(TextWriter writer)
    {
        var bytes = GenerateExcelBytes();
        var htmlDownload = new HtmlDownload(bytes, $"{FileHelper.RemoveInvalidFileName(Title.Humanize(LetterCasing.AllCaps))}.xlsx");

        var data = $"const data = [{string.Join(",",
            from tuple in Point3Ds
            let x = tuple.X
            let y = tuple.Y
            let z = tuple.Z
            select $"[{x}, {y}, {z}]"
        )}]";

        var minZ = Point3Ds.Min(t => t.Z);
        var maxZ = Point3Ds.Max(t => t.Z);
        var minX = Point3Ds.Min(t => t.X) - 1;
        var maxX = Point3Ds.Max(t => t.X) + 1;
        var minY = Point3Ds.Min(t => t.Y) - 1;
        var maxY = Point3Ds.Max(t => t.Y) + 1;

        var series = HtmlPlot3DType switch
        {
            HtmlPlot3DType.Bar3D => $$"""
                                      series: [
                                          {
                                              name: "{{((HtmlBlank)Title).ToContentHtml()}}",
                                              type: "bar3D",
                                              data: data,
                                              label: {
                                                  formatter: function (value) {
                                                      return "";
                                                  }
                                              },
                                              emphasis: {
                                                  itemStyle: {
                                                      color: "blue"
                                                  }
                                              }
                                          }
                                      ]
                                      """,
            HtmlPlot3DType.Surface => $$"""
                                        series: [
                                            {
                                                name: "{{((HtmlBlank)Title).ToContentHtml()}}",
                                                type: "surface",
                                                data: data,
                                            }
                                        ]
                                        """,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(HtmlPlot3DType), HtmlPlot3DType, null)
        };

        writer.WriteLine($$"""
                           <div class="w-full bg-gray-50 border border-gray-300 rounded-xl shadow" x-data="{toggle3DEChart: false}">
                               <div :class="{ 'rounded-b-xl': !toggle3DEChart }" @click="$event.preventDefault(); toggle3DEChart= !toggle3DEChart;toggle3DEChart{{_id}}(toggle3DEChart);" class="flex items-center cursor-pointer select-none bg-gray-200 rounded-t-xl">
                                   <div class="shrink-0 mx-2 my-1 text-gray-500">
                                       <svg class="shrink-0 size-5" fill="currentColor" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="0.5" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                           <path d="M12 21q-1.868 0-3.51-.709t-2.857-1.923t-1.924-2.858T3 12h1q0 3.125 2.091 5.402t5.205 2.564L9.731 18.4l.708-.708l3.127 3.127q-.38.097-.78.139T12 21m.808-6.308V9.308h2.769q.425 0 .712.287t.288.713v3.384q0 .425-.288.713t-.712.287zm-5.385 0v-.884h2.885v-1.424H8.423v-.769h1.885v-1.423H7.423v-.884H10.5q.294 0 .493.199t.2.493v4q0 .294-.2.493t-.493.2zm6.27-.884h1.769q.115 0 .173-.058t.057-.173v-3.154q0-.115-.058-.173q-.057-.058-.173-.058h-1.769zM20 12q0-3.125-2.091-5.402t-5.205-2.564L14.269 5.6l-.708.708l-3.127-3.127q.38-.096.78-.139T12 3q1.868 0 3.51.709t2.858 1.924T20.29 8.49T21 12z"/>
                                       </svg>
                                   </div>
                                   <h3 class="flex-1 text-base font-medium">Plot3D: {{((HtmlBlank)Title).ToContentHtml()}}</h3>
                           """);

        htmlDownload.WriteToHtml(writer);

        writer.WriteLine($$"""
                                   <svg :class="{ 'rotate-180': !toggle3DEChart }" class="size-4 mx-2 my-1" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="4" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                       <path d="M19 9l-7 7-7-7"/>
                                   </svg>
                               </div>
                               <div class="min-w-[1200px] p-2" style="height: 600px" x-show="toggle3DEChart">
                                   <div class="m-auto p-1 bg-white" id="{{_id}}" style="height: 100%"></div>
                               </div>
                               <script>
                                   let echarts{{_id}} = null;

                                   function toggle3DEChart{{_id}}(toggle) {
                                       if (toggle) {
                                           setTimeout(function () {
                                               {{data}}
                                               echarts{{_id}} = echarts.init(document.getElementById("{{_id}}"));
                                               echarts{{_id}}.setOption({
                                                   title: {
                                                       text: "{{((HtmlBlank)Title).ToContentHtml()}}",
                                                       left: "center"
                                                   },
                                                   tooltip: {
                                                       formatter: function (params) {
                                                           const x = params.data[0].toFixed(3);
                                                           const y = params.data[1].toFixed(3);
                                                           const z = params.data[2].toFixed(3);
                                                           const color = params.color;
                                                           return `<span style="display:block; width:10px; height:10px; border-radius:50%; background-color:${color}; margin-right:5px;"></span>
                                                                   - x: ${x}<br/>
                                                                   - y: ${y}<br/>
                                                                   - z: ${z}`;
                                                       }
                                                   },
                                                   visualMap: {
                                                       min: {{minZ}},
                                                       max: {{maxZ}},
                                                       inRange: {
                                                           color: colorsMap
                                                       }
                                                   },
                                                   xAxis3D: {
                                                       type: "value",
                                                       axisLabel: {
                                                           formatter: function (value) {
                                                               return value.toFixed(3);
                                                           }
                                                       },
                                                       min: {{minX}},
                                                       max: {{maxX}},
                                                   },
                                                   yAxis3D: {
                                                       type: "value",
                                                       axisLabel: {
                                                           formatter: function (value) {
                                                               return value.toFixed(3);
                                                           }
                                                       },
                                                       min: {{minY}},
                                                       max: {{maxY}},
                                                   },
                                                   zAxis3D: {
                                                       type: "value",
                                                       axisLabel: {
                                                           formatter: function (value) {
                                                               return value.toFixed(3);
                                                           }
                                                       },
                                                       min: {{minZ}},
                                                       max: {{maxZ}},
                                                   },
                                                   grid3D: {},
                                                   {{series}}
                                               });
                                               echarts{{_id}}.resize();
                                           }, 100);
                                       } else {
                                           if (echarts{{_id}} != null) {
                                               echarts{{_id}}.dispose();
                                               echarts{{_id}} = null;
                                           }
                                       }
                                   }
                               </script>
                           </div>
                           """);
    }

    internal override string ToViewString() => $"{nameof(HtmlPlot3DChart).Humanize(LetterCasing.Title)}: {Title}";

    private byte[] GenerateExcelBytes()
    {
        try
        {
            var values = (from point3D in Point3Ds
                          select new Dictionary<string, object>
                {
                    { nameof(point3D.X), point3D.X },
                    { nameof(point3D.Y), point3D.Y },
                    { nameof(point3D.Z), point3D.Z }
                }).ToList();

            using var memoryStream = new MemoryStream();
            memoryStream.SaveAs(values);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            return Encoding.UTF8.GetBytes(ex.ToString());
        }
    }
}
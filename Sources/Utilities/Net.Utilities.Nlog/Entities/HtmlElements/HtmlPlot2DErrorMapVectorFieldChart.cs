using Humanizer;
using MiniExcelLibs;
using Net.Utilities.Algorithm.MathNet.Helper;
using Net.Utilities.Helper.File;
using Net.Utilities.Models;
using Newtonsoft.Json;
using System.Text;

namespace Net.Utilities.Nlog.Entities.HtmlElements;

public sealed record HtmlPlot2DErrorMapVectorFieldChart(
    Point[,] IdealMatrix,
    Point[,] RealMatrix,
    Point[,] ErrorMatrix,
    string Title) : AbstractHtmlElement
{
    [JsonProperty]
    private readonly string _id = Guid.NewGuid().ToString("N");

    internal override void WriteToHtml(TextWriter writer)
    {
        var bytes = GenerateExcelBytes();
        var htmlDownload = new HtmlDownload(bytes, $"{FileHelper.RemoveInvalidFileName(Title.Humanize(LetterCasing.AllCaps))}.xlsx");

        var (rowCount, columnCount) = MatrixHelper.GetRowCountColCount(IdealMatrix);
        if ((rowCount, columnCount) != MatrixHelper.GetRowCountColCount(RealMatrix) || (rowCount, columnCount) != MatrixHelper.GetRowCountColCount(ErrorMatrix))
        {
            writer.WriteLine("The matrix dimensions are inconsistent");

            return;
        }

        var rowInterval = Math.Abs(IdealMatrix[1, 0].Y - IdealMatrix[0, 0].Y);
        var colInterval = Math.Abs(IdealMatrix[0, 1].X - IdealMatrix[0, 0].X);

        var idealArray = MatrixHelper.ToArrayByRow(IdealMatrix);
        var realArray = MatrixHelper.ToArrayByRow(RealMatrix);
        var errorArray = MatrixHelper.ToArrayByRow(ErrorMatrix);

        writer.WriteLine($$"""
                           <div class="w-full bg-gray-50 border border-gray-300 rounded-xl shadow" x-data="{toggle2DVectorField: false}">
                               <div :class="{ 'rounded-b-xl': !toggle2DVectorField }" @click="$event.preventDefault(); toggle2DVectorField= !toggle2DVectorField;toggle2DVectorField{{_id}}(toggle2DVectorField);" class="flex items-center cursor-pointer select-none bg-gray-200 rounded-t-xl">
                                   <div class="shrink-0 mx-2 my-1 text-gray-500">
                                       <svg class="shrink-0 size-5" fill="currentColor" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="0.5" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                           <path d="M3.5 20.73L2.77 20l6.807-6.808l4 4l6.985-8l.707.67l-7.653 8.83l-4.039-4.038zm0-6L2.77 14l6.807-6.808l4 4l6.985-8l.707.67l-7.653 8.83l-4.039-4.038z"/>
                                       </svg>
                                   </div>
                                   <h3 class="flex-1 text-base font-medium">Vector Field: {{((HtmlBlank)Title).ToContentHtml()}}</h3>
                           """);

        htmlDownload.WriteToHtml(writer);

        writer.WriteLine($$"""
                                   <svg :class="{ 'rotate-180': !toggle2DVectorField }" class="size-4 mx-2 my-1" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="4" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                       <path d="M19 9l-7 7-7-7"/>
                                   </svg>
                               </div>
                               <div class="min-w-[1200px] p-2" style="height: 600px" x-show="toggle2DVectorField">
                                   <div class="m-auto p-1 bg-white" id="{{_id}}" style="height: 100%"></div>
                               </div>
                               <script>
                                   function toggle2DVectorField{{_id}}(toggle) {
                                       if (toggle) {
                                           setTimeout(function () {
                                               let idealX = [{{string.Join(", ", idealArray.Select(t => t.X))}}];
                                               let idealY = [{{string.Join(", ", idealArray.Select(t => t.Y))}}];
                                               let realX = [{{string.Join(", ", realArray.Select(t => t.X))}}];
                                               let realY = [{{string.Join(", ", realArray.Select(t => t.Y))}}];
                                               let errorX = [{{string.Join(", ", errorArray.Select(t => t.X))}}];
                                               let errorY = [{{string.Join(", ", errorArray.Select(t => t.Y))}}];
                                               const xInterval = {{colInterval}};
                                               const yInterval = {{rowInterval}};
                                               const xCount = {{columnCount}};
                                               const yCount = {{rowCount}};

                                               const chartWidth = document.getElementById("{{_id}}").offsetWidth;
                                               const chartHeight = document.getElementById("{{_id}}").offsetHeight;
                                               const xPixelRatio = chartWidth / (xInterval * xCount);
                                               const yPixelRatio = chartHeight / (yInterval * yCount);

                                               // pixel/x
                                               const pixelRatio = Math.min(xPixelRatio, yPixelRatio);

                                               // 计算每个点的矢量长度
                                               let lengths = errorX.map((errorXValue, i) => Math.sqrt(errorXValue * errorXValue + errorY[i] * errorY[i]));

                                               // 找到最小长度和最大长度
                                               let minLength = Math.min(...lengths);
                                               let maxLength = Math.max(...lengths);

                                               let colors = lengths.map(length => {
                                                   let normalizedLength = (length - minLength) / (maxLength - minLength);
                                                   let colorIndex = Math.floor(normalizedLength * (colorsMap.length - 1));
                                                   return colorsMap[colorIndex];
                                               });

                                               const maxPixelLength = 25; // pixel
                                               const scalingFactor = maxPixelLength / pixelRatio;

                                               // 创建annotations，绘制带箭头的矢量
                                               let annotations = [];
                                               for (let i = 0; i < idealX.length; i++) {
                                                   let startX = idealX[i];
                                                   let startY = idealY[i];
                                                   let endX = idealX[i] + Math.cos(Math.atan2(errorY[i], errorX[i])) * ((lengths[i] - minLength) / (maxLength - minLength)) * scalingFactor;
                                                   let endY = idealY[i] + Math.sin(Math.atan2(errorY[i], errorX[i])) * ((lengths[i] - minLength) / (maxLength - minLength)) * scalingFactor;
                                                   let arrowColor = colors[i];
                                                   annotations.push({
                                                       x: endX,// 箭头指向的位置
                                                       y: endY,// 箭头指向的位置
                                                       xref: "x",
                                                       yref: "y",
                                                       ax: startX,
                                                       ay: startY,
                                                       axref: "x",
                                                       ayref: "y",
                                                       arrowcolor: arrowColor,
                                                       showarrow: true
                                                   });
                                               }

                                               // 创建scatter数据，添加点显示
                                               let scatterData = {
                                                   name: "ideal",
                                                   x: idealX,
                                                   y: idealY,
                                                   mode: "markers",
                                                   marker: {
                                                       color: colors, // 使用颜色映射
                                                       size: 5 // 点的大小
                                                   },
                                                   hovertext: idealX.map((_, i) => {
                                                       return `x: ${idealX[i].toFixed(3)}<br>y: ${idealY[i].toFixed(3)}<br>u: ${errorX[i].toFixed(3)}<br>v: ${errorY[i].toFixed(3)}`;
                                                   }),
                                                   hoverinfo: "text"
                                               };

                                               let textData = {
                                                   name: "error",
                                                   x: idealX,
                                                   y: idealY.map(yVal => yVal - 10 / pixelRatio),
                                                   mode: "text",
                                                   text: idealX.map((_, i) => {
                                                       return `(${errorX[i].toFixed(3)},${errorY[i].toFixed(3)})`; // 常规文本显示 errorX 和 errorY 值
                                                   }),
                                                   textposition: "bottom center",
                                                   textfont: {
                                                       color: colors // 使用 colors 数组为每个文本点设置不同的颜色
                                                   },
                                                   visible: "legendonly"
                                               };

                                               // 设置布局
                                               let layout = {
                                                   title: "{{((HtmlBlank)Title).ToContentHtml()}}",
                                                   showlegend: true,
                                                   legend: {
                                                       orientation: "h"
                                                   },
                                                   annotations: annotations,
                                                   xaxis: {
                                                       showgrid: true,
                                                       zeroline: false,
                                                       showticklabels: true,
                                                       tickformat: "f"
                                                   },
                                                   yaxis: {
                                                       showgrid: true,
                                                       zeroline: false,
                                                       showticklabels: true,
                                                       tickformat: "f"
                                                   }
                                               };

                                               // 使用 Plotly 绘制图表
                                               Plotly.newPlot("{{_id}}", [scatterData, textData], layout, {
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

    internal override string ToViewString() => $"{nameof(HtmlPlot2DErrorMapVectorFieldChart).Humanize(LetterCasing.Title)}: {Title}";

    private byte[] GenerateExcelBytes()
    {
        try
        {
            var sheets = new Dictionary<string, object>();

            Add(IdealMatrix, nameof(IdealMatrix));
            Add(RealMatrix, nameof(RealMatrix));
            Add(ErrorMatrix, nameof(ErrorMatrix));

            using var memoryStream = new MemoryStream();
            memoryStream.SaveAs(sheets);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return memoryStream.ToArray();

            void Add(Point[,] matrix, string sheetName)
            {
                var temp = new List<Dictionary<string, object>>();
                for (var row = 0; row < matrix.GetLength(0); row++)
                {
                    var dictionary = new Dictionary<string, object> { { "Index", $"Row: {row + 1}" } };
                    for (var column = 0; column < matrix.GetLength(1); column++)
                    {
                        dictionary.Add($"Column: {column + 1}", $"({matrix[row, column].X}, {matrix[row, column].Y})");
                    }

                    temp.Add(dictionary);
                }

                sheets.Add(sheetName, temp);
            }
        }
        catch (Exception ex)
        {
            return Encoding.UTF8.GetBytes(ex.ToString());
        }
    }
}
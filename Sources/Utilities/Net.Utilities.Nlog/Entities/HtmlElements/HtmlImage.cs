using CommunityToolkit.Diagnostics;
using Net.Utilities.Enums;
using Net.Utilities.Helper.Enum;
using Net.Utilities.Helper.File;
using Net.Utilities.Models;
using Newtonsoft.Json;

namespace Net.Utilities.Nlog.Entities.HtmlElements;

public record HtmlImageOverlay
{
    public SharpeTypeEnum SharpeTypeEnum { get; init; }

    public Point Point { get; init; }

    public Size Size { get; init; }
}

public sealed record HtmlImageCrossOverlay : HtmlImageOverlay
{
    public bool IsCenterCross { get; init; }

    public HtmlImageCrossOverlay(bool isCenterCross)
    {
        IsCenterCross = isCenterCross;
        SharpeTypeEnum = SharpeTypeEnum.Cross;
    }

    public HtmlImageCrossOverlay(bool isCenterCross, Size size)
    {
        IsCenterCross = isCenterCross;
        Size = size;
        SharpeTypeEnum = SharpeTypeEnum.Cross;
    }

    public HtmlImageCrossOverlay(Point point)
    {
        Point = point;
        SharpeTypeEnum = SharpeTypeEnum.Cross;
    }

    public HtmlImageCrossOverlay(Point point, Size size)
    {
        Point = point;
        Size = size;
        SharpeTypeEnum = SharpeTypeEnum.Cross;
    }

    public Point GetPoint(Size imageSize) => IsCenterCross ? (Point)(imageSize / 2d) : Point;

    public Size GetSize(Size imageSize) => Size.IsEmpty ? new Size(imageSize.Width / 3d, imageSize.Width / 3d) : Size;
}

public sealed record HtmlImageRectangleOverlay : HtmlImageOverlay
{
    public HtmlImageRectangleOverlay(Point rectCenterPoint, Size size)
    {
        Point = rectCenterPoint;
        Size = size;
        SharpeTypeEnum = SharpeTypeEnum.Rectangle;
    }

    public HtmlImageRectangleOverlay(Rect rect) : this(rect.Point + rect.Size / 2, rect.Size)
    {
    }
}

public sealed record HtmlImage : AbstractHtmlElement
{
    private readonly bool _isBase64;
    private readonly HtmlBlank _descriptionHtmlBlank;

    [JsonIgnore]
    public string Uri { get; }

    public string Description { get; }

    public IEnumerable<HtmlImageOverlay> HtmlImageOverlays { get; }

    public HtmlImage(string uri, string? description = null, IEnumerable<HtmlImageOverlay>? htmlImageOverlays = null)
    {
        Guard.IsNotNullOrWhiteSpace(uri);

        Uri = uri;
        Description = description ?? string.Empty;
        HtmlImageOverlays = htmlImageOverlays ?? [];

        _isBase64 = uri.StartsWith("data:");
        _descriptionHtmlBlank = _isBase64
            ? new HtmlBlank(Description)
            : new HtmlBlank($"{Uri} {Description}");
    }

    internal override void WriteToHtml(TextWriter writer)
    {
        var (isSuccess, dataUri, size) = ConvertImageToDataUri();
        var descriptionContentHtml = _descriptionHtmlBlank.ToContentHtml();

        if (isSuccess == false)
            writer.WriteLine($"""
                              <div class="w-full">
                                  <figure class="max-w-5xl m-auto border shadow rounded border-gray-300">
                                      <figcaption class="m-auto my-2 text-lg text-center text-red-500 break-all">
                                          {dataUri}. {descriptionContentHtml}
                                      </figcaption>
                                  </figure>
                              </div>
                              """);

        const int maxWidth = 1000;
        var width = size.Width < maxWidth ? size.Width : maxWidth;
        var height = size.Height * width / size.Width;
        var scale = Math.Min(width / size.Width, 1);

        var sharpeList = new List<string>();
        foreach (var item in HtmlImageOverlays)
        {
            switch (item)
            {
                case { SharpeTypeEnum: SharpeTypeEnum.Cross }:
                    var htmlImageCrossOverlay = item as HtmlImageCrossOverlay;
                    var crossSize = htmlImageCrossOverlay?.GetSize(size) ?? item.Size;
                    var crossPoint = (htmlImageCrossOverlay?.GetPoint(size) ?? item.Point) - crossSize / 2;
                    sharpeList.Add($"""
                                    <div style="width: {crossSize.Width:0.######}px; height: {crossSize.Height:0.######}px; left: {crossPoint.X:0.######}px; top: {crossPoint.Y:0.######}px;"
                                         class="absolute bg-transparent">
                                        <div class="absolute w-full h-[1px] top-1/2 left-0 bg-red-500"></div>
                                        <div class="absolute h-full w-[1px] top-0 left-1/2 bg-red-500"></div>
                                    </div>
                                    """);
                    break;

                case { SharpeTypeEnum: SharpeTypeEnum.Rectangle }:
                    var htmlImageRectangleOverlay = item as HtmlImageRectangleOverlay;
                    var rectangleSize = htmlImageRectangleOverlay?.Size ?? item.Size;
                    var rectanglePoint = (htmlImageRectangleOverlay?.Point ?? item.Point) - rectangleSize / 2;
                    sharpeList.Add($"""
                                    <div style="width: {rectangleSize.Width:0.######}px; height: {rectangleSize.Height:0.######}px; left: {rectanglePoint.X:0.######}px; top: {rectanglePoint.Y:0.######}px;"
                                       class="absolute border border-green-500 bg-transparent">
                                    </div>
                                    """);
                    break;
            }
        }

        writer.WriteLine($"""
                          <div class="w-full">
                              <figure class="max-w-5xl m-auto border shadow rounded border-gray-300">
                                  <div class="m-auto my-2" style="width: {width:0.######}px; height: {height:0.######}px;" x-show="toggleAllImage">
                                      <div class="relative overflow-hidden cursor-zoom-in"
                                           style="width: {size.Width:0.######}px; height: {size.Height:0.######}px; transform-origin: top left; transform: scale({scale:0.######});"
                                           @click="openViewer($el, '{Path.GetFileNameWithoutExtension(Uri)}')">
                                          <img class="absolute"
                                               src="{dataUri}"
                                               style="width: {size.Width:0.######}px; height: {size.Height:0.######}px; left: 0; top:0; "/>
                          """);

        foreach (var sharpe in sharpeList)
        {
            writer.WriteLine(sharpe);
        }

        writer.WriteLine($"""
                                      </div>
                                  </div>
                                  <figcaption class="m-auto my-2 text-lg text-center text-red-500 break-all" x-show="!toggleAllImage">
                                      Image Is Hidde. {descriptionContentHtml}
                                  </figcaption>
                                  <figcaption class="m-auto mb-2 text-sm text-center text-gray-500 break-all" x-show="toggleAllImage">
                                      {descriptionContentHtml}
                                  </figcaption>
                              </figure>
                          </div>
                          """);

        return;

        (bool IsSuccess, string DataUri, Size size) ConvertImageToDataUri()
        {
            try
            {
                if (_isBase64)
                {
                    var base64Data = Uri.Split(',')[1];
                    var imageBytes = Convert.FromBase64String(base64Data);
                    var (_, imageSize) = ImageHelper.GetImageInfo(imageBytes);

                    return (true, Uri, imageSize);
                }
                else
                {
                    if (File.Exists(Uri) == false) return (false, $"The specified image file does not exist. {Uri}", Size.Empty);
                    var imageBytes = File.ReadAllBytes(Uri);

                    var base64String = Convert.ToBase64String(imageBytes);
                    var (imageTypeEnum, imageSize) = ImageHelper.GetImageInfo(imageBytes);

                    return (true, $"data:{EnumHelper.ToDescriptionString(imageTypeEnum)};base64,{base64String}", imageSize);
                }
            }
            catch (Exception ex)
            {
                const string convertImageToDataUriFailed = "Convert Image To Data Uri Failed";
                return (false, $"{convertImageToDataUriFailed}: {ex}", Size.Empty);
            }
        }
    }

    internal override string ToViewString() => Description;
}
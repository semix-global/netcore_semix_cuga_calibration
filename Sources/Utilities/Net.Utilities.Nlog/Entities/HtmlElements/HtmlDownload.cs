using Net.Utilities.Helper.File;
using Newtonsoft.Json;

namespace Net.Utilities.Nlog.Entities.HtmlElements;

public sealed record HtmlDownload : AbstractHtmlElement
{
    [JsonProperty]
    private readonly string _id = Guid.NewGuid().ToString("N");

    [JsonIgnore]
    public byte[] Bytes { get; }

    public string FileName { get; }

    public HtmlDownload(string uri) : this(File.Exists(uri) ? File.ReadAllBytes(uri) : [], uri)
    {
    }

    public HtmlDownload(byte[] bytes, string fileName)
    {
        if (string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(fileName)))
            fileName = $"{FileHelper.RemoveInvalidFileName(Guid.NewGuid().ToString())}{Path.GetExtension(fileName)}";

        Bytes = bytes;
        FileName = FileHelper.RemoveInvalidFileName(Path.GetFileName(fileName));
    }

    internal override void WriteToHtml(TextWriter writer)
    {
        if (Bytes.Length == 0)
        {
            writer.WriteLine($"""
                              <button class="py-1 px-2 h-[21px] inline-flex items-center gap-x-1 text-[8px] rounded-md border border-transparent bg-red-600 text-white hover:bg-red-700 focus:outline-none focus:bg-red-700 disabled:opacity-50 disabled:pointer-events-none" title="{FileName}" type="button">
                                  The specified image file does not exist.
                              </button>
                              """);
        }

        writer.WriteLine($$"""
                           <button class="py-1 px-2 h-[21px] inline-flex items-center gap-x-1 text-[8px] rounded-md border border-transparent bg-blue-600 text-white hover:bg-blue-700 focus:outline-none focus:bg-blue-700 disabled:opacity-50 disabled:pointer-events-none" title="{{FileName}}" @click="$event.stopPropagation(); export{{_id}}();" type="button">
                               Download
                               <svg class="shrink-0 size-4" fill="currentColor" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="0.5" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                   <path d="M4 22v-2h16v2zm8-4L5 9h4V2h6v7h4zm0-3.25L14.9 11H13V4h-2v7H9.1zM12 11"/>
                               </svg>
                               <script>
                                   function export{{_id}}() {
                                       const link = document.createElement("a");
                                       link.href = "data:application/octet-stream;base64,{{Convert.ToBase64String(Bytes)}}"
                                       link.download = "{{FileName}}";
                                       link.click();
                                       URL.revokeObjectURL(link.href);
                                   }
                               </script>
                           </button>
                           """);
    }

    internal override string ToViewString() => FileName;
}
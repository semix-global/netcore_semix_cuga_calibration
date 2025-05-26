using CommunityToolkit.Diagnostics;
using Net.Utilities.Enums;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using System.Reflection;

namespace Net.Utilities.Nlog.Entities;

public sealed record Html
{
    private static readonly string Css = GetEmbeddedResource("Net.Utilities.Nlog.Assets.Css.index-LP96SQ8L.css");
    private static readonly string Js = GetEmbeddedResource("Net.Utilities.Nlog.Assets.Js.index-DKuw_BDL.js");
    private static readonly string PlotlyJs = GetEmbeddedResource("Net.Utilities.Nlog.Assets.Js.plotly-2.35.2.min.js");
    private static readonly string EchartJs = GetEmbeddedResource("Net.Utilities.Nlog.Assets.Js.echarts.min.js");
    private static readonly string EchartGlJs = GetEmbeddedResource("Net.Utilities.Nlog.Assets.Js.echarts-gl.min.js");

    public List<AbstractHtmlElement> ElementList { get; } = [];

    public void Add(string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, LogLevelEnum logLevelEnum, HtmlLog? htmlLog = null)
    {
        ElementList.Add(new HtmlHeader
        (
            header,
            htmlHeaderLevelEnum,
            logLevelEnum,
            htmlLog
        ));
    }

    public void Add(string header, HtmlHeaderLevelEnum htmlHeaderLevelEnum, LogLevelEnum logLevelEnum, AbstractHtmlElement htmlElement, HtmlLog htmlLog)
    {
        htmlLog.ContentList.Add(htmlElement);

        ElementList.Add(new HtmlHeader
        (
            header,
            htmlHeaderLevelEnum,
            logLevelEnum,
            htmlLog
        ));
    }

    public string ToHtml()
    {
        using var writer = new StringWriter();
        WriteToHtml(writer);

        return writer.ToString();
    }

    public void WriteToHtml(TextWriter writer)
    {
        var navigation = this.ToNavigation();

        writer.WriteLine($$"""
                           <!DOCTYPE html>
                           <html lang="en">

                           <head>
                               <meta charset="utf-8"/>
                               <meta content="width=device-width, initial-scale=1" name="viewport"/>
                               <style charset="utf-8">{{Css}}</style>
                               <script charset="utf-8">{{Js}}</script>
                               <script charset="utf-8">{{PlotlyJs}}</script>
                               <script charset="utf-8">{{EchartJs}}</script>
                               <script charset="utf-8">{{EchartGlJs}}</script>
                           </head>
                           <body x-data="viewModel">
                              <div class="fixed right-6 bottom-6 space-y-3 z-50 bg-transparent">
                                    <button onclick="document.getElementById('right-panel').scrollTo({  top: 0, behavior: 'smooth' })"
                                            class="button-container text-amber-600 hover:bg-amber-100">
                                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 10l7-7m0 0l7 7m-7-7v18"/>
                                        </svg>
                                    </button>
                                    <button onclick="const el=document.getElementById('right-panel');  el.scrollTo({  top: el.scrollHeight,behavior: 'smooth' })"
                                            class="button-container text-slate-600 hover:bg-slate-100">
                                        <svg class="w-4 h-4 " fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 14l-7 7m0 0l-7-7m7 7V3"/>
                                        </svg>
                                    </button>
                                    <button id="buttonLeft" onclick="const el=document.getElementById('left-panel'); if (!el.classList.contains('hidden'))  {el.classList.add('hidden');}"
                                            class="button-container text-amber-600 hover:bg-amber-100">
                                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
                                        </svg>
                                    </button>
                                    <button id="buttonRight" onclick="const el=document.getElementById('left-panel'); if (el.classList.contains('hidden'))  { el.classList.remove('hidden');el.classList.add('visible')}"
                                            class="button-container  text-slate-600 hover:bg-slate-100">
                                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
                                        </svg>
                                    </button>
                           </div>
                           <div id="horizontal-splitter" class="flex h-screen w-full bg-gray-100">
                              <aside id="left-panel" class="shrink-0 w-64 m-1">
                                  <div class="flex flex-col gap-1 h-full">
                                      <nav class="bg-white rounded border shadow p-1 overflow-auto flex-1 flex flex-col items-start gap-1" x-ref="nav">
                                          <h5 class="self-center text-base font-bold text-black">Menu</h5>
                                          {{navigation}}
                                      </nav>
                                      <div class="bg-white rounded border shadow p-1 flex flex-col gap-1">
                                          <h5 class="self-center text-base font-bold text-black">Setting</h5>
                                          <div class="gap gap-cols-2 place-content-center place-items-center">
                                              <button @click="toggleAllMenuVisibility($refs.nav)" class="toggle-button" x-text="toggleAllMenu ? 'Menu Collapse' : 'Menu Expand'"></button>
                                              <button @click="toggleAllLogVisibility($refs.body)" class="toggle-button" x-text="toggleAllLog ? 'Log Hide' : 'Log Show'"></button>
                                              <button @click="toggleAllImage=!toggleAllImage" class="toggle-button" x-text="toggleAllImage ? 'Image Hide' : 'Image Show'"></button>
                                          </div>
                                      </div>
                                  </div>
                              </aside>

                              <div id="horizontal-handle" class="w-1 bg-gray-200 hover:bg-blue-500 cursor-col-resize transition-colors"></div>

                               <div id="right-panel" class="flex-1 overflow-auto space-y-2 pl-1 pr-2" x-ref="body">
                           """);

        foreach (var element in ElementList)
        {
            element.WriteToHtml(writer);
        }

        writer.WriteLine("""
                             </div>
                         </div>
                         </body>

                         </html>
                         """);
    }

    private static string GetEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null) ThrowHelper.ThrowArgumentException($"Resource '{resourceName}' not found in assembly '{assembly.FullName}'.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
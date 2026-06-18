using CommunityToolkit.Diagnostics;
using Markdig;
using System.IO;
using System.Text.RegularExpressions;

namespace Core.Utilities.WPF.Entities;

public sealed record MarkdownHtmlBuilder
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> FileLocks = new();
    private static readonly string Css = GetEmbeddedResource("Core.Utilities.WPF.Assets.Css.githubish.css");
    private static readonly string PrismJs = GetEmbeddedResource("Core.Utilities.WPF.Assets.Js.prism.js");
    private static readonly string PrismCss = GetEmbeddedResource("Core.Utilities.WPF.Assets.Css.prism.css");

    // HTML 格式版本标记，用于检测旧版本并触发重建
    private const string HtmlFormatVersion = "3.9.8";

    // 缓存 Markdig pipeline，避免每次重新构建（性能提升 30-50%）
    private static readonly MarkdownPipeline CachedPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>
    /// Builds HTML from Markdown content without virtual host name injection.
    /// 生成纯净的 HTML，保持相对路径，用于保存到文件
    /// </summary>
    /// <param name="markdown">The Markdown content to convert.</param>
    /// <returns>A complete HTML document string with inline CSS and JS.</returns>
    private static string BuildHtmlFromMarkdown(string markdown)
    {
        // 使用缓存的 pipeline，避免每次重新构建
        var bodyHtml = Markdown.ToHtml(markdown, CachedPipeline);

        const string responsiveCss = """
                                     html, body {{
                                         margin: 0;
                                         padding: 0;
                                         width: 100%;
                                         height: 100%;
                                         box-sizing: border-box;
                                         transition: none !important;
                                         animation: none !important;
                                     }}
                                     html {{ overflow: auto; }}
                                     body {{ overflow: visible !important; }}
                                     .markdown-body {{
                                         width: 100%;
                                         height: auto;
                                         min-height: 100%;
                                         box-sizing: border-box;
                                         padding: 16px;
                                         transition: none !important;
                                         animation: none !important;
                                     }}
                                     """;

        const string overrideCss = """
                                   /* 覆盖 githubish.css 的固定宽度，使 body 在 WebView2 中响应式 */
                                   body {{
                                       width: 100% !important;
                                       max-width: 100% !important;
                                       margin: 0 !important;
                                       box-sizing: border-box !important;
                                   }}
                                   """;

        const string viewportCss = """
                                   .container { width: 100%; }
                                    @media (max-width: 768px) { ... }
                                   """;

        const string prismOverrideCss = """
                                        pre[class*="language-"] { overflow-x: auto; }
                                        """;

        const string popupButtonCss = """
                                      #popup-btn {
                                          position: fixed;
                                          top: 10px;
                                          right: 10px;
                                          z-index: 9999;
                                          display: none;
                                          padding: 6px 12px;
                                          background: #fff;
                                          border: 1px solid #d0d7de;
                                          border-radius: 6px;
                                          cursor: pointer;
                                          font-size: 13px;
                                          font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif;
                                          color: #24292f;
                                          box-shadow: 0 1px 3px rgba(0,0,0,0.1);
                                          transition: background 0.2s;
                                      }
                                      #popup-btn:hover { background: #f3f4f6; }
                                      """;

        const string popupButtonJs = """
                                     document.addEventListener('DOMContentLoaded', function() {
                                         var btn = document.createElement('button');
                                         btn.id = 'popup-btn';
                                         btn.textContent = 'Popup';
                                         btn.title = 'Zoom';
                                         btn.onclick = function() {
                                             if (window.chrome && window.chrome.webview) {
                                                 window.chrome.webview.postMessage('popup');
                                             }
                                         };
                                         document.body.appendChild(btn);
                                     });
                                     """;

        const string tocCss = """
                              .toc {
                                  position: fixed;
                                  top: 0;
                                  left: 0;
                                  width: 220px;
                                  height: 100vh;
                                  background: #f6f8fa;
                                  border-right: 1px solid #d0d7de;
                                  overflow-y: auto;
                                  z-index: 100;
                                  padding: 12px;
                                  box-sizing: border-box;
                                  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif;
                                  font-size: 12px;
                                  line-height: 1.6;
                              }
                              .toc {
                                  transition: width 0.25s ease, padding 0.25s ease;
                              }
                              .toc.collapsed {
                                  width: 32px;
                                  overflow: hidden;
                                  padding: 8px 0;
                              }
                              .toc.collapsed .toc-list,
                              .toc.collapsed .toc-sublist,
                              .toc.collapsed .toc-title,
                              .toc.collapsed .toc-resizer { display: none; }
                              .toc.collapsed .toc-header {
                                  justify-content: center;
                                  padding-bottom: 0;
                                  border-bottom: none;
                                  margin-bottom: 0;
                              }
                              .toc.collapsed .toc-toggle {
                                  font-size: 18px;
                                  padding: 2px 0;
                              }
                              .toc-header {
                                  display: flex;
                                  justify-content: space-between;
                                  align-items: center;
                                  margin-bottom: 10px;
                                  padding-bottom: 6px;
                                  border-bottom: 1px solid #d0d7de;
                              }
                              .toc-title { font-weight: 600; font-size: 13px; color: #24292f; }
                              .toc-toggle {
                                  background: none; border: none; cursor: pointer;
                                  font-size: 15px; color: #57606a; padding: 0 4px; line-height: 1;
                              }
                              .toc-toggle:hover { color: #24292f; }
                              .toc-list, .toc-sublist { list-style: none; padding: 0; margin: 0; }
                              .toc-sublist { padding-left: 14px; }
                              .toc-item { margin: 2px 0; }
                              .toc-row { display: flex; align-items: baseline; padding-left: 14px; }
                              .toc-row a {
                                  color: #0969da; text-decoration: none; flex: 1;
                                  padding: 2px 4px; border-radius: 3px; transition: background 0.15s;
                                  white-space: nowrap;
                                  overflow: hidden;
                                  text-overflow: ellipsis;
                              }
                              .toc-row a:hover { background: #ddf4ff; }
                              .toc-item.active .toc-row > a { background: #0969da; color: #fff; }
                              .toc-node-toggle {
                                  cursor: pointer;
                                  margin-left: -14px;
                                  margin-right: 3px;
                                  font-size: 9px;
                                  color: #57606a;
                                  user-select: none;
                                  display: inline-block;
                                  width: 11px;
                                  text-align: center;
                              }
                              .toc-node-toggle:hover { color: #24292f; }
                              .toc-resizer {
                                  position: absolute;
                                  top: 0;
                                  right: -4px;
                                  width: 8px;
                                  height: 100%;
                                  cursor: col-resize;
                                  background: transparent;
                                  z-index: 101;
                                  transition: background 0.15s;
                              }
                              .toc-resizer:hover,
                              .toc-resizer.dragging { background: #0969da; }
                              .markdown-body { margin-left: 220px; padding-top: 8px; }
                              .toc.collapsed ~ .markdown-body { margin-left: 32px; }
                              """;

        const string tocJs = """
                             (function() {
                                 function generateSlug(text) {
                                     return text.toLowerCase().replace(/<[^>]+>/g, '').replace(/[^a-z0-9\s-]/g, '').replace(/\s+/g, '-').replace(/-+/g, '-').replace(/^-|-$/g, '') || 'heading';
                                 }
                                 function buildToc() {
                                     var headings = document.querySelectorAll('.markdown-body h1, .markdown-body h2, .markdown-body h3, .markdown-body h4, .markdown-body h5, .markdown-body h6');
                                     if (headings.length === 0) return;
                                     var nav = document.createElement('nav');
                                     nav.className = 'toc';
                                     var header = document.createElement('div');
                                     header.className = 'toc-header';
                                     header.innerHTML = '<span class="toc-title">目录</span><button class="toc-toggle" title="折叠/展开">−</button>';
                                     nav.appendChild(header);
                                     var rootList = document.createElement('ul');
                                     rootList.className = 'toc-list';
                                     var stack = [{ level: 0, ul: rootList }];
                                     var counter = 0;
                                     headings.forEach(function(heading) {
                                         var level = parseInt(heading.tagName[1]);
                                         if (!heading.id) {
                                             heading.id = generateSlug(heading.textContent || '') + '-' + (++counter);
                                         }
                                         while (stack.length > 1 && stack[stack.length - 1].level >= level) {
                                             stack.pop();
                                         }
                                         var li = document.createElement('li');
                                         li.className = 'toc-item toc-level-' + level;
                                         var row = document.createElement('div');
                                         row.className = 'toc-row';
                                         var a = document.createElement('a');
                                         a.href = '#' + heading.id;
                                         a.textContent = heading.textContent || '';
                                         a.addEventListener('click', function(e) {
                                             e.preventDefault();
                                             heading.scrollIntoView({ behavior: 'smooth', block: 'start' });
                                             history.replaceState(null, null, '#' + heading.id);
                                         });
                                         row.appendChild(a);
                                         li.appendChild(row);
                                         var parentUl = stack[stack.length - 1].ul;
                                         parentUl.appendChild(li);
                                         var childUl = document.createElement('ul');
                                         childUl.className = 'toc-sublist';
                                         li.appendChild(childUl);
                                         stack.push({ level: level, ul: childUl });
                                     });
                                     nav.appendChild(rootList);
                                     var resizer = document.createElement('div');
                                     resizer.className = 'toc-resizer';
                                     nav.appendChild(resizer);
                                     document.body.insertBefore(nav, document.body.firstChild);
                                     var isResizing = false;
                                     var ghost = null;
                                     resizer.addEventListener('mousedown', function(e) {
                                         isResizing = true;
                                         resizer.classList.add('dragging');
                                         document.body.style.cursor = 'col-resize';
                                         document.body.style.userSelect = 'none';
                                         ghost = document.createElement('div');
                                         ghost.style.cssText = 'position:fixed;top:0;left:' + nav.offsetWidth + 'px;width:2px;height:100vh;background:#0969da;z-index:9999;pointer-events:none;opacity:0.7;';
                                         document.body.appendChild(ghost);
                                         if (nav.classList.contains('collapsed')) {
                                             nav.classList.remove('collapsed');
                                             nav.querySelector('.toc-toggle').textContent = '−';
                                         }
                                         e.preventDefault();
                                     });
                                     document.addEventListener('mousemove', function(e) {
                                         if (!isResizing || !ghost) return;
                                         var x = e.clientX;
                                         if (x < 150) x = 150;
                                         if (x > 600) x = 600;
                                         ghost.style.left = x + 'px';
                                     });
                                     document.addEventListener('mouseup', function() {
                                         if (isResizing) {
                                             isResizing = false;
                                             resizer.classList.remove('dragging');
                                             document.body.style.cursor = '';
                                             document.body.style.userSelect = '';
                                             if (ghost) {
                                                 var newWidth = parseInt(ghost.style.left);
                                                 nav.style.width = newWidth + 'px';
                                                 ghost.remove();
                                                 ghost = null;
                                             }
                                         }
                                     });
                                     nav.querySelectorAll('.toc-sublist').forEach(function(sublist) {
                                         if (sublist.children.length > 0) {
                                             var parentLi = sublist.parentElement;
                                             if (parentLi && parentLi.classList.contains('toc-item')) {
                                                 var toggle = document.createElement('span');
                                                 toggle.className = 'toc-node-toggle';
                                                 toggle.textContent = '▼';
                                                 toggle.addEventListener('click', function(e) {
                                                     e.stopPropagation();
                                                     var isCollapsed = sublist.style.display === 'none';
                                                     sublist.style.display = isCollapsed ? '' : 'none';
                                                     toggle.textContent = isCollapsed ? '▼' : '▶';
                                                 });
                                                 var row = parentLi.querySelector('.toc-row');
                                                 if (row) row.insertBefore(toggle, row.firstChild);
                                             }
                                         }
                                     });
                                     var toggleBtn = nav.querySelector('.toc-toggle');
                                     toggleBtn.addEventListener('click', function() {
                                         nav.classList.toggle('collapsed');
                                         toggleBtn.textContent = nav.classList.contains('collapsed') ? '+' : '−';
                                     });
                                     var scrollTimer = null;
                                     window.addEventListener('scroll', function() {
                                         if (scrollTimer) return;
                                         scrollTimer = setTimeout(function() {
                                             scrollTimer = null;
                                             var currentId = '';
                                             for (var i = 0; i < headings.length; i++) {
                                                 if (headings[i].getBoundingClientRect().top > 60) break;
                                                 currentId = headings[i].id;
                                             }
                                             document.querySelectorAll('.toc-item').forEach(function(item) {
                                                 item.classList.remove('active');
                                             });
                                             if (currentId) {
                                                 var active = nav.querySelector('.toc-item a[href="#' + currentId + '"]');
                                                 if (active) active.parentElement.parentElement.classList.add('active');
                                             }
                                         }, 100);
                                     });
                                 }
                                 if (document.readyState === 'loading') {
                                     document.addEventListener('DOMContentLoaded', buildToc);
                                 } else {
                                     buildToc();
                                 }
                             })();
                             """;

        return $"""
                <!DOCTYPE html>
                <html>
                <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <style>
                {viewportCss}
                </style>
                <meta name="generator" content="MarkdownViewer-v{HtmlFormatVersion}" />
                <style>
                {responsiveCss}
                </style>
                <style>
                {Css}
                </style>
                <style>
                {overrideCss}
                </style>
                <style>
                {PrismCss}
                </style>
                <style>
                {popupButtonCss}
                </style>
                <style>
                {tocCss}
                </style>
                </head>
                <body>
                <div class="markdown-body">
                {bodyHtml}
                </div>
                <script>{PrismJs}</script>
                <script>{popupButtonJs}</script>
                <script>{tocJs}</script>
                <style>
                {prismOverrideCss}
                </style>
                </body>
                </html>
                """;
    }

    /// <summary>
    /// Builds a fallback HTML page for displaying errors.
    /// </summary>
    /// <param name="title">The error title.</param>
    /// <param name="message">The error message.</param>
    /// <param name="icon">The Unicode icon to display (e.g., ⚠️, ❌, ℹ️).</param>
    /// <returns>A complete HTML document string with error information.</returns>
    public static string BuildFallbackHtml(string title, string message, string icon)
    {
        const string style = """
                             body { margin: 0; padding: 0; }
                             .error-container {
                                 display: flex;
                                 flex-direction: column;
                                 justify-content: center;
                                 align-items: center;
                                 height: 100vh;
                                 font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif;
                             }
                             .error-icon { font-size: 64px; margin-bottom: 20px; }
                             .error-title { font-size: 24px; font-weight: bold; margin-bottom: 10px; color: #24292e; }
                             .error-message { font-size: 14px; color: #586069; text-align: center; max-width: 600px; }
                             """;

        return $"""
                <!DOCTYPE html>
                <html>
                <head>
                <meta charset="utf-8">
                <style>
                {style}
                </style>
                </head>
                <body>
                <div class="error-container">
                    <div class="error-icon">{icon}</div>
                    <div class="error-title">{title}</div>
                    <div class="error-message">{message}</div>
                </div>
                </body>
                </html>
                """;
    }

    /// <summary>
    /// Builds an HTML page indicating that WebView2 Runtime is not installed.
    /// </summary>
    /// <returns>A complete HTML document string with installation instructions.</returns>
    public static string BuildWebView2NotInstalledHtml()
    {
        return BuildFallbackHtml(
            "WebView2 Runtime is not installed",
            "Unable to load Markdown content because the Microsoft Edge WebView2 runtime is missing。<br/><br/>Please visit <a href=\"https://developer.microsoft.com/microsoft-edge/webview2/\">Microsoft WebView2</a> 下载并安装。",
            "❌"
        );
    }

    /// <summary>
    /// Converts a Markdown file to HTML and saves it to the specified path.
    /// 转换 Markdown 文件为 HTML 并保存到指定路径（智能缓存版本）
    /// 自动复制 Markdown 副本和图片到指定目录，HTML 单独保存到另一个目录。
    /// Markdown 副本与源保持相同的文件夹结构，图片复制到 Markdown 副本的同目录。
    /// HTML 保存到单独的 htmlOutputPath，与 Markdown 副本保持相同的子文件夹结构。
    /// 不支持 file:/// 绝对路径图片，仅支持相对路径和 HTTP(S) 网络图片。
    /// </summary>
    /// <param name="markdownFilePath">The path to the source Markdown file.</param>
    /// <param name="markdownCopyOutputPath">The path where the Markdown copy will be saved (same structure as source).</param>
    /// <param name="htmlOutputPath">The path where the HTML file will be saved (separate directory, same sub-structure).</param>
    /// <param name="forceRebuild">Force rebuild even if cached HTML exists.</param>
    /// <returns>The path to the saved HTML file, or empty string if Markdown file does not exist.</returns>
    public static string ConvertAndSaveHtml(string markdownFilePath, string markdownCopyOutputPath, string htmlOutputPath, bool forceRebuild = false)
    {
        if (!File.Exists(markdownFilePath))
            return string.Empty;

        var markdownDirectory = Path.GetDirectoryName(markdownFilePath) ?? string.Empty;
        var markdownCopyDirectory = Path.GetDirectoryName(markdownCopyOutputPath) ?? string.Empty;
        var htmlDirectory = Path.GetDirectoryName(htmlOutputPath) ?? string.Empty;

        // 版本检测：如果 HTML 已存在但版本不匹配，强制重建
        if (!forceRebuild && File.Exists(htmlOutputPath))
        {
            var isOldVersion = CheckIfOldVersion(htmlOutputPath);
            if (isOldVersion)
            {
                forceRebuild = true;
            }
        }

        // 智能缓存：如果 HTML 已存在且比 Markdown 和所有图片新，直接返回（性能优化 90%+）
        if (!forceRebuild && File.Exists(htmlOutputPath))
        {
            var markdownLastWrite = File.GetLastWriteTimeUtc(markdownFilePath);
            var htmlLastWrite = File.GetLastWriteTimeUtc(htmlOutputPath);

            // 读取 Markdown 内容以提取图片
            var markdownContent = File.ReadAllText(markdownFilePath);
            var cachedImagePaths = ExtractImagePaths(markdownContent);

            // 找出所有源图片的最新修改时间
            var latestImageTime = markdownLastWrite;
            foreach (var imagePath in cachedImagePaths)
            {
                var sourceImagePath = Path.IsPathRooted(imagePath) ? imagePath : Path.Combine(markdownDirectory, imagePath);
                if (!File.Exists(sourceImagePath)) continue;
                var imageTime = File.GetLastWriteTimeUtc(sourceImagePath);
                if (imageTime > latestImageTime)
                    latestImageTime = imageTime;
            }

            // 如果 HTML 文件比 Markdown 和所有图片都新，说明已经是最新的
            if (htmlLastWrite >= latestImageTime)
            {
                return htmlOutputPath;
            }
        }

        // 读取 Markdown
        var markdown = File.ReadAllText(markdownFilePath);

        // 确保输出目录存在
        if (!string.IsNullOrEmpty(markdownCopyDirectory))
            Directory.CreateDirectory(markdownCopyDirectory);
        if (!string.IsNullOrEmpty(htmlDirectory))
            Directory.CreateDirectory(htmlDirectory);

        // 提取并并行复制图片（复制到 Markdown 副本目录，与副本同目录）
        var imagePaths = ExtractImagePaths(markdown);
        CopyImagesParallel(imagePaths, markdownDirectory, markdownCopyDirectory);

        // 规范化图片路径：计算图片相对于 HTML 文件的路径
        var normalizedMarkdown = NormalizeImagePaths(markdown, markdownDirectory, markdownCopyDirectory, htmlDirectory);

        // 保存 Markdown 副本（图片路径已改为相对路径，与副本同目录）
        var mdFileLock = FileLocks.GetOrAdd(markdownCopyOutputPath, _ => new object());
        lock (mdFileLock)
        {
            File.WriteAllText(markdownCopyOutputPath, normalizedMarkdown);
        }

        // 生成并保存 HTML
        var htmlContent = BuildHtmlFromMarkdown(normalizedMarkdown);
        var htmlFileLock = FileLocks.GetOrAdd(htmlOutputPath, _ => new object());
        lock (htmlFileLock)
        {
            File.WriteAllText(htmlOutputPath, htmlContent);
        }

        return htmlOutputPath;
    }

    /// <summary>
    /// 检查 HTML 文件是否是旧版本（没有版本标记或版本不匹配）
    /// </summary>
    private static bool CheckIfOldVersion(string htmlFilePath)
    {
        try
        {
            // 读取前 50 行查找版本标记
            var lines = File.ReadLines(htmlFilePath).Take(50);
            foreach (var line in lines)
            {
                if (!line.Contains("<meta name=\"generator\"", StringComparison.OrdinalIgnoreCase)) continue;
                // 提取版本号
                var match = Regex.Match(line, @"content=""MarkdownViewer-v([\d.]+)""", RegexOptions.IgnoreCase);
                if (!match.Success) continue;
                var version = match.Groups[1].Value;
                return version != HtmlFormatVersion; // 版本不匹配
            }

            // 没有找到版本标记，认为是旧版本
            return true;
        }
        catch
        {
            // 读取失败，安全起见重建
            return true;
        }
    }

    /// <summary>
    /// 规范化 Markdown 中的图片路径：
    /// 1. 去除 ./ 或 .\ 前缀
    /// 2. 将反斜杠转为正斜杠
    /// 3. 计算图片相对于 HTML 文件的路径（图片在 Markdown 副本目录，HTML 在单独目录）
    /// </summary>
    private static string NormalizeImagePaths(string markdown, string markdownDirectory, string markdownCopyDirectory, string htmlDirectory)
    {
        return Regex.Replace(
            markdown,
            @"!\[(.*?)\]\((?!https?://|file:///)(.*?)\)",
            m =>
            {
                var alt = m.Groups[1].Value;
                var path = m.Groups[2].Value.Trim();

                string copiedImagePath;
                if (Path.IsPathRooted(path))
                {
                    // 绝对路径：复制后扁平化到 Markdown 副本目录
                    copiedImagePath = Path.Combine(markdownCopyDirectory, Path.GetFileName(path));
                }
                else
                {
                    // 去除 ./ 或 .\ 前缀
                    if (path.StartsWith("./") || path.StartsWith(".\\"))
                        path = path[2..];

                    // 相对路径：复制后保持目录结构
                    copiedImagePath = Path.Combine(markdownCopyDirectory, path);
                }

                // 计算从 HTML 目录到复制后图片的相对路径
                var relativeToHtml = GetRelativePath(htmlDirectory, copiedImagePath);
                relativeToHtml = relativeToHtml.Replace('\\', '/');

                return $"![{alt}]({relativeToHtml})";
            },
            RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 从 Markdown 内容中提取所有非网络图片路径（包含相对路径和绝对路径）
    /// </summary>
    private static List<string> ExtractImagePaths(string markdown)
    {
        var imagePaths = new List<string>();
        // 正则匹配：![任意内容](路径)，排除 http:// https:// file:///
        var matches = Regex.Matches(markdown, @"!\[.*?\]\(((?!https?://|file:///).*?)\)", RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var imagePath = match.Groups[1].Value.Trim();
            imagePaths.Add(imagePath);
        }

        return imagePaths;
    }

    /// <summary>
    /// 并行复制图片文件到目标目录。
    /// 相对路径的图片保持原始目录结构，绝对路径扁平化为文件名。
    /// </summary>
    private static void CopyImagesParallel(List<string> imagePaths, string sourceBaseDirectory, string targetBaseDirectory)
    {
        if (imagePaths.Count == 0)
            return;

        Parallel.ForEach(imagePaths, imagePath =>
        {
            try
            {
                // 解析源路径：绝对路径直接使用，相对路径基于 sourceBaseDirectory
                var sourceImagePath = Path.IsPathRooted(imagePath) ? imagePath : Path.Combine(sourceBaseDirectory, imagePath);

                // 目标路径：相对路径保持原始结构，绝对路径扁平化
                string targetImagePath;
                if (Path.IsPathRooted(imagePath))
                {
                    targetImagePath = Path.Combine(targetBaseDirectory, Path.GetFileName(imagePath));
                }
                else
                {
                    targetImagePath = Path.Combine(targetBaseDirectory, imagePath);
                }

                // 确保目标图片目录存在
                var targetImageDirectory = Path.GetDirectoryName(targetImagePath);
                if (!string.IsNullOrEmpty(targetImageDirectory))
                    Directory.CreateDirectory(targetImageDirectory);

                // 仅在源文件存在时复制
                if (File.Exists(sourceImagePath))
                {
                    File.Copy(sourceImagePath, targetImagePath, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                // 单个图片复制失败不阻止整体流程
                System.Diagnostics.Debug.WriteLine($"Copy image failed: {imagePath}, {ex.Message}");
            }
        });
    }

    /// <summary>
    /// 计算从 basePath 到 targetPath 的相对路径（兼容 .NET Framework 和 .NET Core）。
    /// </summary>
    private static string GetRelativePath(string basePath, string targetPath)
    {
        if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(targetPath))
            return targetPath;

        basePath = Path.GetFullPath(basePath);
        targetPath = Path.GetFullPath(targetPath);

        if (basePath[^1] != Path.DirectorySeparatorChar && basePath[^1] != Path.AltDirectorySeparatorChar)
            basePath += Path.DirectorySeparatorChar;

        var baseUri = new Uri(basePath, UriKind.Absolute);
        var targetUri = new Uri(targetPath, UriKind.Absolute);

        if (baseUri.Scheme != targetUri.Scheme)
            return targetPath;

        var relativeUri = baseUri.MakeRelativeUri(targetUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        return relativePath.Replace('/', Path.DirectorySeparatorChar);
    }

    private static string GetEmbeddedResource(string resourceName)
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null) ThrowHelper.ThrowArgumentException($"Resource '{resourceName}' not found in assembly '{assembly.FullName}'.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
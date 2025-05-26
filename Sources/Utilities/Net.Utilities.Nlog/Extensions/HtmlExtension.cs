using Net.Utilities.Enums;
using Net.Utilities.Nlog.Entities;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace Net.Utilities.Nlog.Extensions;

internal static class HtmlExtension
{
    internal static string ToNavigation(this Html html)
    {
        var headers = html.ElementList.OfType<HtmlHeader>().ToList();
        var navigationTree = BuildNavigationTree(headers);

        IsBuildNavigationLevel(navigationTree);

        return ToHtml(navigationTree.ChildrenList);

        static Navigation BuildNavigationTree(List<HtmlHeader> headers)
        {
            var root = new Navigation(new HtmlHeader("root", 0, LogLevelEnum.Info), []);
            var stack = new Stack<Navigation>();
            stack.Push(root);

            foreach (var header in headers)
            {
                var node = new Navigation(header, []);

                while (stack.Peek().Header.HtmlHeaderLevelEnum >= header.HtmlHeaderLevelEnum) // 只要当前栈顶的层级 >= 当前标题的层级，就弹出栈顶节点
                {
                    stack.Pop();
                }

                stack.Peek().ChildrenList.Add(node); // 将当前节点添加到栈顶节点的子列表中
                stack.Push(node); // 将当前节点压入栈，成为新的栈顶
            }

            return root;
        }

        static LogLevelEnum IsBuildNavigationLevel(Navigation navigation)
        {
            if (navigation.ChildrenList.Count < 1)
                return navigation.Header.LogLevelEnum;

            var logLevelEnum = navigation.ChildrenList.Select(IsBuildNavigationLevel)
                .Aggregate(LogLevelEnum.Info, (current, childrenLogLevelEnum) => childrenLogLevelEnum switch
                {
                    LogLevelEnum.Error => LogLevelEnum.Error,
                    LogLevelEnum.Warn when current != LogLevelEnum.Error => LogLevelEnum.Warn,
                    _ => current
                });

            navigation.Header.LogLevelEnum = logLevelEnum;
            return logLevelEnum;
        }

        static string ToHtml(List<Navigation> navigationList, bool isRecursive = false, bool isExpand = true)
        {
            if (navigationList.Count == 0) return string.Empty;

            var li = string.Join(Environment.NewLine,
                from node in navigationList
                let navigationHtml = node.Header.ToNavigationHtml(node.ChildrenList.Count > 0)
                let childUl = ToHtml(node.ChildrenList, true, node.Header.HtmlHeaderLevelEnum.ToIsExpand())
                select string.IsNullOrWhiteSpace(childUl)
                    ? isRecursive
                        ? $"<li>{navigationHtml}</li>"
                        : $"""<li class="ml-[1rem]">{navigationHtml}</li>"""
                    : $"""
                       {(isRecursive
                               ? """<li class="-ml-[1rem]">"""
                               : $"""<li class="{node.Header.HtmlHeaderLevelEnum.ToClassOfMarginLeft()}">"""
                           )}
                       {navigationHtml}
                       {childUl}
                       </li>
                       """);

            var ul = $"""
                      {(isRecursive
                              ? $"""<ul class="menu-ul-display ml-6 mr-2" data-menu-visible="{isExpand.ToString().ToLower()}">"""
                              : """<ul class="menu-ul-display">"""
                          )}
                      {li}
                      </ul>
                      """;
            return ul;
        }
    }
}

file sealed record Navigation(HtmlHeader Header, List<Navigation> ChildrenList);
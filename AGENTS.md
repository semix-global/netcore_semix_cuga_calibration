# 用户全局工作规则

## 编译与构建

- 完成代码编辑后，默认不要执行编译或构建命令，因为编译和构建耗时较长。
- 只有当用户明确要求编译或构建时，才执行相关命令。
- 需要时可以执行不涉及编译或构建的静态检查。

## 代码提交与推送

- 完成代码修改后，编写详细的提交信息并提交本地代码。
- 提交信息中不得包含 `Co-Authored-By`。
- 如果当前分支名称符合 `feature/DEV-number-**` 格式，则提取其中的 `number`，并在提交标题开头添加 `#number`。
- 如果当前分支名称不符合上述格式，则检查上一个提交使用的任务编号，并在新提交标题开头添加相同的 `#number`。
- 如果从当前分支名称和上一个提交中都找不到任务编号，则提交标题不添加编号。
- 完成本地提交后，将当前本地分支推送到其已配置的远程分支。
- 如果推送失败，不要尝试其他推送操作；立即停止并询问用户下一步如何处理。

## 文件删除安全规则

- 禁止批量删除文件或目录。
- 不要使用以下命令：
  - `del /s`
  - `rd /s`
  - `rmdir /s`
  - `Remove-Item -Recurse`
  - `rm -rf`
- 需要删除文件时，一次只能删除一个具有明确完整路径的文件。
- 单文件删除示例：`Remove-Item "C:\path\to\file.txt"`。
- 如果需要批量删除文件，应停止操作并向用户说明，由用户手动删除。

## 文件编码与结尾格式

- 新建或修改的文本文件统一使用 UTF-8 无 BOM 编码, 换行使用 CRLF.
- 新建或修改文件后，文件最后一行末尾不要添加换行符；文件应直接结束于最后一个有效字符。

## C# 编码风格

- 命名中的缩写字母必须全部大写, 例如 `Dto` 应写为 `DTO`, `Ecs` 应写为 `ECS`.
- `double` 类型的整数形式字面量优先使用 `d` 后缀，例如写 `2d`，不要写 `2.0`。
- `double` 类型的小数形式字面量不添加 `d` 后缀, 例如写 `0.5`, 不要写 `0.5d`.
- `bool` 类型的变量和参数必须以 `is` 开头, 属性使用 `Is` 前缀, 私有字段使用 `_is` 前缀.
- 仅用于保存方法或委托最终返回结果的 `bool` 局部变量, 允许命名为 `result`, 并在对应逻辑末尾使用 `return result`. 过程中的判断和状态变量, 以及参数仍须遵循 `is` 前缀规则.
- 布尔取反使用 `== false`, 不使用 `!`. 不等比较 `!=` 不受此规则影响.
- 禁止使用空值抑制运算符 `!`, 包括 `a!.b`, `a![index]` 和 `null!`. 使用显式判空, 模式匹配或现有校验与断言取得非空对象, 再访问其成员.
- 方法或代码块只有一条 `return` 语句时, 不在其前面添加空行.
- `return` 或包含 `return` 的提前返回判断是当前代码块的第一条语句时, 不在其前面添加空行.
- `return` 前还有其他语句时, 在返回语句或单行提前返回判断所在行前保留一个空行.
- 保留单行提前返回写法, 不在 `if` 条件与其对应的 `return` 之间插入空行.
- LINQ 中需要同时使用元素及其索引时, 统一先调用 `.Index()`, 再通过元组的 `Item` 和 `Index` 访问元素与索引, 不使用 `.Select((item, index) => ...)` 等带索引参数的重载. 需要保留原序列索引时, 在过滤前调用 `.Index()`.

## 属性顺序同步

- 调整模型属性声明顺序时, 必须检索对应属性的全部引用, 同步调整构造与对象初始化, 赋值, 复制与映射, 重置, 相关用法, 日志与导出字段, 页面展示和已有测试中的排列顺序, 在各自可以连续处理的逻辑块内与新的属性声明顺序保持一致.
- 对象初始化器中能够一起赋值的属性, 按声明顺序集中写在同一个 `{}` 中. `Rods`, 只读 ROI 或集合等需要单独复制的成员, 在初始化器之后独立处理, 不为跨块排序拆散初始化赋值.
- 普通赋值, 随机赋值和测试断言遵循同样的分组原则. 简单字段连续处理, 集合, ROI 和子对象的逐项处理或专用方法调用单独成组; 各组内部保持属性顺序, 不强制不同处理块之间的整体顺序.
- 日志模板中的字段顺序与实参顺序必须对应. 涉及计算依赖时, 先按依赖完成计算与校验, 再按属性声明顺序赋值, 保持计算结果和业务行为正确. 不得只移动属性声明而遗漏关联位置.
- 子对象或公共断言方法已经覆盖的字段, 外层不再添加同义的重复断言.

## XAML 格式化

- 新建或修改 XAML 文件后，使用 XAML Styler 对文件进行格式化。

## WPF 控件样式与 Converter 代码风格

本节约定控件样式及 Converter 代码风格. 示例中的绑定与内容按实际页面调整.

### 只读 TextBox

- 只读信息绑定显式使用 `Mode=OneWay`.
- 使用 `hc:TitleElement.Title`, `TitlePlacement="Left"`, `TitleWidth="190"`, `IsReadOnly="True"`, `SecondaryRegionBrush` 背景和 `TextBoxExtend` 样式.
- 同一条绑定同时包含 `Mode` 和 `Converter` 时, `Mode` 写在 `Converter` 前面, 例如 `Mode=OneWay, Converter={StaticResource RectToStringConverter}`.
- 普通只读信息默认不设置 `TextWrapping="Wrap"`, 参考以下写法.

```xml
<TextBox Text="{Binding Value, Mode=OneWay}"
         hc:TitleElement.Title="Value: "
         hc:TitleElement.TitlePlacement="Left"
         hc:TitleElement.TitleWidth="190"
         IsReadOnly="True"
         Background="{StaticResource SecondaryRegionBrush}"
         Style="{StaticResource TextBoxExtend}" />
```

### NumericUpDown 与 ComboBox

- `hc:NumericUpDown` 的 `Value` 默认是 `TwoWay`, 不要显式写 `Mode=TwoWay`.
- `ComboBox` 和 `Selector` 的 `SelectedValue` 默认是 `TwoWay`, 不要显式写 `Mode=TwoWay`.
- `ItemsSource` 默认是 `OneWay`, 不要显式写 `Mode=OneWay`.

### 文件路径 TextBox

- 显示文件路径的 TextBox 统一设置 `Height="50"`, `TextWrapping="Wrap"` 和 `ScrollViewer.VerticalScrollBarVisibility="Auto"`.
- 使用 `hc:TitleElement.Title`, `TitlePlacement="Left"`, `TitleWidth="190"`, `IsReadOnly="True"`, `SecondaryRegionBrush` 背景和 `TextBoxExtend` 样式.
- 仅用于显示路径时, `Text` 绑定显式使用 `Mode=OneWay`. 需要通过文件选择命令回写路径时, 使用 `Mode=TwoWay`, 同时保留 `IsReadOnly="True"`.
- 标题和绑定路径按实际页面调整, 参考以下写法.

```xml
<TextBox Text="{Binding FilePath, Mode=OneWay}"
         Height="50"
         TextWrapping="Wrap"
         hc:TitleElement.Title="File Path: "
         hc:TitleElement.TitlePlacement="Left"
         hc:TitleElement.TitleWidth="190"
         Background="{StaticResource SecondaryRegionBrush}"
         IsReadOnly="True"
         ScrollViewer.VerticalScrollBarVisibility="Auto"
         Style="{StaticResource TextBoxExtend}" />
```

### 只读结果区域与标题

- 同一结果区域内的只读信息按对应模型属性的声明顺序, 从上到下排列.
- 外层 GroupBox 已有能说明当前区域用途的标题时, 例如 `History` 或 `Channel 1 Odd Image`, 直接复用现有标题, 不再额外添加 `Result` 标题或仅包裹该标题的 `StackPanel`.
- 没有现成分组标题的独立只读结果区域使用 `Result` 标题.

### ListView 与行样式

- 列表默认使用 `ListView.Small`, 横向和纵向滚动条均设置为 `Auto`.
- 行容器使用基于 `ListViewItemBaseStyle.Small` 的样式, 内容水平和垂直居中. 选中背景使用 `PrimaryBrush`.
- 使用 GridView 时, 通过 `GridView.ColumnHeaderContainerStyle` 设置列头水平和垂直居中, 样式基于现有 `GridViewColumnHeader`. 列头对齐方式单独设置, 不依赖行容器的内容对齐设置.
- 页面已有的异常状态可使用 `DarkDangerBrush`, 具体属性与触发条件按页面定义.
- 列表行为参考以下写法. 额外的选择同步行为按页面实际需要配置.

```xml
<ListView ItemsSource="{Binding Items}"
          Style="{StaticResource ListView.Small}"
          ScrollViewer.VerticalScrollBarVisibility="Auto"
          ScrollViewer.HorizontalScrollBarVisibility="Auto">
    <i:Interaction.Behaviors>
        <itemsControlBehaviors:ListBoxScrollToCurrentBehavior />
    </i:Interaction.Behaviors>
    <ListView.ItemContainerStyle>
        <Style BasedOn="{StaticResource ListViewItemBaseStyle.Small}" TargetType="ListViewItem">
            <Setter Property="HorizontalContentAlignment" Value="Center" />
            <Setter Property="VerticalContentAlignment" Value="Center" />
            <Style.Triggers>
                <Trigger Property="IsSelected" Value="True">
                    <Setter Property="Background" Value="{StaticResource PrimaryBrush}" />
                </Trigger>
            </Style.Triggers>
        </Style>
    </ListView.ItemContainerStyle>
    <ListView.View>
        <GridView>
            <GridView.ColumnHeaderContainerStyle>
                <Style BasedOn="{StaticResource {x:Type GridViewColumnHeader}}" TargetType="{x:Type GridViewColumnHeader}">
                    <Setter Property="HorizontalContentAlignment" Value="Center" />
                    <Setter Property="VerticalContentAlignment" Value="Center" />
                </Style>
            </GridView.ColumnHeaderContainerStyle>
            <GridViewColumn Width="360"
                            Header="Value"
                            DisplayMemberBinding="{Binding Value, Mode=OneWay}" />
        </GridView>
    </ListView.View>
</ListView>
```

### TabControl 样式

- TabControl 通过 `i:Interaction.Behaviors` 配置 `SelectorAutoSelectFirstBehavior`.
- TabItem 直接包含 TabControl 时, 该 TabControl 必须显式设置 `Margin="5"`.
- 页签中的普通页面内容使用 `Border Margin="5" Style="{StaticResource BorderRegion}"` 包裹.
- 嵌套 TabControl 的外边距与其内部页签内容 Border 的外边距均须保留, 两层都显式设置 `Margin="5"`, 不得因其中一层已有外边距而省略另一层.
- 以上外边距约定针对 TabItem 的内容, 不要求其他位置的 TabControl 或 Border 统一添加外边距.
- `GroupBox` 或 `Border` 中的 `TabControl` 不是 `TabItem` 的直接内容时, 不设置 `Margin="5"`.
- 页签名称, 内容, 数量, 顺序和页面布局按实际需求确定.

```xml
<TabControl>
    <i:Interaction.Behaviors>
        <itemsControlBehaviors:SelectorAutoSelectFirstBehavior />
    </i:Interaction.Behaviors>
    <TabItem Header="Tab">
        <Border Margin="5" Style="{StaticResource BorderRegion}">
            <Grid />
        </Border>
    </TabItem>
    <TabItem Header="Nested">
        <TabControl Margin="5">
            <i:Interaction.Behaviors>
                <itemsControlBehaviors:SelectorAutoSelectFirstBehavior />
            </i:Interaction.Behaviors>
            <TabItem Header="Content">
                <Border Margin="5" Style="{StaticResource BorderRegion}">
                    <Grid />
                </Border>
            </TabItem>
        </TabControl>
    </TabItem>
</TabControl>
```

### Converter 代码风格

- `Convert` 的排版参考 `PointYToStringConverter`, 使用方法体中的 `return value switch`, 类型与参数判断使用模式匹配.
- 单向 Converter 的 `ConvertBack` 使用表达式方法体. 类型名称冲突时使用类型别名.
- 格式字符串通过 `ConverterParameter` 传入. 可格式化对象直接调用自身的 `ToString(format, culture)`.
- 以下示例仅展示代码结构, 具体类型, 输入处理和转换逻辑按现有业务约定实现.

```csharp
public sealed class ValueToStringConverter : AbstractSingletonConverterBase<ValueToStringConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => DependencyProperty.UnsetValue,
            IFormattable formattable when parameter is string format => formattable.ToString(format, culture),
            _ => ThrowHelper.ThrowNotSupportedException<object>()
        };
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object>();
}
```

## 语言规范

- Git 提交信息（包括标题和正文）及其他 Git 相关说明统一使用中文。
- 新增或修改代码注释时，统一使用中文，包括 XML 文档注释。


## 中文文档输出格式

- 需要输出文档时, 使用中文编写.
- 统一使用英文标点符号.
- 每个标点符号后添加一个空格.
- 最后一个标点符号后不添加空格.

---

# 项目工作规则

## 只读结果显示

- 页面中的只读结果优先复用已有 GroupBox 标题; 没有现成分组标题时, 放在 `Result` 标题下方.
- 只读结果按对应模型属性的声明顺序, 从上到下依次排列.
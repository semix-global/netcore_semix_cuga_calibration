# 用户工作规则

本文件集中约定工作流程, 文档输出和代码规则. 涉及具体语言, 控件库或工具的规则, 仅在使用相应技术的项目中适用. 框架, 第三方 API 和生成器要求的固定签名及标识符保持原样.

## 工作流程

### 修改范围

- 开始修改前先阅读本文件, 已有约定直接遵守.
- 在保证业务正确性和必要验证的前提下, 尽量减少修改的现有代码和新增代码, 直接完成用户要求.
- 优先复用已有实现, 仅在确有需要时增加公共逻辑或新代码, 避免无关的重构和格式调整.

### 编译与构建

- 完成代码编辑后, 默认不要执行编译或构建命令, 因为编译和构建耗时较长.
- 只有当用户明确要求编译或构建时, 才执行相关命令.
- 需要时可以执行不涉及编译或构建的静态检查.

### 代码提交与推送

- 修改前检查当前分支和已有改动, 保留与本次任务无关的改动, 提交时仅包含本次任务涉及的文件.
- 完成代码修改后, 编写详细的提交信息并提交本地代码, 正文说明修改内容和实际完成的验证.
- 提交信息中不得包含 `Co-Authored-By`.
- 如果当前分支名称符合 `feature/DEV-number-**` 格式, 则提取其中的 `number`, 并在提交标题开头添加 `#number`.
- 如果当前分支名称不符合上述格式, 则检查上一个提交使用的任务编号, 并在新提交标题开头添加相同的 `#number`.
- 如果从当前分支名称和上一个提交中都找不到任务编号, 则提交标题不添加编号.
- 完成本地提交后, 将当前本地分支推送到其已配置的远程分支.
- 如果推送失败, 不要尝试其他推送操作, 立即停止并询问用户下一步如何处理.

### 文件删除安全规则

- 禁止批量删除文件或目录.
- 禁止使用 `del /s`, `rd /s`, `rmdir /s`, `Remove-Item -Recurse` 和 `rm -rf` 等批量或递归删除命令.
- 需要删除文件时, 一次只能删除一个具有明确完整路径的文件.
- 单文件删除示例: `Remove-Item -LiteralPath "C:\path\to\file.txt"`.
- 如果需要批量删除文件, 应停止操作并向用户说明, 由用户手动删除.

## 文档与语言规则

- 需要输出文档时, 使用中文编写.
- Git 提交信息, 包括标题和正文, 以及其他 Git 相关说明统一使用中文.
- 统一使用英文标点符号.
- 每个标点符号后添加一个空格. 最后一个标点符号后不添加空格.
- 代码, 标识符, 文件路径, 数值及 Markdown 语法遵循各自语法, 不按正文标点规则插入空格.

## 代码规则

### 文件编码与结尾格式

- 新建或修改的文本文件统一使用 UTF-8 无 BOM 编码, 换行使用 CRLF.
- 文件最后一行末尾不要添加换行符, 文件应直接结束于最后一个有效字符.
- 清理无意义的行尾空格和多余空白行, 不删除字符串内容或 Markdown 换行等具有语法意义的空白.

### 代码注释

- 新增或修改的说明性代码注释统一使用中文, 包括 XML 文档注释. 本规则适用于所有编程语言.
- ReSharper 抑制指令等需要工具识别的固定文本保持工具规定的语法.

### C# 编码风格

#### 命名与类型声明

- 自定义标识符中的缩写字母必须全部大写, 例如 `DTO`, `ECS` 和 `HRPPlotDataSource`, 不写成 `Dto`, `Ecs` 或 `HrpPlotDataSource`.
- Rider 或 ReSharper 将正确的业务缩写标为拼写错误时, 通过 `Alt+Enter` 将其加入项目共享字典, 不为消除提示而改变缩写大小写.
- 界面展示文本和图表图例中的缩写使用全称, 例如分别使用 `Height`, `Roll` 和 `Pitch` 展示 HRP 各分量.
- `bool` 类型的变量和参数必须以 `is` 开头, 属性使用 `Is` 前缀, 私有字段使用 `_is` 前缀.
- 布尔名称使用正向语义, 避免 `IsNotEnabled` 等否定名称与 `false` 组合产生双重否定.
- 仅用于保存方法或委托最终返回结果的 `bool` 局部变量, 允许命名为 `result`, 并在对应逻辑末尾使用 `return result`. 过程中的判断和状态变量, 以及参数仍须遵循 `is` 前缀规则.
- 没有派生类且不承担继承扩展用途的具体类添加 `sealed`, 包括私有嵌套类, 例如 `private sealed class PrivateSetterContractResolver`. 抽象类保留继承能力, 静态类不重复添加 `sealed`.

#### 二维数组, 矩阵和图像的坐标命名

- 处理 `T[][]`, `T[,]`, 矩阵或图像时, 行数表示纵向长度, 统一命名为 `yLength`. 列数表示横向长度, 统一命名为 `xLength`. 例如 `var (yLength, xLength) = ErrorMatrix.GetYXLength();`.
- 纵向索引用 `y`, 横向索引用 `x`. 禁止用 `row`, `col` 或其派生名称表示长度或索引, 例如 `rowCount`, `colCount`, `rowIndex` 和 `colIndex`.
- 按行遍历时, 外层用 `y`, 内层用 `x`, 通过 `array[y][x]`, `array[y, x]` 或 `matrix[y, x]` 访问元素. 调用图像 API 时按其坐标参数顺序传入 `x` 和 `y`.

#### 字面量与表达式

- `double` 类型的整数形式字面量优先使用 `d` 后缀, 例如写 `2d`, 不要写 `2.0`.
- `double` 类型的小数形式字面量不添加 `d` 后缀, 例如写 `0.5`, 不要写 `0.5d`.
- 布尔取反使用 `== false`, 不使用 `!`. 涉及 `bool?` 或空条件访问时显式处理 `null`, 不为统一写法改变原有逻辑. 不等比较 `!=` 不受此规则影响.
- 禁止使用空值抑制运算符 `!`, 包括 `a!.b`, `a![index]` 和 `null!`. 使用显式判空, 模式匹配或现有校验与断言取得非空对象, 再访问其成员.
- 删除冗余的字符串插值标记 `$`, 例如使用 `"X3"`, 不使用 `$"X3"`. 可通过 Rider 或 ReSharper 的 `Alt+Enter` 对应快速修复统一清理. 涉及大括号转义或原始字符串时必须保持字符串实际内容不变, 不机械删除 `$`.
- C# 允许尾部逗号的位置, 如对象初始化器, 集合初始化器, 集合表达式, 枚举和 `switch` 表达式等, 最后一个元素后不保留逗号, 元素之间必要的逗号照常保留.
- 优先复用已有对象的元数据和计算结果, 避免为传参重复构造等价对象或保留无用的中间变量. 相同处理逻辑的 `switch` 分支可使用 `or` 模式合并.

#### 方法调用与可选参数

- 无需显式设置的可选参数直接省略, 使用其声明的默认值.
- 显式传入可选参数时, 必须使用参数名称, 即使实参值等于默认值也遵守此规则, 例如 `bgrImage.GetValue<byte>(x, y, channel: 2)`.
- 参数是否可选以实际选用的重载声明为准. 必填参数不因后面存在可选参数而统一改为具名参数, 按现有调用约定和可读性处理.

#### 参数校验与异常

本节适用于已使用 `CommunityToolkit.Diagnostics` 的项目.

- 参数或状态的前置校验存在等价的 `Guard` 方法时使用该方法, 不手写等价的 `if` 加抛异常逻辑. 没有等价的 `Guard` 方法时允许保留必要的 `if` 判断.
- 需要创建并抛出新异常时, 存在等价的 `ThrowHelper` 方法就使用该方法, 包括上述 `if` 判断的异常分支. 表达式或返回值场景使用匹配结果类型的泛型重载, 例如 `ThrowHelper.ThrowNotSupportedException<object>()`.
- 替换前核对实际存在的重载和行为, 保留所需的异常类型, 参数名, 消息及资源释放行为. 不为简化写法而将对象已释放等特定异常替换成无关的参数异常.
- 仅当 `Guard` 无法等价完成所需校验, 且 `ThrowHelper` 没有满足所需异常语义的重载时, 才允许直接使用 `throw new`. 重新抛出已捕获的异常时保留 `throw;`. 不虚构 API 或为此新增无关依赖.

#### partial 回调与诊断指令

- 对生成器支持的属性变更 `partial` 回调, 不使用旧值时采用只接收新值的简洁重载, 不保留无用的 `oldValue` 参数. 需要旧值时保留双参数重载, 不擅自改变生成器约定的回调签名.
- 回调只执行一次方法调用时, 优先使用表达式方法体, 如下例.
- 回调签名要求保留但方法体未使用的参数, 确实产生 `UnusedParameterInPartialMethod` 警告时, 使用成对的 `ReSharper disable UnusedParameterInPartialMethod` 和 `ReSharper restore UnusedParameterInPartialMethod` 包裹相关回调, 将抑制范围限制在必要代码内.
- 不为消除未使用参数警告添加无意义的读取或赋值.

```csharp
// ReSharper disable UnusedParameterInPartialMethod
partial void OnFitPointsChanged(Point[] value) => RefreshPlot();
// ReSharper restore UnusedParameterInPartialMethod
```

#### return 与空行

- 对 `return` 和包含 `return` 的提前返回判断, 分别按各自所在的最近一层 `{}` 代码块判断空行.
- `return` 或包含 `return` 的提前返回判断是所在代码块的第一条语句时, 前面不添加空行, 包括代码块仅包含该条 `return` 语句的情况.
- `return` 或单行提前返回判断前还有其他语句时, 与上一条语句之间保留一个空行.
- 保留单行提前返回写法, 不在 `if` 条件与其对应的 `return` 之间插入空行. 带 `{}` 的提前返回, 对判断和内部的 `return` 分别按各自所在代码块判断.

#### LINQ 与索引

- LINQ 中需要同时使用元素及其索引时, 统一先调用 `.Index()`, 再通过元组的 `Item` 和 `Index` 访问元素与索引, 不使用 `.Select((item, index) => ...)` 等带索引参数的重载.
- 需要保留原序列索引时, 在过滤前调用 `.Index()`.

### 模型属性顺序同步

- 调整模型属性声明顺序时, 必须检索对应属性的全部引用, 同步调整构造与对象初始化, 赋值, 复制与映射, 重置, 相关用法, 日志与导出字段, 页面展示和已有测试中的排列顺序, 在各自可以连续处理的逻辑块内与新的属性声明顺序保持一致.
- 对象初始化器中能够一起赋值的属性, 按声明顺序集中写在同一个 `{}` 中. `Rods`, 只读 ROI 或集合等需要单独复制的成员, 在初始化器之后独立处理, 不为跨块排序拆散初始化赋值.
- 普通赋值, 随机赋值和测试断言遵循同样的分组原则. 简单字段连续处理, 集合, ROI 和子对象的逐项处理或专用方法调用单独成组. 各组内部保持属性顺序, 不强制不同处理块之间的整体顺序.
- 日志模板中的字段顺序与实参顺序必须对应. 涉及计算依赖时, 先按依赖完成计算与校验, 再按属性声明顺序赋值, 保持计算结果和业务行为正确. 不得只移动属性声明而遗漏关联位置.
- 子对象或公共断言方法已经覆盖的字段, 外层不再添加同义的重复断言.

### PlotDataSource

- 只有一个 plot 且使用默认配置时, 不调用无意义的无参 `Configure()`. 需要自定义配置或配置多个 plot 时, 保留实际需要的配置调用.
- 操作唯一的 plot 时, 使用库已提供的不带 `plotIndex` 的便捷重载或扩展方法, 例如 `SetTitle("Title")` 和 `ToggleLegend(true)`.
- 便捷 API 不代表含 `plotIndex` 的重载将该参数声明为可选. 多图或需要指定目标 plot 时, 使用带 `plotIndex` 的版本并按实际签名传参. 库中没有便捷 API 时保留必要的 `plotIndex`.

### XAML 与 WPF

本节的控件样式, 资源键和行为复用项目已有的 WPF, HandyControl 及相关工具库. 示例中的绑定, 标题和业务内容按实际页面调整.

#### XAML 格式化

- 新建或修改 XAML 文件后, 使用 XAML Styler 对文件进行格式化, 优先使用项目已有的配置.

#### 校准 StepView 页面布局

- 校准 StepView 页面已有的 `GridSplitter` 必须保留, 即使分隔条一侧暂时为空白, 也不要为了精简布局而删除. 同时保留分隔条对应的 `Grid` 行或列定义.

示例:

```xml
<GridSplitter Grid.Column="1" Width="5" />
```

#### 控件绑定与模板复用

- `ContentPresenter` 和内联 `DataTemplate` 仅用于包装单个控件并切换绑定上下文时, 直接在控件上绑定完整属性路径, 不增加这两层嵌套.
- 多处需要相同模板时, 可以通过 `x:Key` 定义并引用共享的 `DataTemplate`, 减少重复代码并支持一处修改统一生效. 控件机制或业务行为确实需要模板时, 保留必要模板.
- `PlotControl` 的 `DataSource` 绑定不显式写 `Mode=OneWay`.

单个控件直接绑定示例:

```xml
<v2:PlotControl DataSource="{Binding CalibratingItem.BFStageMap.PlotDataSource}" />
```

#### 绑定默认值与排列

- 绑定路径不要以 `.` 开头. 当前 `DataContext` 的属性直接写属性名, 例如 `{Binding AlignmentUserControlViewModel}`, 不写 `{Binding .AlignmentUserControlViewModel}`.
- 同一条绑定同时包含 `Mode` 和 `Converter` 时, `Mode` 写在 `Converter` 前面, 例如 `Mode=OneWay, Converter={StaticResource RectToStringConverter}`.
- `hc:NumericUpDown` 的 `Value` 默认是 `TwoWay`, 不要显式写 `Mode=TwoWay`.
- `ComboBox` 和 `Selector` 的 `SelectedValue` 默认是 `TwoWay`, 不要显式写 `Mode=TwoWay`.
- `ItemsSource` 默认是 `OneWay`, 不要显式写 `Mode=OneWay`.
- 只读结果和文件路径绑定遵循下文的显式 `Mode` 要求, 不套用省略默认值的规则.

#### 只读 TextBox

- 只读信息绑定显式使用 `Mode=OneWay`.
- 使用 `hc:TitleElement.Title`, `TitlePlacement="Left"`, `TitleWidth="190"`, `IsReadOnly="True"`, `SecondaryRegionBrush` 背景和 `TextBoxExtend` 样式.
- 普通只读信息默认不设置 `TextWrapping="Wrap"`.
- 由按钮或命令写入的机器位置参数使用下一节的规则.

```xml
<TextBox Text="{Binding Value, Mode=OneWay}"
         hc:TitleElement.Title="Value: "
         hc:TitleElement.TitlePlacement="Left"
         hc:TitleElement.TitleWidth="190"
         IsReadOnly="True"
         Background="{StaticResource SecondaryRegionBrush}"
         Style="{StaticResource TextBoxExtend}" />
```

#### 机器位置 TextBox

- Haze, Shiny 等由按钮或命令写入的机器位置属于参数, 按参数输入框处理.
- 不套用只读 TextBox 样式, 不写 `Mode`, `IsReadOnly` 和 `SecondaryRegionBrush`.
- `TextBox.Text` 默认使用 `TwoWay`, 位置由命令回写时沿用默认双向绑定, 绑定里可以只写 `Converter`.
- 保留 `hc:TitleElement.Title`, `TitlePlacement="Left"`, `TitleWidth` 和 `TextBoxExtend`. 标题较长时保留已有的较宽 `TitleWidth`, 例如 Shiny 使用 `230`, 不要为了对齐只读规则改成 `190`.

```xml
<TextBox Text="{Binding Cache.HazeFindBFMachinePosition, Converter={StaticResource PointToStringConverter}}"
         hc:TitleElement.Title="Haze BF Machine Position(um): "
         hc:TitleElement.TitlePlacement="Left"
         hc:TitleElement.TitleWidth="190"
         Style="{StaticResource TextBoxExtend}" />
```

#### 文件路径 TextBox

- 文件路径 TextBox 复用只读 TextBox 的标题, 标题宽度, 只读状态, 背景和样式设置, 额外设置 `Height="50"`, `TextWrapping="Wrap"` 和 `ScrollViewer.VerticalScrollBarVisibility="Auto"`.
- 仅用于显示路径时, `Text` 绑定显式使用 `Mode=OneWay`. 需要通过文件选择命令回写路径时, 使用 `Mode=TwoWay`, 同时保留 `IsReadOnly="True"`.

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

#### 只读结果区域与标题

- 同一结果区域内的只读信息按对应模型属性的声明顺序, 从上到下排列.
- 外层 GroupBox 已有能说明当前区域用途的标题时, 例如 `History` 或 `Channel 1 Odd Image`, 直接复用现有标题, 不再额外添加 `Result` 标题或仅包裹该标题的 `StackPanel`.
- 没有现成分组标题的独立只读结果区域使用 `Result` 标题.

#### ListView 与行样式

- 列表默认使用 `ListView.Small`, 横向和纵向滚动条均设置为 `Auto`.
- 行容器使用基于 `ListViewItemBaseStyle.Small` 的样式, 内容水平和垂直居中. 选中背景使用 `PrimaryBrush`.
- 使用 GridView 时, 通过 `GridView.ColumnHeaderContainerStyle` 设置列头水平和垂直居中, 样式基于现有 `GridViewColumnHeader`. 列头对齐方式单独设置, 不依赖行容器的内容对齐设置.
- 页面已有的异常状态可使用 `DarkDangerBrush`, 具体属性与触发条件按页面定义.
- 额外的选择同步行为按页面实际需要配置.

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

#### TabControl 样式

- TabControl 通过 `i:Interaction.Behaviors` 配置 `SelectorAutoSelectFirstBehavior`.
- TabItem 的直接内容是 `ListBox`, `TabControl`, `GroupBox` 这类已有自身边框的控件时, 不再额外包裹 `Border`. 外边距直接写在该控件上, 显式设置 `Margin="5"`.
- 页签中的普通页面内容使用 `Border Margin="5" Style="{StaticResource BorderRegion}"` 包裹.
- 嵌套 TabControl 的外边距与其内部页签内容的外边距均须保留, 两层都显式设置 `Margin="5"`, 不得因其中一层已有外边距而省略另一层.
- 以上外边距约定针对 TabItem 的内容, 不要求其他位置的 TabControl 或 Border 统一添加外边距.
- `GroupBox` 或 `Border` 中的 `TabControl` 不是 `TabItem` 的直接内容时, 不设置 `Margin="5"`.
- 页签名称, 内容, 数量, 顺序和页面布局按实际需求确定.

```xml
<TabControl>
    <i:Interaction.Behaviors>
        <itemsControlBehaviors:SelectorAutoSelectFirstBehavior />
    </i:Interaction.Behaviors>
    <TabItem Header="List">
        <ListBox Margin="5" />
    </TabItem>
    <TabItem Header="Group">
        <GroupBox Margin="5" Header="Group">
            <Grid />
        </GroupBox>
    </TabItem>
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
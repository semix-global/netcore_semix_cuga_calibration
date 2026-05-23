---
name: mvvm-convert-partial-property
description: |
  批量将 CommunityToolkit.Mvvm 的 [ObservableProperty] 字段格式转换为 public partial 分布属性格式。
  处理 private/public 字段、多行初始化器、[property: Newtonsoft.Json.JsonIgnore] 清理等。
---

## 触发条件

用户要求以下任一操作：
- 将 `[ObservableProperty]` 字段改为 partial property / 分布属性
- 升级 CommunityToolkit.Mvvm 字段格式到源生成器新格式
- 批量转换项目中所有 `[ObservableProperty]` 注解
- 清理 `[property: Newtonsoft.Json.JsonIgnore]` 等 attribute target specifier

## 转换规则

### 字段 → 分布属性

```csharp
// Before
[ObservableProperty]
private Type _fieldName = initialValue;

// After
[ObservableProperty]
public partial Type FieldName { get; set; } = initialValue;
```

命名规则：去掉前导下划线，首字母大写。`_algorithmTemplateTypeEnum` → `AlgorithmTemplateTypeEnum`。

同样适用于遗留的 `public` 字段：

```csharp
// Before
[ObservableProperty]
public Point[] _yGhostListCH11 = [];

// After
[ObservableProperty]
public partial Point[] YGhostListCH11 { get; set; } = [];
```

### 无边框下划线前缀的字段

部分字段未使用下划线前缀，直接使用小驼峰命名：

```csharp
// Before
[ObservableProperty]
private bool isFindWaferCenterOffsetPositionEnabled = true;

// After
[ObservableProperty]
public partial bool IsFindWaferCenterOffsetPositionEnabled { get; set; } = true;
```

命名规则：首字母大写即可。`isFindWaferCenterOffsetPositionEnabled` → `IsFindWaferCenterOffsetPositionEnabled`。

### 其他 Attribute

- `[NotifyPropertyChangedFor(nameof(X))]` — 保持位置不变，仅转换下方字段行
- `[property: Newtonsoft.Json.JsonIgnore]` → `[Newtonsoft.Json.JsonIgnore]`（移除 `property:` 前缀）
- `[RecipeCache]` / `[DefaultCache]` / `[NotifyCanExecuteChangedFor]` — 完全保持不动

### 已转换的属性跳过

文件中已有 `public partial Type Name { get; set; }` 格式的，**不做任何修改**。

## 执行步骤

1. **确认范围**
   - 默认扫描当前目录下所有 `.cs` 文件
   - 如用户指定了目录（如 `@Sources\`），按指定范围执行

2. **统计与分类**
   - `grep -rl "\[ObservableProperty\]"` 找出所有目标文件
   - 区分：已转换的 `public partial` vs 待转换的字段格式

3. **自动转换（Python 脚本）**
   - 编写一次性 Python 脚本逐行解析：
     - 收集连续 Attribute 行（`[...]`）
     - 判断下一行是否为 `private/public Type _name = ...;` 或 `private/public Type _name;`
     - 匹配则转换，不匹配则原样保留
     - 对 `[property: ...]` 执行正则替换：`\[property:\s*` → `[`
   - 脚本放在项目根目录，运行后立即删除

4. **人工处理边界情况**
   - 检查是否还有 `public/private .* _\w+` 紧跟在 `[ObservableProperty]` 后（遗漏的带下划线字段）
   - 检查是否还有 `private Type camelCaseName = ...;` 紧跟在 `[ObservableProperty]` 后（遗漏的无下划线字段）
   - 检查多行集合初始化器（`[` 或 `new() {` 跨多行）是否遗漏
   - 检查类似 `= new(), _field2 = new()` 这种被错误合并到一行的声明，拆分为独立属性

5. **验证**
   - 确认无 `[property: Newtonsoft.Json.JsonIgnore]` 残留
   - 确认无字段格式遗漏
   - 尝试 `dotnet build` 编译（如环境允许）

6. **提交**
   - 按项目 Git 规范编写 commit message
   - 提取分支名或上一次 commit 的 Issue 编号作为前缀
   - commit 后尝试 `git push`，失败则询问用户

## 注意事项

- **绝不转换**不带 `[ObservableProperty]` 的普通字段（如 `private readonly`、依赖注入字段）
- 转换后属性名的引用由源生成器自动处理（如 `_name` → `Name`）
- 转换量大时（>100 文件），优先用 Python 脚本批量处理，不要手动逐个 Edit

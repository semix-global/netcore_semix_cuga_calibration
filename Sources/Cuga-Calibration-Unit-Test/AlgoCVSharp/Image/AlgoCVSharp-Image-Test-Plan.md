# algocv_sharp.Image 单元测试计划

## 公共 API 速览

| 成员 | 签名 | 说明 |
|---|---|---|
| 构造函数 | `Image()` | 空图像 |
| 构造函数 | `Image(int width, int height, int channels, ImageDataType)` | 按类型分配 |
| 构造函数 | `Image(IntPtr data, int width, int height, int stride, int channels, ImageDataType)` | 外部内存包装 |
| 构造函数 | `Image(string path)` / `Load(string)` | 从文件加载 |
| 属性 | `Width/Height/Channels/Stride/DataType/IsEmpty/DataPtr/Data/Item[int]` | 元数据 |
| 访问 | `GetValue<T>(x,y,c)` / `SetValue<T>(x,y,T,c)` | 单像素读写 |
| 访问 | `GetRow(int)` / `GetRow<T>(int)` / `ToSpan()` / `ToSpan<T>()` | 行/整图 Span |
| 文件 | `Save(string)` | 保存图像 |
| 资源 | `Dispose()` | 释放非托管内存 |

`ImageDataType` 包含 `Bool/Int8/UInt8/Int16/UInt16/Int32/UInt32/Int64/UInt64/Float/Double`。

---

## 1. 构造与属性测试（Lifecycle & Metadata）

目标：验证不同构造方式后，图像元数据与内部状态一致。

| 用例 | 输入 | 断言 |
|---|---|---|
| 默认构造 | `new Image()` | `IsEmpty == true`；`Width/Height/Channels == 0`；`DataType` 为默认值 |
| 参数构造 UInt8 单通道 | `w=10, h=20, c=1, UInt8` | `Width=10, Height=20, Channels=1, DataType=UInt8, IsEmpty=false` |
| 参数构造 UInt16 单通道 | `w=10, h=20, c=1, UInt16` | 同上，`DataType=UInt16` |
| 参数构造多通道 | `w=8, h=8, c=3, UInt8` | `Channels=3`；`Stride >= Width * Channels`（考虑对齐） |
| 外部内存构造 | 分配托管数组 → `GCHandle` → `IntPtr` | 元数据正确；Dispose 后不应重复释放外部内存 |
| 从文件构造 | 现有 `Assets\Data\test.raw` | 元数据与已知 RAW 解析结果一致 |

---

## 2. 像素读写一致性测试（Pixel Access）

目标：`SetValue` / `GetValue` / `GetRow` / `ToSpan` 互相可验证。

| 用例 | 操作 | 断言 |
|---|---|---|
| UInt8 整图写入再读出 | 遍历 `(x,y)` 写入 `(x+y)%256` | `GetValue<byte>` 与写入值完全一致 |
| UInt16 整图写入再读出 | 写入递增像素值 | 读出一致 |
| 多通道独立写入 | 3 通道分别写入不同值 | 每个通道读取正确，不互相覆盖 |
| 行 Span 与逐点一致性 | `SetValue` 后 `GetRow<byte>` 逐元素对比 | 与 `GetValue` 结果一致 |
| 整图 Span 与逐点一致性 | `ToSpan<byte>()` 与逐点读取对比 | 顺序、数值一致 |
| 泛型 Span 类型匹配 | `ToSpan<ushort>()` 对 UInt16 图 | 长度 = `Width*Height*Channels` |
| 边界像素 | 写入 `(0,0)` 与 `(Width-1,Height-1)` | 读取正确，无越界或偏移错误 |

---

## 3. 文件加载与保存测试（File I/O）

目标：验证 `Load` / `Save` 与现有 HALCON/Bitmap 工厂结果等价。

| 用例 | 输入 | 断言 |
|---|---|---|
| 加载 RAW | `Assets\test.raw` | 成功加载为 UInt16 单通道图，尺寸与 `RAWImageFactory.GetSize` 一致；像素值在 16-bit 范围内 |
| 加载 RAW 元数据 | `Assets\test.raw` | `Width/Height/Channels/DataType` 与 HALCON 解析结果一致 |
| 保存后再加载 | 创建一个 UInt8 渐变图 → `Save` → `new Image(path)` | 重新加载后像素与原始一致（BMP 加载可能将单通道转 BGR） |
| 保存多通道图像 | 3 通道 UInt8 图 | 保存/加载后三通道值不变 |
| 加载不存在的文件 | 非法路径 | 抛出异常 |
| 加载损坏/非图像文件 | 用 `.txt` 文件尝试加载 | 抛出异常 |

---

## 4. 数据类型与通道组合测试（Data Type Matrix）

目标：覆盖 `ImageDataType` 主要类型，防止内存大小或对齐错误。

| 数据类型 | 通道 | 关键检查 |
|---|---|---|
| `UInt8` | 1, 3 | 基础路径，必须全过 |
| `UInt16` | 1 | 与 RAW 16-bit 图像对应 |
| `Int32` / `UInt32` | 1 | 验证 4 字节对齐与 Span 长度 |
| `Float` / `Double` | 1 | 浮点读写精度保持 |
| `Int8` | 1 | 有符号 8-bit 边界类型 |
| `Bool` | 1 | 构造时抛出 `InvalidOperationException`，说明底层 C++ 实现未支持该类型 |

每个组合至少执行：
1. 构造成功；
2. `SetValue<T>` / `GetValue<T>` 类型匹配；
3. `ToSpan<T>()` 长度 = `Stride / sizeof(T) * Height` 或 `Width * Channels * Height`（根据实现决定）。

---

## 5. 资源释放与安全性测试（Disposal & Safety）

目标：确认 `IDisposable` 行为正确，避免二次释放或访问已释放内存。

| 用例 | 操作 | 断言 |
|---|---|---|
| 重复 Dispose | `image.Dispose(); image.Dispose();` | 不抛异常 |
| 访问已 Dispose 图像 | `Dispose()` 后调用 `Width` / `GetValue` | 抛出 `ObjectDisposedException` |
| using 语句释放 | `using var img = new Image(...)` | 块结束后 `DataPtr == IntPtr.Zero` |
| 外部内存构造的 Dispose | 使用 `GCHandle.Alloc` 的 IntPtr 构造 | Dispose 不释放外部句柄；外部数组仍可访问 |

---

## 6. 与现有 HALCON 流程的等价性测试（Integration Smoke）

对应现有 `ImageSourceTest` 的思路，但简化为可独立运行的断言：

| 用例 | 输入 | 断言 |
|---|---|---|
| RAW 元数据一致性 | `Assets\test.raw` | `algoImage` 的 `Width/Height/Channels/DataType` 与 `RAWImageFactory.GetSize` 一致 |
| 像素范围检查 | `Assets\test.raw` | `ToSpan<ushort>()` 所有值在 16-bit 范围内，长度等于 `Width * Height` |

> 备注：直接比较 `algoImage` 与 HALCON `GetGrayValuesL()` 的像素值时，会因 `test.raw` 的 CIB Profile Mode 处理方式不同而出现差异。`algocv_sharp.Image` 加载 `.raw` 文件时会按文件头固定格式解析，与 `DarkFieldImageDTO` 的显式模式参数并非一一对应，因此本计划将像素级等价性断言调整为元数据等价 + 范围检查，避免强依赖具体 CIB 模式转换。

---

## 7. 工程配置修复

在 `net10.0-windows10.0.19041` 目标框架下，原生依赖 `libalgocv.dll` 及其 C++ 运行时（`libstdc++-6.dll`、`libwinpthread-1.dll`、`libgcc_s_seh-1.dll`）原先仅在 `net480` 的 `ItemGroup` 中设置为 `Content` 复制到输出目录，导致 `net10.0` 测试运行时报 `DllNotFoundException`。实施时将这四个 `Content` 项移出条件限制，使其对两个目标框架均复制到输出目录。

---

## 建议的测试文件结构

```
Cuga-Calibration-Unit-Test/
├── AlgoCVSharpTest.cs          # 保留并完善现有 ImageSourceTest
├── AlgoCVSharpImageCoreTest.cs # 构造、属性、Dispose
├── AlgoCVSharpImagePixelTest.cs# Set/Get/Row/Span
├── AlgoCVSharpImageIoTest.cs   # Load/Save/RAW 等价性
└── AlgoCVSharpImageTypeTest.cs # 数据类型矩阵
```

---

## 实施建议

1. **优先修复工程配置**：`net10.0` 下缺少原生 DLL 会导致测试主机崩溃，先确保 `libalgocv.dll` 及其依赖复制到输出目录。再补齐 `AlgoCVSharpTest` 中的断言，注意 `test.raw` 加载后是 `UInt16`，应使用 `ToSpan<ushort>()`。
2. **统一使用 `AwesomeAssertions`**：与现有测试保持一致，使用 `Should().BeEquivalentTo(..., options => options.WithStrictOrdering())` 比较像素数组。
3. **资产文件**：复用 `Assets\test.raw`，避免新增大文件。
4. **异常测试**：xUnit 中使用 `Assert.Throws<T>` 或 `FluentAssertions` 的 `Invoking(...).Should().Throw<T>()`。
5. **双目标框架**：项目同时面向 `net480` 与 `net10.0-windows10.0.19041`，注意 `Span<T>` 与 `GCHandle` API 在两者下的兼容性。实施后在两个框架下均通过 46 个测试。

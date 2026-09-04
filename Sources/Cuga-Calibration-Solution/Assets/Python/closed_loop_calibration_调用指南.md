# `closed_loop_calibration.py` 外部调用指南

本文说明外部程序如何使用 `closed_loop_calibration.py`，重点说明阶段 1 mask、阶段 2 逐扫描 masks，以及最终插值 mask 的传递方式。

注意：`closed_loop_calibration.py` 本身是 Python 模块，目前没有内置的 C ABI、REST 接口或命令行协议。外部控制程序可以是 Python，也可以是 C++、C#、LabVIEW、MATLAB 等其他语言。非 Python 程序不能直接 `import` 这个文件，需要由外部程序配套一个 Python 适配层；适配层可以做成独立进程、常驻服务，或嵌入到外部程序中。

## 0. 总体流程

整个闭环校准可以概括为：

```text
阶段1测量 residual + stage1_mask
        ↓
生成校准源网格 C0（去仿射后的测量误差），并将 mask=False 点填入有限的初始值
        ↓
把 C0 插值到设备下发网格，得到 C0_download
        ↓
下发 C0_download，开始阶段2扫描
        ↓
每次保存 residual_i + mask_i，并累计传入全部历史
        ↓
阶段2统计得到 deltaC + stage2_valid_mask
        ↓
合并 C0_download 和 deltaC，得到 C_final + interpolation_mask
        ↓
使用源点 interpolation_mask 和目标 final_target_mask 将 C_final 插值到最终下发网格
        ↓
下发 final_download_table
```

其中有四个关键点：

1. 阶段 1 的 `C0` 不是设备最终直接使用的表，必须先插值成 `C0_download` 后下发；阶段 2 的残差也必须在 `C0_download` 已经生效后测量。
2. 阶段 2 每次扫描的 mask 可以不同；这些逐点 mask 由外部测量程序根据设备状态或质量判断产生，外部程序必须保存完整的 `residuals` 和 `masks` 历史，不能只传最后一次扫描的 mask。
3. 阶段 1、阶段 2 都无效的点在最终源表中置为 `0.0`，并在 `interpolation_mask` 中标记为无效；最终设备表只使用其他有效源点插值生成。
4. 最终目标 mask 中 `True` 的位置才执行插值，`False` 的位置不插值，最终输出对应的 X/Y 值都为 `0.0`。

## 1. 调用方式

### 1.1 Python 调用方

如果调用程序与 `closed_loop_calibration.py` 位于同一目录，可以直接导入：

```python
from closed_loop_calibration import (
    combine_correction_tables,
    interpolate_residual_table,
    process_first_measurement,
    process_stage2_residuals,
)
```

如果调用程序位于其他目录，需要把本项目根目录加入 Python 的模块搜索路径，或按外部程序自己的工程方式配置模块路径。

下面第 3～7 节中的 Python 代码，表示“Python 适配层”的实现示例，不要求实际控制设备的外部程序也使用 Python。

### 1.2 非 Python 调用方

非 Python 外部程序与 Python 适配层之间只需要约定数据和返回状态，不需要理解本模块内部算法。调用链可以是：

```text
外部控制程序
    -> 发送数组、mask 和参数
Python 适配层
    -> 调用 closed_loop_calibration.py
    -> 返回数组、need_more、warning 或 error
外部控制程序
    -> 根据返回结果决定继续扫描、插值或下发
```

适配层至少需要提供以下四类操作；具体名称和通信方式由外部程序决定：

| 操作      | 输入                                                                                 | 输出                                          |
| ------- | ---------------------------------------------------------------------------------- | ------------------------------------------- |
| 阶段 1 处理 | `residual`、`positions`、`stage1_mask`、`fill_value`                                  | 完整有限的源网格测量误差 `C0`                           |
| 初始插值    | `C0`、源网格坐标、下发网格坐标                                                                  | 实际下发的 `C0_download`                         |
| 阶段 2 处理 | 累计 `residuals`、必传 `masks`、阶段 2 坐标及参数                                               | `need_more`、`deltaC`、`stage2_valid_mask`    |
| 合并和最终插值 | 初始表、`deltaC`、`stage1_mask`、`stage2_valid_mask`、X 分组数 `x_group_size`、目标坐标及目标插值 mask | `final_download_table`、`interpolation_mask` |

适配层按固定位置传参，参数顺序为：

```text
process_stage2_residuals(residuals, positions, alpha, m_min, m_max, masks)
combine_correction_tables(initial_correction, deltaC, stage1_mask, stage2_valid_mask, x_group_size)
interpolate_residual_table(residual_table, positions, target_positions, source_mask, target_mask)
```

建议适配层统一使用以下数据约定：

- 数值数组使用双精度浮点；

- mask 在适配层中转换为布尔数组，`True` 表示可用；

- 坐标和残差的最后一维长度为 2，依次为 X、Y；

- 阶段 2 的 `masks` 必须传入；`mask=False` 的残差可以传 `0`、`NaN` 或其他非有限值，`mask=True` 的残差必须是有限值；

- 阶段 2 返回的 `stage2_valid_mask` 是点级结果可用性 mask，不能通过 `deltaC` 是否为 0 或是否为有限数值推断；`need_more` 由有效点集（`stage2_valid_mask=True` 的点）是否全部达到精度要求决定，始终无效的点不阻塞收敛；

- 最终插值的目标 mask 中，`True` 表示需要插值，`False` 表示不需要插值；目标 mask 为 `False` 的输出 X/Y 分量固定为 `0.0`；

- 最终插值的源坐标必须构成轴对齐的规则矩形网格，X 沿列方向变化、Y 沿行方向变化；允许 X/Y 方向使用非均匀间距；

- 合并时的 X 分组修正（`x_group_size = N > 1`）按列序号 mod N 分组，调用方需要保证列序号与数据的 X 分组相位一致（见 5.1 节）；

- 外部程序可以使用 JSON、二进制、共享内存或其他方式传输，但必须保留数组形状、数据类型、最后一维分量顺序和各个 mask；不需要依赖 NaN 表示有效性。

返回状态建议按以下方式处理：

- 正常返回：使用返回的数组；

- `need_more=True`：继续采集阶段 2，不生成最终下发表；

- `RuntimeWarning`：记录警告并按策略继续，例如目标点超出源网格范围、双线性角点无效或达到 `m_max`；

- `ValueError`、`TypeError` 等错误：停止本次校准，不下发错误结果。

## 2. 数组形状约定

假设阶段 1 的校准源网格为 `Ny × Nx`，每个点有 X/Y 两个分量：

```text
calibration_positions.shape == (Ny, Nx, 2)
```

调用时要区分三个坐标网格：

- `calibration_positions`：阶段 1 实际测量的源网格；

- `stage2_positions`：阶段 2 残差实际对应的网格；

- `download_positions` / `final_download_positions`：设备实际接收表的目标网格。

阶段 2 的残差、`deltaC`、`stage2_valid_mask`、合并时的初始表和阶段 1 质量 mask 必须属于同一个 `stage2_positions` 网格。最简单的情况是阶段 2 就在初始下发网格上扫描，此时 `stage2_positions = download_positions`。

最后一维固定为：

```text
[..., 0] = X
[..., 1] = Y
```

mask 是布尔数组：

```text
True  = 该点本次测量可用
False = 该点本次测量不可用
```

阶段 1 和阶段 2 的 mask 形状不同：

| 数据                   | 形状                                    | 含义                                                  |
| -------------------- | ------------------------------------- | --------------------------------------------------- |
| 阶段 1 残差              | `(Ny, Nx, 2)`                         | 单次扫描的 X/Y 残差                                        |
| 阶段 1 mask            | `(Ny, Nx)`                            | 阶段 1 单次扫描的有效点                                       |
| 阶段 2 残差历史            | `(M, *stage2_grid_shape, 2)`          | 已累计的 M 次扫描                                          |
| 阶段 2 masks 历史        | `(M, *stage2_grid_shape)`             | 每次扫描各自的有效点                                          |
| 阶段 2 结果有效 mask       | `stage2_grid_shape`                   | `stage2_valid_mask`，表示对应 `deltaC` 是否至少有一个可用的 X/Y 结果 |
| 阶段 1 对应阶段 2 的质量 mask | `stage2_grid_shape`                   | `stage1_mask_on_stage2_grid`，用于最终插值                 |
| 最终目标插值 mask          | `final_download_positions.shape[:-1]` | `True` 需要插值；`False` 不插值且输出设为 0                      |

阶段 2 的 `masks[i]` 必须和 `residuals[i]` 对应，不能只传最新一次扫描的 mask。

## 3. 阶段 1：生成处理后的测量误差 `C0`，插值后再下发

以下代码是 Python 适配层示例。非 Python 外部程序只需把同样的数据传给适配层，并接收 `C0_download`；设备采集和下发接口仍由外部程序负责。

```python
calibration_positions = ...     # 阶段1测量的校准源网格，shape: (Ny, Nx, 2)
download_positions = ...        # 设备实际使用的下发网格，shape: (..., 2)
stage1_residual = ...           # shape: (Ny, Nx, 2)
stage1_mask = ...               # shape: (Ny, Nx), dtype=bool

C0_on_calibration_grid = process_first_measurement(
    stage1_residual,
    calibration_positions,
    stage1_mask,
    0.0,
)

# 实际下发的是插值后的表，不是校准源网格上的 C0。
C0_download = interpolate_residual_table(
    C0_on_calibration_grid,
    calibration_positions,
    download_positions,
)
```

`process_first_measurement` 输出的是去仿射后的测量误差 `E - A0`，不取相反数，不转换为修正方向。`mask=False` 的点不参与阶段 1 仿射拟合，源网格上对应的 `C0_on_calibration_grid` 使用 `fill_value`，默认是 `0.0`。这些填充值是阶段 2 的初始估计，因此在这一次初始插值中应让它们参与计算；不要在这里传入 `source_mask=stage1_mask`。

`C0_download` 是插值后的完整测量误差表。如果设备接口要求的是与测量误差相反方向的修正量，应由外部适配层在下发边界处按设备约定转换；本模块不在阶段 1 处理函数中隐式取反：

```python
# 这里调用外部程序自己的设备下发接口；本模块不提供该函数。
your_stage_download_api(C0_download)
```

阶段 2 的残差必须在 `C0_download` 已经生效后采集。后续传给阶段 2 的坐标，必须是残差实际对应的坐标网格；如果阶段 2 就是在下发网格上扫描，则使用 `download_positions`。

如果希望给阶段 1 无效点使用其他初始值，可以修改：

```python
C0_on_calibration_grid = process_first_measurement(
    stage1_residual,
    calibration_positions,
    stage1_mask,
    some_initial_value,
)
```

`fill_value` 必须是有限标量，并同时用于该点的 X/Y 两个误差分量。

## 4. 阶段 2：逐次扫描并累计 mask

每次阶段 2 扫描后，外部程序需要同时保存：

1. 本次残差 `residual_i`；
2. 本次有效点 mask `mask_i`。

`mask_i` 不是本模块计算出来的，而是外部测量程序根据本次扫描的设备状态、
采集状态或质量判定产生的结果。例如，缺测、超时、饱和、拟合失败、匹配质量
分数低于阈值（例如 Match Score < 0.7）或人工屏蔽
的点应设为 `False`。调用指南中的 `your_measure_stage2_mask_api()` 只是占位
名称，实际名称和实现由外部程序决定。

推荐用列表保存历史，然后每次调用时整体堆叠：

```python
residual_history = []
mask_history = []

while True:
    # 以下两个函数均是外部程序自己的采集接口，不属于本模块。
    residual_i = your_measure_residual_after_C0_api()
    mask_i = your_measure_valid_mask_api()

    residual_i = np.asarray(residual_i, dtype=float)
    mask_i = np.asarray(mask_i, dtype=bool)

    residual_history.append(residual_i)
    mask_history.append(mask_i)

    residuals = np.stack(residual_history, axis=0)
    masks = np.stack(mask_history, axis=0)
    # 如果残差是在实际下发网格上测得，这里必须传 download_positions。
    stage2_positions = download_positions

    need_more, deltaC, stage2_valid_mask = process_stage2_residuals(
        residuals,
        stage2_positions,
        0.4,
        5,
        20,
        masks,
    )

    if not need_more:
        break
```

### 阶段 2 mask 的行为

- 某点只在本次扫描无效：只排除本次观测，其他扫描仍可使用；

- 每次扫描的 mask 可以不同；

- 仿射拟合、平均值和有效样本计数都只使用对应 mask 为 True 的观测；
  不执行任何基于数值分布的时间维异常值剔除，异常观测的识别完全由
  调用方的 mask 负责；

- `stage2_valid_mask` 不是 `masks` 的简单交集或并集，而是根据 mask 过滤
  后的有效观测数计算；`point_precision_reached` 则是独立的精度停止条件：

  ```text
  minimum_point_count = max(m_min, ceil(1 / alpha²))
  有效观测数 N(x) = masks 中该点为 True 的扫描数
  stage2_valid_mask = (N(x) > 0)
  point_precision_reached = (N(x) >= minimum_point_count)
  整体收敛 = stage2_valid_mask=True 的点全部满足 point_precision_reached
  ```

- 二维非共线网格中，某次扫描如果 mask 过滤后不足 3 个不共线点，会直接报错；

- 某点达到 `m_max` 但没有达到精度要求时，只要仍有至少一个可用的 X/Y 结果，
  就保留当前 `deltaC` 并在 `stage2_valid_mask` 中标记为 `True`；只有始终没有
  有效结果的点才返回 `0.0` 并标记为 `False`，同时告警；

- `need_more=True` 时，`stage2_valid_mask` 可能已经有部分 `True`，这只表示
  这些点当前有可用结果，不表示整张表可以提前合并；必须等到 `need_more=False`。

- 扫描次数少于 `m_min` 时，仍返回形状为 `(..., 2)` 的逐点去漂移简单平均表，
  但该表仅用于日志记录；已有至少一次有效观测的点在
  `stage2_valid_mask` 中为 `True`，只有始终没有有效结果的点为 `False`；

- `need_more=True` 时不要生成最终修正表；应继续扫描。

如果 `residuals` 的扫描次数还没有达到 `m_min`，返回的 `deltaC` 只是日志用的
临时表；无有效观测值的点填为 `0.0`，只有始终没有有效结果的点在
`stage2_valid_mask` 中为 `False`。由于 `need_more=True`，不能拿它进入合并和最终
下发流程。

## 5. 合并阶段 1 和阶段 2

阶段 2 完成或达到 `m_max` 后，调用：

```python
# initial_correction 和 deltaC 必须在同一个坐标网格上。
# 下面以阶段2就在实际下发网格上扫描为例。
# x_group_size 为沿 X 方向的分组数 N：数据存在按 N 点周期出现的组间系统偏差时
# 传入 N（如 B3 9.1 数据为 3、9.2 数据为 2），否则保持默认 1。
C_final, interpolation_mask = combine_correction_tables(
    C0_download,
    deltaC,
    stage1_mask_on_stage2_grid,
    stage2_valid_mask,
    x_group_size,
)
```

如果阶段 1 源网格和阶段 2/下发网格相同，`stage1_mask_on_stage2_grid` 就是原始的 `stage1_mask`。如果网格不同，外部程序必须根据自己的坐标对应关系生成同形状的 `stage1_mask_on_stage2_grid`；不能直接把源网格上的 `stage1_mask` 传进来。

合并规则为：

```text
阶段2有效点：C_final = C0_download - deltaC
阶段2无效点：C_final = C0_download
阶段1、阶段2都无效：C_final = 0.0，interpolation_mask = False
```

因此阶段 1 无效、但阶段 2 后来有效的点也能被阶段 2 修正。

`C_final` 作为最终插值的源表；阶段 1、阶段 2 都无效的点虽然数值为 `0.0`，
但不能作为插值源点，必须同时保存 `interpolation_mask`：

```text
interpolation_mask = stage1_mask_on_stage2_grid OR stage2_valid_mask
```

其中 `stage2_valid_mask` 由阶段 2 函数返回，表示该点的 `deltaC` 至少有一个
可用的 X/Y 结果。它不等同于 `point_precision_reached`：达到 `m_max` 时，
低精度但有结果的点仍可参与合并。不能通过 `deltaC == 0` 或是否为有限数值来
推断有效性。

特别地：

```text
stage1_mask_on_stage2_grid == False 且 stage2_valid_mask == False
```

的点在 `C_final` 中为 `0.0`，没有可靠校准依据，不得作为后续插值源点。

### 5.1 X 方向分组系统偏差修正

当 `x_group_size = N > 1` 时，合并完成后对 `C_final` 追加一步组间偏差修正：

1. 把网格的最后一个网格维视为 X 列方向，列序号 mod N 相同的点为一组，共 N 组；
2. 对 X/Y 两个分量分别计算各组有效点（`interpolation_mask = True`）的平均值；
3. 以 N 个组平均值的等权平均值为目标，把每组有效点整体平移，使各组平均值都等于该目标值。

该修正用于扣除测量数据中沿 X 方向按 N 点周期出现的系统性组间偏差；无效点保持
`0.0`，不参与统计也不被平移。修正只改变各组之间的相对平移，不改变 `interpolation_mask`。

适配层需要保证列序号与数据的 X 分组相位一致。对 B3 这类每行按 X 排序、行首相位
对齐的数据，把每行排序后的点依次放入列 `0, 1, 2, ...` 即可；行尾不存在的点用
`mask=False` 补齐。若某行中途缺测导致后续点相位错位，外部程序需要自行调整列号，
保证同组数据位于相同 `列序号 mod N` 的列上。

## 6. 将最终表插值到最终下发坐标

```python
final_download_positions = ...       # shape: (..., 2)
final_target_mask = ...               # shape: (...,), dtype=bool

final_download_table = interpolate_residual_table(
    C_final,
    stage2_positions,
    final_download_positions,
    interpolation_mask,
    final_target_mask,
)
```

这里必须显式传入 `source_mask=interpolation_mask`：因为无效源点的数值是
`0.0`，插值函数无法仅根据数值区分“无效的 0”和“有效的 0”。

最后调用外部程序的下发接口：

```python
your_download_or_apply_correction_api(final_download_table)
```

因此，设备最终收到的是 `final_download_table`，而不是未经插值的 `C_final`。

插值时：

- `target_mask=False` 的目标点不执行插值，输出的 X/Y 两个分量都为 `0.0`；

- `target_mask=True` 的目标点先定位到源网格单元，使用该单元的 4 个角点做双线性插值；

- 如果角点无效，则用距离该角点最近且尚未使用的有效源点替代该角点的值，再保留原角点的双线性权重，并发出 `RuntimeWarning`；

- 目标点超出源网格范围时，使用最靠近的边界单元继续计算，允许双线性坐标超出 `[0, 1]`，从而沿边界局部趋势外插，并发出 `RuntimeWarning`；

- 只要存在一个 `target_mask=True` 的点，源表中有效源点总数少于 4 个就抛出 `ValueError`；如果所有目标点都是 `False`，不执行这个检查；

- `source_mask` 在最终插值时应传入 `interpolation_mask`；只有初始阶段 `C0` 的
  `fill_value` 是有意参与初始插值的有限值。

## 7. Python 适配层的完整调用骨架

如果外部控制程序不是 Python，可以把下面代码封装成一个独立的 Python 进程或服务；外部程序通过自己选定的通信方式传入数据，并取回结果。

```python
import numpy as np

from closed_loop_calibration import (
    combine_correction_tables,
    interpolate_residual_table,
    process_first_measurement,
    process_stage2_residuals,
)


# ---------- 阶段 1：先生成源网格 C0 ----------
# 以下 load/measure/download 函数均是外部程序自己的接口示例，
# closed_loop_calibration.py 不提供这些函数。
calibration_positions = your_load_calibration_positions()  # (Ny, Nx, 2)
download_positions = your_load_download_positions()       # (N2y, N2x, 2)
stage1_residual = your_measure_stage1_residual()          # (Ny, Nx, 2)
stage1_mask = your_load_stage1_mask().astype(bool)        # (Ny, Nx)

C0_on_calibration_grid = process_first_measurement(
    stage1_residual,
    calibration_positions,
    stage1_mask,
    0.0,
)

# 初始实际下发表：先插值，再下发。
C0_download = interpolate_residual_table(
    C0_on_calibration_grid,
    calibration_positions,
    download_positions,
)
your_stage_download_api(C0_download)

# 阶段2和 C0_download 使用同一个网格的示例。
stage2_positions = download_positions
# 如果两个网格不同，此处必须由外部程序生成对应网格上的阶段1质量 mask。
stage1_mask_on_stage2_grid = your_get_stage1_mask_on_download_grid().astype(bool)


# ---------- 阶段 2 ----------
residual_history = []
mask_history = []

while True:
    residual_i = np.asarray(
        your_measure_stage2_residual_api(), dtype=float
    )
    mask_i = np.asarray(your_measure_stage2_mask_api(), dtype=bool)

    residual_history.append(residual_i)
    mask_history.append(mask_i)

    need_more, deltaC, stage2_valid_mask = process_stage2_residuals(
        np.stack(residual_history, axis=0),
        stage2_positions,
        0.4,
        5,
        20,
        np.stack(mask_history, axis=0),
    )

    if not need_more:
        break


# ---------- 合并 ----------
# 假设本批数据的 X 方向存在按 N 点周期出现的组间系统偏差（如 B3 9.1 为 3、
# 9.2 为 2）；没有该问题时传 1 或使用默认值。
N = 3
C_final, interpolation_mask = combine_correction_tables(
    C0_download,
    deltaC,
    stage1_mask_on_stage2_grid,
    stage2_valid_mask,
    N,
)


# ---------- 最终插值/下发 ----------
final_download_positions = your_load_target_positions()  # (..., 2)
final_target_mask = your_load_target_mask().astype(bool)  # (...,)
final_download_table = interpolate_residual_table(
    C_final,
    stage2_positions,
    final_download_positions,
    interpolation_mask,
    final_target_mask,
)
your_download_or_apply_correction_api(final_download_table)
```

## 8. 最重要的调用原则

1. 非 Python 外部程序通过适配层调用本模块；通信协议可以自定，但必须保留数组形状、mask 和数据类型，不依赖 NaN 表示有效性。
2. 阶段 1 的无效点必须先填成有限值；`C0` 插值后得到的 `C0_download` 才是实际下发并用于阶段 2 的表。
3. 阶段 2 的 `residuals` 和 `masks` 必须保留完整历史，并按扫描序号对齐。
4. 阶段 2 的残差必须是在实际下发的 `C0_download` 下测得的，且传入的坐标必须与残差对应。
5. 合并时 `initial_correction`、`deltaC`、`stage1_mask_on_stage2_grid` 和 `stage2_valid_mask` 必须属于同一个网格。
6. `C_final` 是合并后的源表；设备最终收到的是对 `C_final` 再插值后的 `final_download_table`。
7. 阶段 1、阶段 2 都失效的点在最终源表中为 `0.0`，但由 `interpolation_mask=False` 标记，不能参与后续插值；最终设备表由其他有效源点插值得到。
8. 数据存在沿 X 方向按 N 点周期出现的组间系统偏差时，合并需要传 `x_group_size = N`（B3 9.1 数据为 3、9.2 数据为 2），并保证列序号与数据的分组相位一致；该修正只平移有效点，不改变 mask。


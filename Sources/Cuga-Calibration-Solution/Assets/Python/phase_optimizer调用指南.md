# `phase_optimizer.py` 调用指南

本文档对应当前版本的 [phase_optimizer.py](../phase_optimizer.py)。当前接口的状态格式为 version 5，不兼容旧版 `optimizer_state.pkl`。

## 1. 接口概览

优化器提供 `suggest()` 主接口；单调幅值转换函数也保留为可独立使用的工具：

```python
import phase_optimizer

phase_optimizer.suggest(
    cost,
    n_phase,
    n_normal,
    n_initial,
    noise,
    n_early_stop,
    acq_func,
)

phase_optimizer.monotonic_amplitude_anchors(parameters)
```

`suggest()` 只能按下面的固定顺序使用位置参数，不能使用关键字参数：

```text
suggest(cost, n_phase, n_normal, n_initial, noise, n_early_stop, acq_func)
```

首次调用至少提供前三个参数；后续调用可以从末尾省略不变的配置。若需要传入靠后的参数，前面的参数必须用 `None` 占位。

## 2. 优化变量的顺序和含义

优化器内部使用一个连续的归一化向量；内部向量的普通段记为 `p_normal`。返回时拆成相位段和已经解码的幅值锚点段：

```text
x_internal = [x_phase[0], ..., x_phase[n_phase-1],
              p_normal[0], ..., p_normal[n_normal-1]]
result["x_normal"] = monotonic_amplitude_anchors(p_normal)
```

### 相位参数

- `x_phase` 长度为 `n_phase`，每个值位于 `[0, 1]`。
- 每个值表示非基准电极在参考频率下相位差的归一化值。
- 参考频率相位差：

  ```text
  phase_ref = 2π × x_phase
  ```

- 基准电极不参与优化，相位差默认为 0。

优化器本身不保存参考频率，也不负责把相位转换为硬件延时。参考频率和硬件接口语义由外部程序处理。

### 幅值参数

优化器内部使用的幅值控制量按频率从低到高排列，并保存在 `optimizer_state.pkl` 中。`suggest()` 返回的 `x_normal` 已经转换为单调不减的幅值锚点，可以直接用于幅值曲线插值：

```python
amplitude_anchors = result["x_normal"]
```

转换关系为：

```text
w[0] = p[0]
w[i] = w[i-1] + (1-w[i-1]) × p[i]
```

因此返回的 `anchors` 满足：

```text
0 ≤ w[0] ≤ w[1] ≤ ... ≤ w[M-1] ≤ 1
```

这些是归一化幅值锚点，不是必然对应伏值或 dBm 的硬件量。若硬件使用电压、功率或 DAC 码值，仍需由外部程序完成物理量映射。

对于独立于 `suggest()` 的控制量，仍可直接调用转换函数。例如：

```python
controls = [0.25, 0.0, 0.5, 0.0, 1.0]
anchors = phase_optimizer.monotonic_amplitude_anchors(controls)
# [0.25, 0.25, 0.625, 0.625, 1.0]
```

当锚点位于均匀归一化频率位置时，可用分段线性插值得到目标频率的幅值：

```python
import numpy as np

anchor_frequency = np.linspace(0.0, 1.0, len(anchors))
frequency = (frequency_hz - f_min_hz) / (f_max_hz - f_min_hz)
amplitude = np.interp(frequency, anchor_frequency, anchors)
```

## 3. 首次调用

以 5 电极、4 个相位变量和 5 个幅值控制量为例：

```python
import phase_optimizer

n_phase = 5 - 1
n_normal = 5
n_initial = 32
noise_variance = 0.02 ** 2       # 测量 score 噪声的方差，不是标准差
n_early_stop = 100                # 连续 100 次无显著改善后停止
acq_func = "LCB"

result = phase_optimizer.suggest(
    None,
    n_phase,
    n_normal,
    n_initial,
    noise_variance,
    n_early_stop,
    acq_func,
)
```

首次调用的 `cost` 必须是 `None`。函数会创建当前工作目录下的 `optimizer_state.pkl`，并返回第一个待测点。

## 4. 标准测量循环

外部程序的基本流程是“取得候选参数 → 下发硬件 → 测量 → 反馈 cost”：

```python
import numpy as np
import phase_optimizer

n_electrodes = 5
n_phase = n_electrodes - 1
n_normal = 5
n_initial = 32
noise_variance = 0.02 ** 2
n_early_stop = 100

result = phase_optimizer.suggest(
    None, n_phase, n_normal, n_initial, noise_variance, n_early_stop, "LCB"
)

while not result["done"]:
    # 1. 读取优化器返回的相位归一化参数和可直接插值的幅值锚点。
    x_phase = np.asarray(result["x_phase"], dtype=float)
    amplitude_anchors = np.asarray(
        result["x_normal"], dtype=float
    )

    # 2. 按硬件接口定义，把 x_phase / amplitude_anchors 解码为实际命令。
    hardware_parameters = decode_for_hardware(
        x_phase, amplitude_anchors
    )

    # 3. 下发并测量。score 越大越好，而 suggest() 约定 cost 越小越好。
    score = measure_score(hardware_parameters)
    cost = -float(score)

    # 后续位置参数仍按同一顺序传入；不变的首次配置用 None 占位。
    result = phase_optimizer.suggest(
        cost, n_phase, n_normal, None, None, n_early_stop, "LCB"
    )

best_x_phase = np.asarray(result["best_x_phase"], dtype=float)
best_amplitude_anchors = np.asarray(
    result["best_x_normal"], dtype=float
)
best_score = -float(result["best_cost"])
```

注意：`n_initial` 只在首次创建优化器时生效；`noise` 和采集函数配置也应保持首次调用时的值。后续调用若不需要重复传配置，可以直接写成：

```python
result = phase_optimizer.suggest(cost)
```

如果进程在硬件测量期间重启，使用 `cost=None` 会重新返回状态中尚未完成的同一个待测点，不会跳过该次测量：

```python
result = phase_optimizer.suggest(None)
```

## 5. 相位差和延时的换算

假设：

- `f_ref_hz` 是参考频率，单位为 Hz；
- `x_phase[j]` 是第 `j+1` 个电极相对基准电极的归一化参考相位差；
- 硬件采用固定延时语义。

先计算参考频率相位和相对延时：

```python
phase_ref = 2.0 * np.pi * x_phase
delay = x_phase / f_ref_hz
```

在目标频率 `frequency_hz` 下，第 `j+1` 个电极的相位差为：

```python
phase_at_frequency = 2.0 * np.pi * frequency_hz * delay
```

等价地：

```python
phase_at_frequency = phase_ref * frequency_hz / f_ref_hz
```

如果硬件接口要求相位范围为 `[0, 2π)`，只在最终下发前进行包络：

```python
phase_command = np.mod(phase_at_frequency, 2.0 * np.pi)
```

不要把这个包络操作用于优化器输入变量本身；优化器返回的 `x_phase` 应原样参与本次候选参数对应的测量反馈。

## 6. 返回值说明

| 字段 | 含义 |
|---|---|
| `x_phase` | 当前待测点的相位归一化参数，长度为 `n_phase` |
| `x_normal` | 当前待测点已转换的单调不减幅值锚点，长度为 `n_normal`，可直接用于插值 |
| `best_x_phase` | 当前历史最优点的相位参数；尚无测量时为 `None` |
| `best_x_normal` | 当前历史最优点已转换的幅值锚点；尚无测量时为 `None` |
| `best_cost` | 历史最小 cost；尚无测量时为 `None` |
| `done` | 是否已满足早停条件 |

由于约定 `cost = -score`：

```text
best_score = -best_cost
```

当前接口不再返回 `x_periodic`、`best_x_periodic` 或合并后的 `x` 字段。

## 7. 状态文件和重新开始

- 状态文件名为 `optimizer_state.pkl`，默认位于调用程序的当前工作目录。
- 同一个状态文件只能对应一组固定的 `n_phase` 和 `n_normal`。
- 更换参数数量、优化器算法、评分定义或变量语义时，应使用新的工作目录或新的状态文件，从首次调用重新开始。
- 当前版本不兼容旧版状态文件；不要将旧状态直接交给当前 `phase_optimizer.py` 恢复。
- `suggest()` 不设置总测量次数上限；正式仿真使用 `n_early_stop=100`。如需调试时限制次数，应由外部循环自行中止，并将该结果标记为短预算结果。

## 8. 常见错误

### 关键字传参

错误：

```python
phase_optimizer.suggest(None, n_phase=4, n_normal=5)
```

正确：

```python
phase_optimizer.suggest(None, 4, 5)
```

### 把标准差当成 `noise`

`noise` 要传方差。如果重复测量估计出的 score 噪声标准差为 `sigma`，应传：

```python
noise_variance = sigma ** 2
```

### 把 score 直接传给 `suggest()`

`suggest()` 按 cost 越小越好处理。如果业务评分是越大越好，应传入负值：

```python
cost = -score
```

### 跳过 `cost=None` 的待测点

调用 `suggest(None)` 表示“当前待测点还没有测量结果”，函数会返回状态中保存的同一点。只有拿到该点的测量结果后，才应将对应的 `cost` 传回。

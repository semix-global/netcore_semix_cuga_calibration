# phase_optimizer.py 调用指南

本文档对应同目录的 [phase_optimizer.py](phase_optimizer.py). 当前幅值模型使用 4 个原始归一化参数, 由 `single_sigmoid_amplitude_curve()` 生成完整测量频段的幅值数组.

## 1. 接口与参数

`suggest()` 只接受位置参数, 顺序如下:

```python
suggest(cost, n_phase, n_normal, n_initial, noise, n_early_stop, acq_func)
single_sigmoid_amplitude_curve(normal_parameters, frequencies)
```

- `cost`: 上一次测量的代价. 首次调用或恢复尚未测量的点时传 `None`. score 越大越好时传 `-score`.
- `n_phase`: 非基准电极的延时变量数量, 当前 C# 调用为电极数量减 1.
- `n_normal`: 单 Sigmoid 幅值模型固定传 `4`.
- `n_initial`: 初始采样点数. 传 `None` 时按总维度的 2 倍向上取 2 的幂.
- `noise`: score 的测量噪声方差, 不是标准差. 默认值为 `1e-4`.
- `n_early_stop`: 连续无显著改善的早停步数. 默认 `"auto"` 为总维度的 20 倍, `None` 禁用早停.
- `acq_func`: 采集函数, 默认 `"LCB"`, 也可使用 `"EI"` 或 `"PI"`.

后续调用可以从末尾省略不变的配置. 如需传靠后的参数, 前面的可省略参数用 `None` 占位. 显式传入的参数数量, noise 和早停配置必须与首次调用一致.

## 2. 返回值

| 字段 | 含义 |
| --- | --- |
| `x_phase` | 当前待测点的原始归一化延时参数, 长度为 `n_phase` |
| `x_normal` | 当前待测点的 4 个原始归一化幅值参数 |
| `best_x_phase` | 历史最优点的原始归一化延时参数, 尚无测量时为 `None` |
| `best_x_normal` | 历史最优点的 4 个原始归一化幅值参数, 尚无测量时为 `None` |
| `best_cost` | 历史最小代价, 尚无测量时为 `None` |
| `done` | 是否已经满足早停条件 |

当前接口使用 `x_phase` 和 `best_x_phase`. 旧调用中的 `x_periodic` 和 `best_x_periodic` 应替换为这两个字段.

`x_normal` 和 `best_x_normal` 都是原始搜索参数, 不再是幅值锚点, 不能直接插值或下发硬件.

## 3. 幅值曲线与外部 scale

4 个参数依次为 `[u_A_low, u_A_high, u_center, u_width]`, 均位于 `[0, 1]`. 解码关系如下:

```text
A_low = u_A_low
A_high = A_low + (1 - A_low) * u_A_high
B = max(frequencies) - min(frequencies)
C = min(frequencies) + B * u_center
W = B * 0.01 * 100**u_width
A(f) = A_low + (A_high - A_low) * sigmoid(2 * ln(9) * (f - C) / W)
```

`frequencies` 必须是本次任务的完整测量频率数组. C 和 W 与输入频率使用相同单位. W 表示幅值变化的 10%-90% 过渡宽度, 范围为频段跨度的 1%-100%.

函数返回与输入频率顺序一致的一维 NumPy 数组, 幅值范围为 `[0, 1]`. 支持乱序和重复频率. 空数组返回空数组, 非空数组至少需要两个不同的有限频率且跨度有限.

```python
normalized_amplitudes = phase_optimizer.single_sigmoid_amplitude_curve(
    result["x_normal"], measured_frequencies
)
amplitudes = normalized_amplitudes * scale
```

scale 由外部程序处理, 可以是统一缩放值或与频率一一对应的数组. 不要再次进行锚点插值, 不要重复乘 scale. 同一任务应保持完整频段和 scale 不变, 传入子频段会改变曲线的解码结果.

## 4. 标准测量循环与最佳参数

以下示例中的 `measured_frequencies`, `scale`, `decode_delays_for_hardware`, `measure_score` 和 `apply_best_parameters` 由外部程序提供.

```python
import phase_optimizer

n_electrodes = 5
n_phase = n_electrodes - 1
n_initial = 32
noise_variance = 0.02 ** 2
n_early_stop = 100

result = phase_optimizer.suggest(
    None, n_phase, 4, n_initial, noise_variance, n_early_stop, "LCB"
)

while not result["done"]:
    delays = decode_delays_for_hardware(result["x_phase"])
    amplitudes = phase_optimizer.single_sigmoid_amplitude_curve(
        result["x_normal"], measured_frequencies
    ) * scale
    score = measure_score(delays, measured_frequencies, amplitudes)
    result = phase_optimizer.suggest(-float(score), n_phase, 4)

best_delays = decode_delays_for_hardware(result["best_x_phase"])
best_amplitudes = phase_optimizer.single_sigmoid_amplitude_curve(
    result["best_x_normal"], measured_frequencies
) * scale
best_score = -float(result["best_cost"])
apply_best_parameters(best_delays, measured_frequencies, best_amplitudes)
```

若外部循环在早停前自行中止, 应先反馈最后一次测量的 cost, 再读取最佳参数. 尚无有效测量时, 不应解码为 `None` 的最佳参数.

## 5. 当前 C# 调用方式

Chirp 和 Prescan 共用 `AbstractAODWaveformElectrodeDelayWindowViewModel` 的调用逻辑:

- `suggest()` 的幅值参数数量直接写死为 `4`. 界面和缓存中的 `AlgorithmUniformityAnchorCount` 保持原样, 不再参与此调用.
- 完整频率数组来自 `Cache.AODWaveformElectrodeDelayFrequencies`, 单位为 MHz.
- 当前点读取 `x_normal`, 调用 `single_sigmoid_amplitude_curve()` 生成幅值. `done` 为真时仍完成同样的参数读取和幅值生成, 再由外层循环结束.
- NumPy 返回值通过 `tolist()` 转换后读取. 测量更新函数在原有幅值计算位置, 将归一化曲线值乘对应频点的 `Amplitude` 作为外部 scale.
- 测量流程按频率索引直接下发缩放后的幅值. 噪声测量仍使用配置的频点幅值.
- `x_phase` 先乘 `AlgorithmMaxDelay`, 再按现有电极顺序累加并叠加板卡延时. 基准电极延时为 `0`, 延时单位为 ns.
- 每轮测量的 `finally` 从本地有效测量记录中选取最高 Score 项, 将该项的延时和已下发幅值写入电极结果配置. 优化完成后保留该结果, 供后续波形生成使用.

Python 文件作为嵌入资源读取, 资源名为 `CugaCalibration.Assets.Python.phase_optimizer.py`. 当前源码已经包含新曲线函数, 部署时需要使用包含该资源及新 C# 调用逻辑的程序版本.

## 6. 状态与重新开始

Python 默认将状态保存在当前工作目录的 `optimizer_state.pkl`. 当前 C# 调用通过 `_STATE_FILE` 指定为应用目录下 `Python/<调用类型名称>/optimizer_state.pkl`.

- 由旧锚点插值模型迁移到单 Sigmoid 模型时, 必须重新开始优化. 不要用新曲线解释旧模型的历史测量.
- 更换变量数量, 延时范围, 测量频段, scale 或评分定义时, 应使用新的优化状态.
- 当前 C# 重置流程会备份原状态文件, 清空待反馈 cost 和测量记录.
- `suggest(None)` 会恢复同一个尚未测量的候选点. 只有获得对应测量结果后才反馈 cost, 避免重复记录或错误配对.
- 已完成的状态可以重复查询, 不会再次记录同一次测量.
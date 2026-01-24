# Chirp AOD 波形训练算法

## 概述
本文档描述了 Chirp AOD 波形补偿系数 (P3 到 P8) 的重构训练流程。目标是通过迭代优化这些系数以最大化 Y Strehl Ratio (Y方向斯特列尔比)。

## 参数
以下参数可在界面中配置：
- **步长 P 系数 (`StepPCoefficient`)**: 用于探索系数变化的步长（例如 +/- 0.01）。默认值：`0.01`。
- **最大循环次数 (`RetryTimes`)**: 执行外部迭代的次数。默认值：`10`。

## 算法流程

### 1. 初始化
1.  **重置系数**: 将所有补偿系数 (P3, P4, P5, P6, P7, P8) 设置为 `0`。
2.  **基准测量**:
    - 在所有系数为 0 的情况下运行 `CatchImagesAsync`。
    - **添加记录**: 将基准测量结果添加到 `Cache.Items` 列表中。
    - **设定基准**: 将该结果记录为初始的 **当前最佳项 (Current Best Item)**。

### 2. 优化循环
算法执行一个 **外层循环**，重复 `RetryTimes` 次。在此循环内部，它依次优化从 P3 到 P8 的每个系数。

**外层循环** (`i` 从 1 到 `RetryTimes`):
  - **内层循环** (遍历系数 `P`，顺序为 {P3, P4, P5, P6, P7, P8}):
    1.  **确定当前状态**: 设 `CurrentCoeff` 为 **当前最佳项** 中系数 `P` 的当前值。
    2.  **测试变体**:
        - **变体 A (减)**: 计算 `ValMinus = CurrentCoeff - StepPCoefficient`。
            - 运行 `CatchImagesAsync`。
            - **添加记录**: 将结果 `ItemMinus` 添加到 `Cache.Items`。
        - **变体 B (加)**: 计算 `ValPlus = CurrentCoeff + StepPCoefficient`。
            - 运行 `CatchImagesAsync`。
            - **添加记录**: 将结果 `ItemPlus` 添加到 `Cache.Items`。
    3.  **评估与更新**:
        - 比较 `ItemMinus`、`ItemPlus` 和 `当前最佳项` 的 `BestYStrehlRatio.Y`。
        - **条件**: 如果 `ItemMinus` 或 `ItemPlus` 的比率优于（高于）`当前最佳项`：
            - **修改基准**: 将 **当前最佳项** 更新为比率最高的那一项。
            - **立即应用**: 立即更新当前系数 `P` 的最佳值，用于后续计算。
        - **条件**: 如果两者都不更好：
            - 保持 `当前最佳项` 不变。
            - 继续处理下一个系数。

### 3. 完成
- 该过程对所有系数 (P3->P8) 重复指定的循环次数。
- 最终的 **当前最佳项** 包含优化后的系数。

## 数据结构
- **ChirpAODWaveformTrainingCache**:
  - `StepPCoefficient` (double, 步长)
  - `RetryTimes` (int, 最大循环次数)
  - `Items` (结果列表)
- **ChirpAODWaveformTrainingItem**:
  - 存储该次测量使用的 `P3`...`P8` 系数。
  - 存储结果指标 (`BestYStrehlRatio` 等)。

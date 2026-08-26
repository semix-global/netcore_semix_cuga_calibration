"""闭环校准的首次测量与阶段2多次扫描残差处理。

首次测量使用 :func:`process_first_measurement` 生成粗修正表 ``C0``；下发
``C0`` 后，调用方在每次阶段2扫描后，把截至当前累计的二维残差和对应的
期望位置传给 :func:`process_stage2_residuals`。

数组最后一维统一使用 ``[..., 0] = X``、``[..., 1] = Y``。函数本身不保存
历史状态，因此调用方需要保留每次扫描结果，并在下一次调用时传入完整历史。
"""

from __future__ import annotations

import warnings

import numpy as np


def process_first_measurement(
    residual: np.ndarray,
    desired_positions: np.ndarray,
) -> np.ndarray:
    """处理阶段1的首次测量，生成粗修正表 ``C0``。

    参数:
        residual: 第一次测量得到的二维误差场
            ``E = P_measured - P_desired``，形状为 ``(..., 2)``。最后一维的
            ``[..., 0]``、``[..., 1]`` 依次为 X、Y 残差，前置维度是测量网格。
        desired_positions: 网格点的期望二维坐标，形状为 ``(..., 2)``，必须
            与 ``residual`` 完全相同。最后一维的 ``[..., 0]``、``[..., 1]``
            依次为 X、Y 坐标。

    返回:
        粗修正表 ``C0``，形状与 ``residual`` 相同。二维非共线网格按
        ``C0 = -(E - A0)`` 扣除完整 6 参数仿射场；单行等共线点集只分别
        扣除 X、Y 残差均值，再取相反数。

    说明:
        返回值可以直接作为阶段1修正表下发。阶段2函数的输入仍应是下发
        ``C0`` 后重新测得的残差，而不是本函数返回的修正表。
    """
    # 转为浮点数组，保证后续最小二乘及减法不会发生整数截断；不修改输入。
    values = np.asarray(residual, dtype=float)
    positions = np.asarray(desired_positions, dtype=float)

    # 阶段1没有扫描序号维，数组布局为 (网格维度..., 2)；最后一维的
    # [..., 0]、[..., 1] 分别是 X、Y 分量。
    if values.ndim < 2 or values.shape[-1] != 2:
        raise ValueError("residual 的形状必须为 (网格..., 2)")
    if any(size == 0 for size in values.shape[:-1]):
        raise ValueError("residual 的网格维不能为空")
    if not np.all(np.isfinite(values)):
        raise ValueError("residual 只能包含有限数值")
    if positions.shape != values.shape:
        raise ValueError("desired_positions 的形状必须与 residual 相同")
    if not np.all(np.isfinite(positions)):
        raise ValueError("desired_positions 只能包含有限数值")

    # [x, y, 1] 同时拟合 X、Y 残差，满秩时得到线性变换和平移共 6 个参数。
    flat_positions = positions.reshape(-1, 2)
    affine_design = np.column_stack(
        (flat_positions, np.ones(flat_positions.shape[0], dtype=float))
    )

    if np.linalg.matrix_rank(affine_design) == 3:
        flat_values = values.reshape(-1, 2)
        affine_parameters, _, _, _ = np.linalg.lstsq(
            affine_design, flat_values, rcond=None
        )
        affine_field = (affine_design @ affine_parameters).reshape(values.shape)
        residual_without_drift = values - affine_field
    else:
        # 单行数据无法区分沿行线性趋势和真实 stage map 误差，因此仅去平移。
        component_mean = np.mean(values.reshape(-1, 2), axis=0)
        residual_without_drift = values - component_mean

    # 修正方向与测量误差相反：控制器下发 C0 后用于抵消首次测得的误差场。
    return -residual_without_drift


def interpolate_residual_table(
    residual_table: np.ndarray,
    desired_positions: np.ndarray,
    target_positions: np.ndarray,
) -> np.ndarray:
    """将二维残差表按期望坐标双线性插值到另一组坐标网格。

    ``residual_table`` 和 ``desired_positions`` 共同定义源网格：前者给出
    每个源网格节点的 X/Y 残差，后者给出这些节点的期望 X/Y 坐标。函数不
    修改任何输入数组；输入会转为浮点数组后参与计算。

    参数:
        residual_table: 源残差表，形状为 ``(N_y, N_x, 2)``。最后一维的
            ``[..., 0]``、``[..., 1]`` 依次为 X、Y 残差。
        desired_positions: 源网格点的期望二维坐标，形状必须为
            ``(N_y, N_x, 2)``。最后一维的 ``[..., 0]``、``[..., 1]`` 依次
            为 X、Y 坐标；前两个维度组成轴对齐的二维规则网格，其中行方向
            对应 Y、列方向对应 X。两个坐标轴可以非均匀、升序或降序，但不能
            包含重复或交错的坐标。这里的“轴对齐”表示，在通常布局下有
            ``desired_positions[i, j] == [x[j], y[i]]``；不支持弯曲网格或散点。
        target_positions: 需要查询的期望二维坐标网格，形状为
            ``(..., 2)``。最后一维的 ``[..., 0]``、``[..., 1]`` 依次为待
            查询点的 X、Y 坐标；其前置维度可以与源网格不同，也不要求本身是
            规则网格。

    返回:
        形状与 ``target_positions`` 相同的双线性插值残差表。目标点落在
        源网格范围内时，使用其所在源网格单元的四个节点进行双线性插值。
        目标点落在源网格范围外时，使用对应边界侧相邻的第一个或最后一个
        单元进行线性外插，并发出一次 ``RuntimeWarning``；函数不会静默地把
        外部点裁剪为边界值。

    插值公式（对 X/Y 两个残差分量分别计算）为：对单元格四角
    ``R00 = R(x0, y0)``、``R01 = R(x1, y0)``、``R10 = R(x0, y1)``、
    ``R11 = R(x1, y1)``，令

    ``tx = (x - x0) / (x1 - x0)``，``ty = (y - y0) / (y1 - y0)``，则

    ``R(x, y) = (1-tx)(1-ty)R00 + tx(1-ty)R01 + (1-tx)ty R10 + tx ty R11``。

    说明:
        如果传入的源坐标网格按 ``positions[i, j] = [x_i, y_j]`` 排列，
        函数会自动识别并转置处理。源坐标轴为降序时，函数也会先翻转内部
        残差表和坐标轴，再进行同样的插值计算。
    """
    # 转为浮点数组，避免整数输入在插值计算中发生截断；不修改调用方数据。
    values = np.asarray(residual_table, dtype=float)
    positions = np.asarray(desired_positions, dtype=float)
    targets = np.asarray(target_positions, dtype=float)

    # 源表必须是“二维网格 + 最后一维两个残差分量”；目标点则可有任意数量
    # 的前置维度，只要求最后一维保存 [X, Y] 两个坐标分量。
    if values.ndim != 3 or values.shape[-1] != 2:
        raise ValueError("residual_table 的形状必须为 (网格行, 网格列, 2)")
    if any(size == 0 for size in values.shape[:-1]):
        raise ValueError("residual_table 的网格维不能为空")
    if not np.all(np.isfinite(values)):
        raise ValueError("residual_table 只能包含有限数值")
    if positions.shape != values.shape:
        raise ValueError("desired_positions 的形状必须与 residual_table 相同")
    if not np.all(np.isfinite(positions)):
        raise ValueError("desired_positions 只能包含有限数值")
    if targets.ndim < 1 or targets.shape[-1] != 2:
        raise ValueError("target_positions 的形状必须为 (..., 2)")
    if any(size == 0 for size in targets.shape[:-1]):
        raise ValueError("target_positions 的网格维不能为空")
    if not np.all(np.isfinite(targets)):
        raise ValueError("target_positions 只能包含有限数值")
    if values.shape[0] < 2 or values.shape[1] < 2:
        raise ValueError("双线性插值要求源网格的行数和列数都至少为 2")

    # 通常的布局是 positions[row, column] = [x[column], y[row]]：
    # X 坐标沿列方向变化，Y 坐标沿行方向变化。这里用 allclose 而不是精确
    # 相等，以兼容 meshgrid 或计算坐标时产生的浮点舍入误差。
    x_by_column = positions[0, :, 0]
    y_by_row = positions[:, 0, 1]
    canonical_layout = (
        np.allclose(
            positions[:, :, 0],
            x_by_column[None, :],
            rtol=1e-10,
            atol=1e-12,
        )
        and np.allclose(
            positions[:, :, 1],
            y_by_row[:, None],
            rtol=1e-10,
            atol=1e-12,
        )
    )

    # 也兼容 positions[row, column] = [x[row], y[column]] 的布局；这种布局
    # 常见于 meshgrid(..., indexing="ij")。转置后即可统一为上面的布局。
    x_by_row = positions[:, 0, 0]
    y_by_column = positions[0, :, 1]
    transposed_layout = (
        np.allclose(
            positions[:, :, 0],
            x_by_row[:, None],
            rtol=1e-10,
            atol=1e-12,
        )
        and np.allclose(
            positions[:, :, 1],
            y_by_column[None, :],
            rtol=1e-10,
            atol=1e-12,
        )
    )

    if canonical_layout:
        grid_values = values
        x_axis = x_by_column
        y_axis = y_by_row
    elif transposed_layout:
        grid_values = np.transpose(values, (1, 0, 2))
        x_axis = x_by_row.copy()
        y_axis = y_by_column.copy()
    else:
        raise ValueError(
            "desired_positions 必须表示轴对齐的二维规则网格；"
            "X 坐标应沿一个网格轴变化，Y 坐标应沿另一个网格轴变化"
        )

    x_axis = np.asarray(x_axis, dtype=float)
    y_axis = np.asarray(y_axis, dtype=float)

    # 允许非均匀网格，但每个坐标轴必须严格单调；重复或交错坐标无法唯一
    # 确定目标点所在的源网格单元。
    x_differences = np.diff(x_axis)
    y_differences = np.diff(y_axis)
    x_increasing = bool(np.all(x_differences > 0))
    x_decreasing = bool(np.all(x_differences < 0))
    y_increasing = bool(np.all(y_differences > 0))
    y_decreasing = bool(np.all(y_differences < 0))
    if not (x_increasing or x_decreasing):
        raise ValueError("desired_positions 的 X 坐标轴必须严格单调")
    if not (y_increasing or y_decreasing):
        raise ValueError("desired_positions 的 Y 坐标轴必须严格单调")

    # 统一为升序轴；同步翻转残差表，后续 searchsorted 才能处理两种方向。
    # 这只是内部视图/重排，不会改变调用方传入的 residual_table。
    if x_decreasing:
        x_axis = x_axis[::-1]
        grid_values = grid_values[:, ::-1, :]
    if y_decreasing:
        y_axis = y_axis[::-1]
        grid_values = grid_values[::-1, :, :]

    # 将任意形状的目标网格展平成查询点列表；最后恢复为原目标形状。
    flat_targets = targets.reshape(-1, 2)
    target_x = flat_targets[:, 0]
    target_y = flat_targets[:, 1]
    outside_x = (target_x < x_axis[0]) | (target_x > x_axis[-1])
    outside_y = (target_y < y_axis[0]) | (target_y > y_axis[-1])
    if np.any(outside_x | outside_y):
        warnings.warn(
            "target_positions 中至少有一个点位于源网格范围外，将执行线性外插。",
            RuntimeWarning,
            stacklevel=2,
        )

    # searchsorted 找到满足 axis[index] <= target < axis[index + 1] 的单元格。
    # 对范围外的点，夹住的是“单元格索引”而不是插值系数，因此保留 tx/ty
    # 小于 0 或大于 1，得到真正的线性外插而不是边界值复制。
    x_index = np.searchsorted(x_axis, target_x, side="right") - 1
    y_index = np.searchsorted(y_axis, target_y, side="right") - 1
    x_index = np.clip(x_index, 0, x_axis.size - 2)
    y_index = np.clip(y_index, 0, y_axis.size - 2)

    x0 = x_axis[x_index]
    x1 = x_axis[x_index + 1]
    y0 = y_axis[y_index]
    y1 = y_axis[y_index + 1]
    tx = (target_x - x0) / (x1 - x0)
    ty = (target_y - y0) / (y1 - y0)

    # 取每个目标点所在单元格的四个角点。此时 grid_values 已统一为
    # grid_values[行(y), 列(x), 分量]，因此四个角分别对应下左、下右、上左、上右。
    lower_left = grid_values[y_index, x_index]
    lower_right = grid_values[y_index, x_index + 1]
    upper_left = grid_values[y_index + 1, x_index]
    upper_right = grid_values[y_index + 1, x_index + 1]
    # 每个权重都是一个目标点对应一个标量；[:, None] 用于同时作用于最后
    # 一维的 X/Y 两个残差分量。
    interpolated = (
        ((1.0 - tx) * (1.0 - ty))[:, None] * lower_left
        + (tx * (1.0 - ty))[:, None] * lower_right
        + ((1.0 - tx) * ty)[:, None] * upper_left
        + (tx * ty)[:, None] * upper_right
    )
    # 恢复目标坐标网格的原始形状，并保持最后一维为 [X, Y] 残差。
    return interpolated.reshape(targets.shape)


def process_stage2_residuals(
    residuals: np.ndarray,
    desired_positions: np.ndarray,
    alpha: float = 0.3,
    m_min: int = 5,
    m_max: int = 20,
) -> tuple[bool, np.ndarray | None]:
    """判断阶段2是否需要再次测量，并计算残余重复信号表。

    参数:
        residuals: 累计二维残差数组，形状为 ``(M, ..., 2)``。其中 ``M`` 是
            最前面的扫描序号维，最后一维的 ``[..., 0]``、``[..., 1]`` 依次为
            X、Y 残差，前置网格维度在每次扫描中必须相同，且值均为有限数。
        desired_positions: 网格点的期望二维坐标，形状为 ``(..., 2)``，必须
            与单次残差的形状完全相同。最后一维的 ``[..., 0]``、``[..., 1]``
            依次为 X、Y 坐标。二维非共线网格执行完整仿射拟合；单行等共线
            点集只扣除 X、Y 分量均值。
        alpha: 停止比例。每个网格点保留的有效测量数须达到
            ``ceil(1 / alpha**2)``。
        m_min: 执行 MAD 剔除和计算残差表前所需的最少扫描次数。
        m_max: 最大扫描次数。达到该次数后，即使未满足精度要求也停止。

    返回:
        ``(need_more_measurement, residual_table)``：

        - ``need_more_measurement`` 为 ``True`` 时，调用方应再次测量。
        - ``residual_table`` 是方案中的残余重复信号 ``delta C``；扫描次数少于
          ``m_min`` 时为 ``None``，否则为形状 ``(..., 2)`` 的数组。

    说明:
        对二维非共线网格，每次扫描先用期望坐标最小二乘拟合并扣除完整二维
        仿射漂移场（线性变换和平移共 6 个参数）；对于单行等共线点集，为
        避免误删真实的沿行线性误差，只分别扣除 X、Y 残差均值。达到
        ``m_min`` 后，对每个网格点的 X、Y 分量分别跨扫描执行 3 倍稳健标准差
        的 MAD 异常值剔除，再对保留值取算术均值。达到 ``m_max`` 仍未满足
        停止条件时，会返回当前结果并发出 ``RuntimeWarning``。
    """
    # 统一转换为浮点数组：既允许调用方传入 list，也避免整数输入在减去
    # 仿射拟合结果时发生截断。这里只创建数组视图或副本，不修改调用方数据。
    values = np.asarray(residuals, dtype=float)
    positions = np.asarray(desired_positions, dtype=float)

    # 数据布局约定：
    #   values.shape    == (M, 网格维度..., 2)
    #   positions.shape == (网格维度..., 2)
    # 其中 M 是扫描序号维；最后一维的 [..., 0]、[..., 1] 分别代表 X、Y。
    # 前置网格可以是一维点列，也可以是二维网格。
    if values.ndim < 3 or values.shape[-1] != 2:
        raise ValueError("residuals 的形状必须为 (扫描次数, 网格..., 2)")
    if values.shape[0] == 0 or any(size == 0 for size in values.shape[1:-1]):
        raise ValueError("residuals 的扫描维和网格维均不能为空")
    if not np.all(np.isfinite(values)):
        raise ValueError("residuals 只能包含有限数值")
    if positions.shape != values.shape[1:]:
        raise ValueError("desired_positions 的形状必须与单次残差的形状相同")
    if not np.all(np.isfinite(positions)):
        raise ValueError("desired_positions 只能包含有限数值")
    if not np.isfinite(alpha) or not 0 < alpha <= 1:
        raise ValueError("alpha 必须是 (0, 1] 范围内的有限数")
    if isinstance(m_min, (bool, np.bool_)) or not isinstance(m_min, (int, np.integer)):
        raise TypeError("m_min 必须是整数")
    if isinstance(m_max, (bool, np.bool_)) or not isinstance(m_max, (int, np.integer)):
        raise TypeError("m_max 必须是整数")
    if m_min < 2:
        raise ValueError("m_min 必须至少为 2")
    if m_max < m_min:
        raise ValueError("m_max 必须大于或等于 m_min")

    scan_count = values.shape[0]
    if scan_count > m_max:
        raise ValueError("residuals 的扫描次数不能超过 m_max")

    # 将任意形状的网格展平成点列，并构造仿射最小二乘的设计矩阵：
    #
    #     [rx]   [x  y  1] [a11  a21]
    #     [ry] = [x  y  1] [a12  a22]
    #                       [tx   ty ]
    #
    # 矩阵的两列输出同时拟合 X、Y 残差，共得到 6 个仿射参数。
    flat_positions = positions.reshape(-1, 2)
    affine_design = np.column_stack(
        (flat_positions, np.ones(flat_positions.shape[0], dtype=float))
    )
    # 满列秩表示网格至少包含三个不共线点，可以唯一拟合完整二维仿射。
    # 单行或其他共线点集会秩亏；此时不强行拟合沿行线性趋势，而只扣均值，
    # 避免把可能属于真实 stage map 的线性系统误差一并删除。
    use_full_affine = np.linalg.matrix_rank(affine_design) == 3

    # 方案规定累计达到 m_min 后才估计统计量。在此之前明确要求继续测量，
    # 并用 None 表示尚未形成可用的 delta C 表。
    if scan_count < m_min:
        return True, None

    # 二维网格：每次扫描独立拟合仿射漂移 A_i(x)，计算 r_i(x) - A_i(x)。
    # 共线点集：每次扫描的 X、Y 分量分别扣除全部网格点的算术均值。
    drift_removed = np.empty_like(values)
    for scan_index, scan in enumerate(values):
        if use_full_affine:
            flat_scan = scan.reshape(-1, 2)
            affine_parameters, _, _, _ = np.linalg.lstsq(
                affine_design, flat_scan, rcond=None
            )
            affine_field = (affine_design @ affine_parameters).reshape(scan.shape)
            drift_removed[scan_index] = scan - affine_field
        else:
            component_mean = np.mean(scan.reshape(-1, 2), axis=0)
            drift_removed[scan_index] = scan - component_mean

    # 以下统计均沿最前面的扫描序号维进行，网格位置及最后一维的 X/Y 分量
    # 保持不变。
    # 对每个“网格点 + 分量”计算跨扫描中位数和 MAD：
    #     robust_sigma = 1.4826 * median(|r_i - median(r_i)|)
    point_median = np.median(drift_removed, axis=0)
    absolute_deviation = np.abs(drift_removed - point_median)
    mad = np.median(absolute_deviation, axis=0)
    robust_sigma = 1.4826 * mad

    # 理论上完全相同的数据在浮点仿射运算后可能产生约 1e-15 量级的差异。
    # 这个仅与机器精度和数据量级相关的容差可避免 MAD=0 时把舍入误差误判
    # 为异常点；它远小于正常测量噪声，不改变实际的 3 sigma 判据。
    numerical_tolerance = (
        64.0
        * np.finfo(float).eps
        * np.maximum(1.0, np.max(np.abs(values), axis=0))
    )
    # good 与 drift_removed 形状相同；True 表示该次扫描在该点、该分量有效。
    good = absolute_deviation <= 3.0 * robust_sigma + numerical_tolerance

    # 异常值权重视为 0，剩余有效值取算术平均，得到方案中的 delta C。
    # np.divide 的 where 防止意外出现 0 个有效值时触发除零；对应位置保留 NaN。
    good_count = np.sum(good, axis=0)
    good_sum = np.sum(np.where(good, drift_removed, 0.0), axis=0)
    residual_table = np.divide(
        good_sum,
        good_count,
        out=np.full(point_median.shape, np.nan, dtype=float),
        where=good_count > 0,
    )

    # 由 sigma_deltaC = sigma / sqrt(N) <= alpha * sigma，可得有效次数
    # N >= 1 / alpha**2。向上取整保证实际有效次数不会低于理论要求。
    required_count = int(np.ceil(1.0 / alpha**2))
    # 必须所有网格点的 X、Y 分量都达标，才算阶段2整体收敛。
    precision_reached = bool(np.all(good_count >= required_count))

    if precision_reached:
        return False, residual_table
    # 尚未达到精度且仍有扫描额度时，返回当前估计表供调用方观察，同时要求
    # 继续采集。最终使用应以 need_more_measurement == False 为准。
    if scan_count < m_max:
        return True, residual_table

    # m_max 是防止无限扫描的硬停止条件。此时表仍可使用，但精度未完全达标，
    # 因而通过告警让上层程序能够记录或提示操作人员。
    warnings.warn(
        "阶段2已达到 m_max，但至少一个网格点仍未达到 alpha 精度要求；"
        "返回当前残差表。",
        RuntimeWarning,
        stacklevel=2,
    )
    return False, residual_table

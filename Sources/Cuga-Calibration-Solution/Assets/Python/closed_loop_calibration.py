"""闭环校准的首次测量与阶段2多次扫描残差处理。

首次测量使用 :func:`process_first_measurement` 生成粗修正表 ``C0``；下发
``C0`` 后，调用方在每次阶段2扫描后，把截至当前累计的二维残差和对应的
期望位置以及逐扫描有效 mask 传给 :func:`process_stage2_residuals`。

数组最后一维统一使用 ``[..., 0] = X``、``[..., 1] = Y``。函数本身不保存
历史状态，因此调用方需要保留每次扫描结果，并在下一次调用时传入完整历史。
"""

from __future__ import annotations

import warnings

import numpy as np


def _prepare_boolean_mask(
    mask: np.ndarray | None,
    expected_shape: tuple[int, ...],
    name: str,
) -> np.ndarray:
    """校验并返回指定形状的布尔 mask；未传入时默认为全有效。"""
    if mask is None:
        return np.ones(expected_shape, dtype=bool)

    mask_array = np.asarray(mask)
    if mask_array.shape != expected_shape:
        raise ValueError(f"{name} 的形状必须为 {expected_shape}")
    if mask_array.dtype != np.bool_:
        raise TypeError(f"{name} 必须是布尔数组")
    return mask_array


def process_first_measurement(
    residual: np.ndarray,
    desired_positions: np.ndarray,
    mask: np.ndarray | None = None,
    fill_value: float = 0.0,
) -> np.ndarray:
    """处理阶段1的首次测量，生成粗修正表 ``C0``。

    参数:
        residual: 第一次测量得到的二维误差场
            ``E = P_measured - P_desired``，形状为 ``(..., 2)``。最后一维的
            ``[..., 0]``、``[..., 1]`` 依次为 X、Y 残差，前置维度是测量网格。
        desired_positions: 网格点的期望二维坐标，形状为 ``(..., 2)``，必须
            与 ``residual`` 完全相同。最后一维的 ``[..., 0]``、``[..., 1]``
            依次为 X、Y 坐标。
        mask: 可选的单次扫描有效点掩码，形状为 ``residual.shape[:-1]``。
            ``True`` 表示该点参与阶段1拟合，``False`` 表示该点不参与计算；
            无效点在返回的粗修正表中使用 ``fill_value``。
        fill_value: 阶段1 mask=False 点的初始修正值，必须是有限标量，默认
            为 ``0.0``，表示该点先不施加修正，后续由阶段2测量结果补偿。

    返回:
        粗修正表 ``C0``，形状与 ``residual`` 相同。二维非共线网格按
        ``C0 = -(E - A0)`` 扣除完整 6 参数仿射场；单行等共线点集只分别
        扣除 X、Y 残差均值，再取相反数。mask=False 点使用 ``fill_value``，
        因此返回表可以作为完整的阶段1初始修正表下发。

    说明:
        mask 有效位置的返回值由阶段1测量得到；无效位置使用初始填充值，
        并在该完整 ``C0`` 实际下发后开始阶段2。阶段2函数的输入仍应是
        下发 ``C0`` 后重新测得的残差，而不是本函数返回的原始测量结果。
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
    fill_value_array = np.asarray(fill_value, dtype=float)
    if fill_value_array.ndim != 0 or not np.isfinite(fill_value_array):
        raise ValueError("fill_value 必须是有限标量")
    fill_value_scalar = float(fill_value_array)
    if positions.shape != values.shape:
        raise ValueError("desired_positions 的形状必须与 residual 相同")
    if not np.all(np.isfinite(positions)):
        raise ValueError("desired_positions 只能包含有限数值")

    valid_mask = _prepare_boolean_mask(mask, values.shape[:-1], "mask")
    finite_values = np.all(np.isfinite(values), axis=-1)
    if mask is None:
        if not np.all(finite_values):
            raise ValueError("residual 只能包含有限数值")
    elif np.any(valid_mask & ~finite_values):
        raise ValueError("mask=True 的 residual 点只能包含有限数值")
    if not np.any(valid_mask):
        raise ValueError("mask 至少需要包含一个有效点")

    # [x, y, 1] 同时拟合 X、Y 残差，满秩时得到线性变换和平移共 6 个参数。
    flat_positions = positions.reshape(-1, 2)
    flat_values = values.reshape(-1, 2)
    flat_valid = valid_mask.reshape(-1)
    affine_design = np.column_stack(
        (flat_positions, np.ones(flat_positions.shape[0], dtype=float))
    )
    valid_affine_design = affine_design[flat_valid]

    if np.linalg.matrix_rank(affine_design) == 3:
        if np.linalg.matrix_rank(valid_affine_design) < 3:
            raise ValueError(
                "阶段1 mask 过滤后有效点不足以拟合完整二维仿射；"
                "至少需要 3 个不共线有效点"
            )
        affine_parameters, _, _, _ = np.linalg.lstsq(
            valid_affine_design, flat_values[flat_valid], rcond=None
        )
        affine_field = affine_design @ affine_parameters
        residual_without_drift = np.full_like(flat_values, np.nan, dtype=float)
        residual_without_drift[flat_valid] = (
            flat_values[flat_valid] - affine_field[flat_valid]
        )
    else:
        # 单行数据无法区分沿行线性趋势和真实 stage map 误差，因此仅去平移。
        component_mean = np.mean(flat_values[flat_valid], axis=0)
        residual_without_drift = np.full_like(flat_values, np.nan, dtype=float)
        residual_without_drift[flat_valid] = (
            flat_values[flat_valid] - component_mean
        )

    # 修正方向与测量误差相反：控制器下发 C0 后用于抵消首次测得的误差场。
    correction = -residual_without_drift.reshape(values.shape)
    if mask is not None:
        correction[~valid_mask] = fill_value_scalar
    return correction


def interpolate_residual_table(
    residual_table: np.ndarray,
    desired_positions: np.ndarray,
    target_positions: np.ndarray,
    source_mask: np.ndarray | None = None,
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
        source_mask: 可选的源网格有效点掩码，形状必须为
            ``(N_y, N_x)``。``True`` 表示该源节点可用于插值；``False``
            表示该节点无效。未传入时，函数根据 ``residual_table`` 每个节点
            的 X/Y 分量是否均为有限数值自动判断有效性，因此源表允许在无效
            节点包含 ``NaN``。

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
        每个目标点所在源网格单元优先使用有效角点插值。四个角点均有效时
        执行普通双线性插值；存在无效角点时对剩余角点的权重重新归一化。
        如果有效角点少于三个，或目标点落在无效角点上导致有效权重为零，
        则直接使用距离目标坐标最近的源节点残差。最近邻优先选择
        ``source_mask=True`` 且有限的节点；如果没有这样的节点，则使用源表中
        最近的有限节点；如果源表没有有限值，则将非有限值按零处理后使用最近
        节点，不因有效节点数量触发异常。
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
    finite_source_values = np.all(np.isfinite(values), axis=-1)
    if source_mask is None:
        source_valid_mask = finite_source_values
    else:
        source_mask_array = np.asarray(source_mask)
        if source_mask_array.shape != values.shape[:-1]:
            raise ValueError(
                "source_mask 的形状必须与 residual_table 的网格形状相同"
            )
        if source_mask_array.dtype != np.bool_:
            raise TypeError("source_mask 必须是布尔数组")
        source_valid_mask = source_mask_array & finite_source_values
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
        grid_valid_mask = source_valid_mask
        x_axis = x_by_column
        y_axis = y_by_row
    elif transposed_layout:
        grid_values = np.transpose(values, (1, 0, 2))
        grid_valid_mask = np.transpose(source_valid_mask, (1, 0))
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
        grid_valid_mask = grid_valid_mask[:, ::-1]
    if y_decreasing:
        y_axis = y_axis[::-1]
        grid_values = grid_values[::-1, :, :]
        grid_valid_mask = grid_valid_mask[::-1, :]

    # 最近邻回退使用统一后的源网格坐标。优先使用 mask=True 且有限的节点；
    # 当一个这样的节点都没有时，退回到所有有限节点，避免有效点数量导致
    # 插值失败。若源表完全没有有限值，则将非有限值转换为零后继续处理。
    finite_source_mask = np.all(np.isfinite(grid_values), axis=-1)
    nearest_source_mask = grid_valid_mask.copy()
    if not np.any(nearest_source_mask):
        nearest_source_mask = finite_source_mask
    if np.any(nearest_source_mask):
        nearest_source_values = grid_values[nearest_source_mask]
    else:
        nearest_source_mask = np.ones_like(finite_source_mask, dtype=bool)
        nearest_source_values = np.nan_to_num(
            grid_values,
            nan=0.0,
            posinf=0.0,
            neginf=0.0,
        )[nearest_source_mask]
    source_x_grid, source_y_grid = np.meshgrid(x_axis, y_axis)
    nearest_source_coordinates = np.column_stack(
        (
            source_x_grid[nearest_source_mask],
            source_y_grid[nearest_source_mask],
        )
    )

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
    lower_left_valid = grid_valid_mask[y_index, x_index]
    lower_right_valid = grid_valid_mask[y_index, x_index + 1]
    upper_left_valid = grid_valid_mask[y_index + 1, x_index]
    upper_right_valid = grid_valid_mask[y_index + 1, x_index + 1]

    # 将四个角点统一为 (目标点, 角点, X/Y 分量)，便于按目标点统计有效角点
    # 数量并对缺失角点执行掩码插值。
    corner_values = np.stack(
        (lower_left, lower_right, upper_left, upper_right), axis=1
    )
    corner_valid = np.stack(
        (
            lower_left_valid,
            lower_right_valid,
            upper_left_valid,
            upper_right_valid,
        ),
        axis=1,
    )
    valid_corner_count = np.sum(corner_valid, axis=1)
    insufficient_corner_points = valid_corner_count < 3

    three_corner_points = valid_corner_count == 3
    if np.any(three_corner_points):
        warnings.warn(
            "至少有一个目标点所在源网格单元只有 3 个有效角点；"
            "将对剩余角点执行归一化插值。",
            RuntimeWarning,
            stacklevel=2,
        )

    # 每个权重都是一个目标点对应一个标量；axis=1 对四个角点求和，最后
    # 一维的 X/Y 两个残差分量同时参与计算。无效角点的值先置零，避免
    # 0 * NaN 仍然传播 NaN；其权重在分母中也一并排除。
    corner_weights = np.stack(
        (
            (1.0 - tx) * (1.0 - ty),
            tx * (1.0 - ty),
            (1.0 - tx) * ty,
            tx * ty,
        ),
        axis=1,
    )
    valid_weight_sum = np.sum(
        np.where(corner_valid, corner_weights, 0.0), axis=1
    )
    zero_weight_points = np.isclose(valid_weight_sum, 0.0, atol=1e-12, rtol=0.0)
    nearest_neighbor_points = insufficient_corner_points | zero_weight_points

    weighted_corner_values = np.where(
        corner_valid[..., None], corner_values, 0.0
    )
    interpolated = np.empty((flat_targets.shape[0], 2), dtype=float)
    interpolation_points = ~nearest_neighbor_points
    interpolated[interpolation_points] = np.sum(
        corner_weights[interpolation_points, :, None]
        * weighted_corner_values[interpolation_points],
        axis=1,
    ) / valid_weight_sum[interpolation_points, None]

    if np.any(nearest_neighbor_points):
        fallback_targets = flat_targets[nearest_neighbor_points]
        distances = np.sum(
            (
                fallback_targets[:, None, :]
                - nearest_source_coordinates[None, :, :]
            )
            ** 2,
            axis=-1,
        )
        nearest_source_indices = np.argmin(distances, axis=1)
        interpolated[nearest_neighbor_points] = nearest_source_values[
            nearest_source_indices
        ]
    # 恢复目标坐标网格的原始形状，并保持最后一维为 [X, Y] 残差。
    return interpolated.reshape(targets.shape)


def process_stage2_residuals(
    residuals: np.ndarray,
    desired_positions: np.ndarray,
    alpha: float = 0.3,
    m_min: int = 5,
    m_max: int = 20,
    masks: np.ndarray | None = None,
) -> tuple[bool, np.ndarray | None]:
    """判断阶段2是否需要再次测量，并计算残余重复信号表。

    参数:
        residuals: 累计二维残差数组，形状为 ``(M, ..., 2)``。其中 ``M`` 是
            最前面的扫描序号维，最后一维的 ``[..., 0]``、``[..., 1]`` 依次为
            X、Y 残差，前置网格维度在每次扫描中必须相同。传入 ``masks`` 时，
            mask=False 的位置允许为 ``NaN`` 或其他非有限数值；mask=True 的
            位置必须是有限数值。
        desired_positions: 网格点的期望二维坐标，形状为 ``(..., 2)``，必须
            与单次残差的形状完全相同。最后一维的 ``[..., 0]``、``[..., 1]``
            依次为 X、Y 坐标。二维非共线网格执行完整仿射拟合；单行等共线
            点集只扣除 X、Y 分量均值。
        alpha: 停止比例。每个网格点保留的有效测量数须达到
            ``ceil(1 / alpha**2)``。
        m_min: 执行 MAD 剔除和计算残差表前所需的最少扫描次数。
        m_max: 最大扫描次数。达到该次数后，即使未满足精度要求也停止。
        masks: 可选的逐扫描有效点掩码，形状必须为
            ``(M, ...)``，其中每个 ``masks[i]`` 与 ``residuals[i, ..., 0]``
            的网格形状相同。``masks[i, ...] == True`` 表示第 ``i`` 次扫描的
            该点可用；不同扫描可以有不同 mask。未传入时所有扫描的所有点均
            视为有效。调用方传入累计历史时，``masks`` 必须与 ``residuals``
            的扫描序号逐一对应。

    返回:
        ``(need_more_measurement, residual_table)``：

        - ``need_more_measurement`` 为 ``True`` 时，调用方应再次测量。
        - ``residual_table`` 是方案中的残余重复信号 ``delta C``；扫描次数少于
          ``m_min`` 时为 ``None``，否则为形状 ``(..., 2)`` 的数组。没有任何
          有效观测的点保留为 ``NaN``；达到 ``m_max`` 仍未获得足够有效观测的
          点也保留为 ``NaN``。

    说明:
        对二维非共线网格，每次扫描只用该次 mask 有效的点拟合并扣除完整二维
        仿射漂移场（线性变换和平移共 6 个参数）；如果某次扫描的有效点不足
        以拟合完整二维仿射，则直接报错。对于整个输入本来就是单行等共线点
        集的情况，仍只分别扣除 X、Y 残差均值。达到 ``m_min`` 后，对每个网
        格点的 X、Y 分量分别在各自有效扫描上执行 3 倍稳健标准差的 MAD 异常
        值剔除，再对保留值取算术均值。每个点的停止条件同时要求其有效样本
        数达到 ``m_min`` 和 ``ceil(1 / alpha**2)``；达到 ``m_max`` 仍未满足
        时，会返回当前结果并发出 ``RuntimeWarning``。
    """
    # 统一转换为浮点数组：既允许调用方传入 list，也避免整数输入在减去
    # 仿射拟合结果时发生截断。这里只创建数组视图或副本，不修改调用方数据。
    values = np.asarray(residuals, dtype=float)
    positions = np.asarray(desired_positions, dtype=float)

    # 数据布局约定：
    #   values.shape    == (M, 网格维度..., 2)
    #   positions.shape == (网格维度..., 2)
    #   masks.shape     == (M, 网格维度...)
    # 其中 M 是扫描序号维；最后一维的 [..., 0]、[..., 1] 分别代表 X、Y。
    # 前置网格可以是一维点列，也可以是二维网格。
    if values.ndim < 3 or values.shape[-1] != 2:
        raise ValueError("residuals 的形状必须为 (扫描次数, 网格..., 2)")
    if values.shape[0] == 0 or any(size == 0 for size in values.shape[1:-1]):
        raise ValueError("residuals 的扫描维和网格维均不能为空")
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

    finite_values = np.all(np.isfinite(values), axis=-1)
    masks_shape = (scan_count,) + values.shape[1:-1]
    scan_masks = _prepare_boolean_mask(masks, masks_shape, "masks")
    if masks is None:
        if not np.all(finite_values):
            raise ValueError("residuals 只能包含有限数值")
    elif np.any(scan_masks & ~finite_values):
        raise ValueError("masks=True 的 residuals 点只能包含有限数值")
    scan_valid = scan_masks & finite_values

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

    # 对二维非共线网格，每次扫描都必须保留至少三个不共线有效点；这是
    # mask 造成的几何退化，按约定直接报错，而不把不同扫描混用不同的去漂移
    # 模型。整个输入若本来就是共线网格，则沿用原有的只扣均值模式。
    for scan_index, scan_valid_mask in enumerate(scan_valid):
        flat_scan_valid = scan_valid_mask.reshape(-1)
        valid_count = int(np.sum(flat_scan_valid))
        if valid_count == 0:
            raise ValueError(
                f"第 {scan_index} 次扫描没有有效点，无法进行阶段2去漂移"
            )
        if use_full_affine:
            valid_design = affine_design[flat_scan_valid]
            if np.linalg.matrix_rank(valid_design) < 3:
                raise ValueError(
                    f"第 {scan_index} 次扫描 mask 过滤后有效点不足以拟合完整二维仿射；"
                    "至少需要 3 个不共线有效点"
                )

    # 方案规定累计达到 m_min 后才估计统计量。在此之前明确要求继续测量，
    # 并用 None 表示尚未形成可用的 delta C 表。
    if scan_count < m_min:
        return True, None

    # 每次扫描独立使用自己的 mask 拟合仿射漂移 A_i(x)，计算 r_i(x)-A_i(x)。
    # 无效点不参与拟合，也不进入后续跨扫描统计，内部统一保留为 NaN。
    drift_removed = np.full(values.shape, np.nan, dtype=float)
    for scan_index, scan in enumerate(values):
        flat_scan = scan.reshape(-1, 2)
        flat_scan_valid = scan_valid[scan_index].reshape(-1)
        flat_drift = drift_removed[scan_index].reshape(-1, 2)
        if use_full_affine:
            valid_design = affine_design[flat_scan_valid]
            affine_parameters, _, _, _ = np.linalg.lstsq(
                valid_design,
                flat_scan[flat_scan_valid],
                rcond=None,
            )
            affine_field = affine_design @ affine_parameters
            flat_drift[flat_scan_valid] = (
                flat_scan[flat_scan_valid] - affine_field[flat_scan_valid]
            )
        else:
            component_mean = np.mean(flat_scan[flat_scan_valid], axis=0)
            flat_drift[flat_scan_valid] = (
                flat_scan[flat_scan_valid] - component_mean
            )

    # 以下统计均沿最前面的扫描序号维进行，网格位置及最后一维的 X/Y 分量
    # 保持不变。nanmedian/nanmax 只在逐扫描 mask 有效的观测上计算；全程没有
    # 有效观测的点保留为 NaN，不让 NumPy 的空切片告警干扰正常流程。
    with warnings.catch_warnings():
        warnings.simplefilter("ignore", RuntimeWarning)
        point_median = np.nanmedian(drift_removed, axis=0)
        absolute_deviation = np.abs(drift_removed - point_median)
        mad = np.nanmedian(absolute_deviation, axis=0)
        maximum_abs_value = np.nanmax(
            np.where(scan_valid[..., None], np.abs(values), np.nan), axis=0
        )
    robust_sigma = 1.4826 * mad

    # 理论上完全相同的数据在浮点仿射运算后可能产生约 1e-15 量级的差异。
    # 这个仅与机器精度和数据量级相关的容差可避免 MAD=0 时把舍入误差误判
    # 为异常点；它远小于正常测量噪声，不改变实际的 3 sigma 判据。
    maximum_abs_value = np.nan_to_num(
        maximum_abs_value,
        nan=0.0,
        posinf=0.0,
        neginf=0.0,
    )
    numerical_tolerance = (
        64.0
        * np.finfo(float).eps
        * np.maximum(1.0, maximum_abs_value)
    )
    # good 与 drift_removed 形状相同；True 表示该次扫描在该点、该分量有效，
    # 同时满足预先 mask 和 MAD 判据。
    good = scan_valid[..., None] & (
        absolute_deviation <= 3.0 * robust_sigma + numerical_tolerance
    )

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
    # 动态 mask 下还要求每个点达到 m_min 个有效观测，防止某点只有极少数
    # 样本时因 MAD=0 而过早收敛。
    required_count = int(np.ceil(1.0 / alpha**2))
    minimum_point_count = max(m_min, required_count)
    point_precision_reached = np.all(
        good_count >= minimum_point_count,
        axis=-1,
    )
    # 必须所有网格点的 X/Y 分量都达标，才算阶段2整体收敛。
    precision_reached = bool(np.all(point_precision_reached))

    if precision_reached:
        return False, residual_table
    # 尚未达到精度且仍有扫描额度时，返回当前估计表供调用方观察，同时要求
    # 继续采集。最终使用应以 need_more_measurement == False 为准。
    if scan_count < m_max:
        return True, residual_table

    # m_max 是防止无限扫描的硬停止条件。未获得足够有效观测的点不输出当前
    # 的低样本估计，避免上层误把它们当成可下发的补偿值。
    residual_table = residual_table.copy()
    residual_table[~point_precision_reached] = np.nan
    warnings.warn(
        "阶段2已达到 m_max，但至少一个网格点仍未达到 alpha 精度要求或有效"
        "观测次数不足；未达标点返回 NaN。",
        RuntimeWarning,
        stacklevel=2,
    )
    return False, residual_table


def combine_correction_tables(
    initial_correction: np.ndarray,
    residual_table: np.ndarray | None,
    stage1_mask: np.ndarray,
) -> tuple[np.ndarray, np.ndarray]:
    """合并阶段1初始表和阶段2残差表，并生成插值有效 mask。

    参数:
        initial_correction: 实际下发阶段2前的完整初始修正表 ``C0_used``，
            形状为 ``(..., 2)``。它应当已经通过
            :func:`process_first_measurement` 的 ``fill_value`` 填补无效点，
            因而所有分量都必须是有限数值。
        residual_table: 阶段2输出的 ``delta C``，形状必须与
            ``initial_correction`` 相同。没有达到 ``m_min`` 时传入 ``None``
            会直接报错，因为此时不能形成最终修正表。阶段2长期无效的点可
            以是 ``NaN``。
        stage1_mask: 阶段1原始有效点 mask，形状为
            ``initial_correction.shape[:-1]``。``True`` 表示该点的 ``C0``
            来自有效的阶段1测量；``False`` 表示 ``C0`` 是填充值。

    返回:
        ``(final_correction, interpolation_mask)``：

        - ``final_correction``：阶段2有效点使用
          ``C_final = C0_used - delta C``；阶段2无效点保留 ``C0_used``，
          以保证仍可生成完整的设备下发表。
        - ``interpolation_mask``：后续插值可使用的源点 mask，定义为
          ``stage1_mask | stage2_valid``。阶段1和阶段2都无效的点虽然在
          ``final_correction`` 中保留了填充值，但不会参与后续插值。
    """
    initial_values = np.asarray(initial_correction, dtype=float)
    if initial_values.ndim < 2 or initial_values.shape[-1] != 2:
        raise ValueError(
            "initial_correction 的形状必须为 (网格..., 2)"
        )
    if any(size == 0 for size in initial_values.shape[:-1]):
        raise ValueError("initial_correction 的网格维不能为空")
    if not np.all(np.isfinite(initial_values)):
        raise ValueError(
            "initial_correction 必须是实际下发的完整有限修正表"
        )
    if residual_table is None:
        raise ValueError("residual_table 不能为 None；阶段2尚未形成 delta C")

    delta_values = np.asarray(residual_table, dtype=float)
    if delta_values.shape != initial_values.shape:
        raise ValueError(
            "residual_table 的形状必须与 initial_correction 相同"
        )

    stage1_valid = _prepare_boolean_mask(
        stage1_mask,
        initial_values.shape[:-1],
        "stage1_mask",
    )
    stage2_valid = np.all(np.isfinite(delta_values), axis=-1)

    final_correction = initial_values.copy()
    final_correction[stage2_valid] = (
        initial_values[stage2_valid] - delta_values[stage2_valid]
    )
    interpolation_mask = stage1_valid | stage2_valid
    return final_correction, interpolation_mask
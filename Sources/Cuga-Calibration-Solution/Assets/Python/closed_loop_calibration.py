"""闭环校准的首次测量与阶段2多次扫描残差处理。

首次测量使用 :func:`process_first_measurement` 生成去仿射后的测量误差表
``C0``；调用方按设备约定处理并下发 ``C0`` 后，在每次阶段2扫描后把截至当前
累计的二维残差和对应的期望位置以及逐扫描有效 mask 传给
:func:`process_stage2_residuals`。

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
    """处理阶段1的首次测量，生成去仿射后的测量误差表 ``C0``。

    参数:
        residual: 第一次测量得到的二维误差场
            ``E = P_measured - P_desired``，形状为 ``(..., 2)``。最后一维的
            ``[..., 0]``、``[..., 1]`` 依次为 X、Y 残差，前置维度是测量网格。
        desired_positions: 网格点的期望二维坐标，形状为 ``(..., 2)``，必须
            与 ``residual`` 完全相同。最后一维的 ``[..., 0]``、``[..., 1]``
            依次为 X、Y 坐标。
        mask: 可选的单次扫描有效点掩码，形状为 ``residual.shape[:-1]``。
            ``True`` 表示该点参与阶段1拟合，``False`` 表示该点不参与计算；
            无效点在返回的测量误差表中使用 ``fill_value``。
        fill_value: 阶段1 mask=False 点的初始填充值，必须是有限标量，默认
            为 ``0.0``，表示该点暂时没有可用的测量误差值，后续由阶段2测量结果补偿。

    返回:
        处理后的测量误差表 ``C0``，形状与 ``residual`` 相同。二维非共线网格
        按 ``C0 = E - A0`` 扣除完整 6 参数仿射场；单行等共线点集只分别
        扣除 X、Y 残差均值，不改变符号。mask=False 点使用 ``fill_value``。

    说明:
        mask 有效位置的返回值由阶段1测量得到；无效位置使用初始填充值。
        本函数只输出去仿射后的测量误差，不将其转换为相反方向的修正量；
        阶段2函数的输入仍应是调用方按设备约定处理并下发 ``C0`` 后重新测得的
        残差，而不是本函数返回的原始测量结果。
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

    # 输出去仿射后的测量误差，不转换为与测量误差相反的修正方向。
    processed_residual = residual_without_drift.reshape(values.shape)
    if mask is not None:
        processed_residual[~valid_mask] = fill_value_scalar
    return processed_residual


def interpolate_residual_table(
    residual_table: np.ndarray,
    desired_positions: np.ndarray,
    target_positions: np.ndarray,
    source_mask: np.ndarray | None = None,
    target_mask: np.ndarray | None = None,
) -> np.ndarray:
    """将规则二维残差源网格双线性插值/外插到另一组坐标网格。

    ``residual_table`` 和 ``desired_positions`` 共同定义源网格：前者给出
    每个源网格节点的 X/Y 残差，后者给出这些节点的期望 X/Y 坐标。函数不
    修改任何输入数组；输入会转为浮点数组后参与计算。

    参数:
        residual_table: 源残差表，形状为 ``(N_y, N_x, 2)``。最后一维的
            ``[..., 0]``、``[..., 1]`` 依次为 X、Y 残差。
        desired_positions: 源网格点的期望二维坐标，形状必须为
            ``(N_y, N_x, 2)``。最后一维的 ``[..., 0]``、``[..., 1]`` 依次
            为 X、Y 坐标；前两个维度对应源表节点。源坐标必须构成轴对齐的
            规则矩形网格：X 只能沿列方向变化，Y 只能沿行方向变化；每个
            方向允许使用非均匀间距，也允许坐标轴递减。
        target_positions: 需要查询的期望二维坐标网格，形状为
            ``(..., 2)``。最后一维的 ``[..., 0]``、``[..., 1]`` 依次为待
            查询点的 X、Y 坐标；其前置维度可以与源网格不同，也不要求本身是
            规则网格。
        source_mask: 可选的源网格有效点掩码，形状必须为
            ``(N_y, N_x)``。``True`` 表示该源节点可用于插值；``False``
            表示该节点无效。未传入时，函数根据 ``residual_table`` 每个节点
            的 X/Y 分量是否均为有限数值自动判断有效性，因此源表允许在无效
            节点包含 ``NaN``。
        target_mask: 可选的目标网格插值掩码，形状必须为
            ``target_positions.shape[:-1]``。``True`` 表示该目标点需要插值；
            ``False`` 表示不需要插值，返回数组中该点的 X/Y 两个分量都设为
            ``0.0``。未传入时所有目标点都需要插值。

    返回:
        形状与 ``target_positions`` 相同的插值残差表。对每个
        ``target_mask=True`` 的目标点，使用其所在源网格单元的四个角点做
        双线性插值。若角点无效，则用距离该角点最近且尚未使用的有效源点
        替代该角点的值，并保留原角点的双线性权重；发生这种补替时发出
        ``RuntimeWarning``。

        目标点落在源网格范围外时，使用最靠近的边界网格单元，并允许双线性
        坐标超出 ``[0, 1]``，因此会沿边界局部趋势外插，同时发出一次
        ``RuntimeWarning``。有效源节点少于 4 个时抛出 ``ValueError``。

    说明:
        源坐标必须是规则矩形网格；目标点可以是任意形状。角点补替只针对
        双线性权重非零的无效角点，并且优先保证同一个目标点使用的四个源点
        不重复。目标 mask 为 ``False`` 的点不会执行源点数量检查，也不会
        触发插值警告。
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

    if target_mask is None:
        target_interpolation_mask = np.ones(targets.shape[:-1], dtype=bool)
    else:
        target_mask_array = np.asarray(target_mask)
        if target_mask_array.shape != targets.shape[:-1]:
            raise ValueError(
                "target_mask 的形状必须与 target_positions 的前置维度相同"
            )
        if target_mask_array.dtype != np.bool_:
            raise TypeError("target_mask 必须是布尔数组")
        target_interpolation_mask = target_mask_array

    # target_mask=False 的点不需要源点检查，也不执行任何插值，输出保持为 0。
    interpolated = np.zeros(targets.shape, dtype=float)
    flat_interpolated = interpolated.reshape(-1, 2)
    flat_target_mask = target_interpolation_mask.reshape(-1)
    active_target_indices = np.flatnonzero(flat_target_mask)
    if active_target_indices.size == 0:
        return interpolated

    # 源表可能按 [Y 行, X 列] 或 meshgrid(indexing="ij") 的 [X 行, Y 列]
    # 布局传入；统一转换为 grid_values[y, x]，不改变调用方数组。
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
        grid_positions = positions
        grid_valid_mask = source_valid_mask
        x_axis = x_by_column
        y_axis = y_by_row
    elif transposed_layout:
        grid_values = np.transpose(values, (1, 0, 2))
        grid_positions = np.transpose(positions, (1, 0, 2))
        grid_valid_mask = np.transpose(source_valid_mask, (1, 0))
        x_axis = x_by_row
        y_axis = y_by_column
    else:
        raise ValueError(
            "desired_positions 必须构成轴对齐的规则矩形源网格；"
            "X 只能沿一个网格轴变化，Y 只能沿另一个网格轴变化"
        )

    source_row_count, source_column_count = grid_values.shape[:-1]
    if source_row_count < 2 or source_column_count < 2:
        raise ValueError("双线性插值要求源网格至少包含 2 行和 2 列")

    # 双线性插值要求源坐标是轴对齐的矩形网格。先验证网格结构，再把
    # 递减坐标轴翻转为递增，便于使用 searchsorted 定位网格单元。
    x_differences = np.diff(x_axis)
    y_differences = np.diff(y_axis)
    if not (np.all(x_differences > 0) or np.all(x_differences < 0)):
        raise ValueError("desired_positions 的 X 轴坐标必须严格单调")
    if not (np.all(y_differences > 0) or np.all(y_differences < 0)):
        raise ValueError("desired_positions 的 Y 轴坐标必须严格单调")

    if x_differences[0] < 0:
        grid_values = grid_values[:, ::-1, :]
        grid_positions = grid_positions[:, ::-1, :]
        grid_valid_mask = grid_valid_mask[:, ::-1]
    if y_differences[0] < 0:
        grid_values = grid_values[::-1, :, :]
        grid_positions = grid_positions[::-1, :, :]
        grid_valid_mask = grid_valid_mask[::-1, :]

    x_axis = grid_positions[0, :, 0]
    y_axis = grid_positions[:, 0, 1]
    flat_positions = grid_positions.reshape(-1, 2)
    flat_values = grid_values.reshape(-1, 2)
    flat_source_valid = grid_valid_mask.reshape(-1)
    valid_flat_indices = np.flatnonzero(flat_source_valid)
    valid_source_count = valid_flat_indices.size
    if valid_source_count < 4:
        raise ValueError(
            "需要至少 4 个有效源点，"
            f"当前只有 {valid_source_count} 个"
        )

    flat_targets = targets.reshape(-1, 2)
    active_targets = flat_targets[active_target_indices]
    # 范围判断使用完整源网格的边界，而不是有效点的包围盒。这样某个边界
    # 节点失效时，仍能把目标点视为同一标定网格内的点，并对缺失角点补替。
    source_min = np.array([x_axis[0], y_axis[0]], dtype=float)
    source_max = np.array([x_axis[-1], y_axis[-1]], dtype=float)
    outside_source_range = np.any(
        (active_targets < source_min) | (active_targets > source_max), axis=1
    )
    if np.any(outside_source_range):
        warnings.warn(
            "target_positions 中至少有一个需要插值的点位于源网格范围外，"
            "将使用边界网格单元进行双线性外插。",
            RuntimeWarning,
            stacklevel=2,
        )

    # searchsorted 的 cell index 在范围外会落到两端之外；clip 到边界单元，
    # 但不裁剪归一化坐标 u/v，使边界单元可以真正外插。
    x_cell_indices = np.searchsorted(x_axis, active_targets[:, 0], side="right") - 1
    y_cell_indices = np.searchsorted(y_axis, active_targets[:, 1], side="right") - 1
    x_cell_indices = np.clip(x_cell_indices, 0, source_column_count - 2)
    y_cell_indices = np.clip(y_cell_indices, 0, source_row_count - 2)

    fallback_used = False
    for target_index, target, x_cell_index, y_cell_index in zip(
        active_target_indices,
        active_targets,
        x_cell_indices,
        y_cell_indices,
    ):
        x_cell_index = int(x_cell_index)
        y_cell_index = int(y_cell_index)
        x0 = x_axis[x_cell_index]
        x1 = x_axis[x_cell_index + 1]
        y0 = y_axis[y_cell_index]
        y1 = y_axis[y_cell_index + 1]
        u = (target[0] - x0) / (x1 - x0)
        v = (target[1] - y0) / (y1 - y0)

        # 角点顺序：左下、右下、左上、右上；数组行对应 Y，列对应 X。
        corner_rows = np.array(
            [y_cell_index, y_cell_index, y_cell_index + 1, y_cell_index + 1],
            dtype=int,
        )
        corner_columns = np.array(
            [x_cell_index, x_cell_index + 1, x_cell_index, x_cell_index + 1],
            dtype=int,
        )
        corner_flat_indices = (
            corner_rows * source_column_count + corner_columns
        )
        corner_values = flat_values[corner_flat_indices].copy()
        corner_valid = grid_valid_mask[corner_rows, corner_columns]
        corner_weights = np.array(
            [(1.0 - u) * (1.0 - v), u * (1.0 - v), (1.0 - u) * v, u * v],
            dtype=float,
        )

        # 角点有效时直接使用；无效角点只在其权重非零时补替。先把当前
        # 单元内的有效角点加入 used，保证替代点不与其他角点重复。
        used_source_indices = {
            int(index)
            for index in corner_flat_indices[corner_valid]
        }
        for corner_index, (is_valid, corner_weight) in enumerate(
            zip(corner_valid, corner_weights)
        ):
            if is_valid:
                continue
            if corner_weight == 0.0:
                corner_values[corner_index] = 0.0
                continue

            candidate_flat_indices = np.array(
                [
                    index
                    for index in valid_flat_indices
                    if int(index) not in used_source_indices
                ],
                dtype=int,
            )
            if candidate_flat_indices.size == 0:
                raise ValueError(
                    "无法为无效网格角点找到不重复的有效源点替代值"
                )
            candidate_distances = np.sum(
                (
                    flat_positions[candidate_flat_indices]
                    - grid_positions[
                        corner_rows[corner_index], corner_columns[corner_index]
                    ]
                )
                ** 2,
                axis=1,
            )
            nearest_order = np.lexsort(
                (candidate_flat_indices, candidate_distances)
            )
            replacement_index = int(candidate_flat_indices[nearest_order[0]])
            corner_values[corner_index] = flat_values[replacement_index]
            used_source_indices.add(replacement_index)
            fallback_used = True

        flat_interpolated[target_index] = np.sum(
            corner_values * corner_weights[:, None],
            axis=0,
        )

    if fallback_used:
        warnings.warn(
            "至少有一个双线性网格角点无效，已使用距离该角点最近的有效源点补替。",
            RuntimeWarning,
            stacklevel=2,
        )

    return interpolated


def process_stage2_residuals(
    residuals: np.ndarray,
    desired_positions: np.ndarray,
    alpha: float = 0.3,
    m_min: int = 5,
    m_max: int = 20,
    masks: np.ndarray | None = None,
) -> tuple[bool, np.ndarray]:
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
        m_min: 执行 MAD 剔除和生成正式残差表前所需的最少扫描次数。
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
        - ``residual_table`` 始终是形状 ``(..., 2)`` 的数组。扫描次数少于
          ``m_min`` 时，返回逐点去漂移后的简单平均值，不执行 MAD 异常值
          剔除；该数组仅用于日志记录，不能作为最终的 ``delta C`` 使用。
          达到 ``m_min`` 后才返回包含 MAD 剔除的 ``delta C``。没有任何有效
          观测的点保留为 ``NaN``；达到 ``m_max`` 仍未获得足够有效观测的点
          也保留为 ``NaN``。

    说明:
        对二维非共线网格，每次扫描只用该次 mask 有效的点拟合并扣除完整二维
        仿射漂移场（线性变换和平移共 6 个参数）；如果某次扫描的有效点不足
        以拟合完整二维仿射，则直接报错。对于整个输入本来就是单行等共线点
        集的情况，仍只分别扣除 X、Y 残差均值。扫描次数少于 ``m_min`` 时，
        先完成逐扫描去漂移，再对各点的有效观测直接取算术均值，仅用于日志
        记录，不执行 MAD 异常值剔除。达到 ``m_min`` 后，对每个网格点的 X、Y
        分量分别在各自有效扫描上执行 3 倍稳健标准差的 MAD 异常值剔除，再对
        保留值取算术均值。每个点的停止条件同时要求其有效样本数达到
        ``m_min`` 和 ``ceil(1 / alpha**2)``；达到 ``m_max`` 仍未满足时，会
        返回当前结果并发出 ``RuntimeWarning``。
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

    # m_min 之前不执行 MAD。仍返回逐点去漂移后的简单平均值，便于调用方
    # 写入日志；need_more=True 时调用方不得把该数组当作最终 delta C 使用。
    if scan_count < m_min:
        observation_count = np.sum(scan_valid, axis=0)
        observation_sum = np.sum(
            np.where(scan_valid[..., None], drift_removed, 0.0),
            axis=0,
        )
        residual_table_without_mad = np.divide(
            observation_sum,
            observation_count[..., None],
            out=np.full(values.shape[1:], np.nan, dtype=float),
            where=observation_count[..., None] > 0,
        )
        return True, residual_table_without_mad

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
            ``initial_correction`` 相同。``process_stage2_residuals`` 在未达到
            ``m_min`` 时也会返回数组，但该数组仅用于日志，调用方必须等到
            ``need_more=False`` 后再传入此函数；显式传入 ``None`` 仍会报错。
            阶段2长期无效的点可以是 ``NaN``。
        stage1_mask: 阶段1原始有效点 mask，形状为
            ``initial_correction.shape[:-1]``。``True`` 表示该点的 ``C0``
            来自有效的阶段1测量；``False`` 表示 ``C0`` 是填充值。

    返回:
        ``(final_correction, interpolation_mask)``：

        - ``final_correction``：阶段2有效点使用
          ``C_final = C0_used - delta C``；阶段2无效但阶段1有效的点保留
          ``C0_used``。阶段1和阶段2都无效的点置为 ``NaN``，等待后续插值
          使用其他有效源点生成设备下发表。
        - ``interpolation_mask``：后续插值可使用的源点 mask，定义为
          ``stage1_mask | stage2_valid``。阶段1和阶段2都无效的点在
          ``final_correction`` 中为 ``NaN``，不会参与后续插值。
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
    final_correction[~interpolation_mask] = np.nan
    return final_correction, interpolation_mask

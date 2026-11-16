"""闭环校准的首次测量与阶段2多次扫描残差处理。

首次测量使用 :func:`process_first_measurement` 生成去仿射后的测量误差表
``C0``；调用方按设备约定处理并下发 ``C0`` 后，在每次阶段2扫描后把截至当前
累计的二维残差和对应的期望位置以及逐扫描有效 mask 传给
:func:`process_stage2_residuals`。阶段2的 mask 必须由调用方显式提供。
阶段2的总扫描次数和外部停止上限也由调用方控制；本模块只按目标有效观测次数
判断是否还需要继续测量。

本模块内部的所有 map 都保持 Python 侧的测量误差/合并符号，不做整体取负；
设备下发所需的符号转换由调用方在下发边界处理。

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
        residual_without_drift = np.zeros_like(flat_values, dtype=float)
        residual_without_drift[flat_valid] = (
            flat_values[flat_valid] - affine_field[flat_valid]
        )
    else:
        # 单行数据无法区分沿行线性趋势和真实 stage map 误差，因此仅去平移。
        component_mean = np.mean(flat_values[flat_valid], axis=0)
        residual_without_drift = np.zeros_like(flat_values, dtype=float)
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
    target_valid_count: int = 7,
    masks: np.ndarray | None = None,
) -> tuple[bool, np.ndarray, np.ndarray]:
    """判断阶段2是否需要再次测量，并计算残余重复信号表。

    参数:
        residuals: 累计二维残差数组，形状为 ``(M, ..., 2)``。其中 ``M`` 是
            最前面的扫描序号维，最后一维的 ``[..., 0]``、``[..., 1]`` 依次为
            X、Y 残差，前置网格维度在每次扫描中必须相同。mask=False 的位置
            允许为 ``NaN`` 或其他非有限数值；mask=True 的位置必须是有限数值。
        desired_positions: 网格点的期望二维坐标，形状为 ``(..., 2)``，必须
            与单次残差的形状完全相同。最后一维的 ``[..., 0]``、``[..., 1]``
            依次为 X、Y 坐标。二维非共线网格执行完整仿射拟合；单行等共线
            点集只扣除 X、Y 分量均值。
        target_valid_count: 每个有效网格点需要达到的最少有效观测次数，记为
            ``N``。总扫描次数和是否在未达到 ``N`` 时强制停止由调用方控制。
        masks: 必须提供的逐扫描有效点掩码，形状必须为
            ``(M, ...)``，其中每个 ``masks[i]`` 与 ``residuals[i, ..., 0]``
            的网格形状相同。``masks[i, ...] == True`` 表示第 ``i`` 次扫描的
            该点可用；不同扫描可以有不同 mask。调用方传入累计历史时，
            ``masks`` 必须与 ``residuals`` 的扫描序号逐一对应。未提供时直接
            抛出错误，不再默认所有点有效。

    返回:
        ``(need_more_measurement, residual_table, stage2_valid_mask)``：

        - ``need_more_measurement`` 为 ``True`` 时，调用方应再次测量。
        - ``residual_table`` 始终是形状 ``(..., 2)`` 且有限的数组，取各点
          mask 有效观测去漂移后的算术均值。没有可用观测的点填为 ``0.0``；
          未达到 ``target_valid_count`` 的点也保留当前均值，是否接受该低
          有效次数结果由调用方决定。
        - ``stage2_valid_mask`` 形状为 ``(...,)``，表示对应点截至当前累计扫描
          是否至少有一次有效观测，可以作为 ``delta C`` 的来源。始终没有有效
           观测的点标记为 ``False``。未达到目标次数但已有可用结果的点仍标记
           为 ``True``。

    说明:
        对二维非共线网格，每次扫描只用该次 mask 有效的点拟合并扣除完整二维
        仿射漂移场（线性变换和平移共 6 个参数）；如果某次扫描的有效点不足
        以拟合完整二维仿射，则直接报错。对于整个输入本来就是单行等共线点
        集的情况，仍只分别扣除 X、Y 残差均值。各点的 ``delta C`` 是其 mask
        有效观测去漂移后的算术均值，不执行任何基于数值分布的时间维异常值
        剔除：匹配读数近似按像素量化时，MAD 类判据会把相邻档位的正常读数
        误判为异常，异常观测的识别完全由调用方的逐扫描 mask 负责。每个点
        的停止条件是其有效观测数达到 ``target_valid_count``；整体收敛只对
        至少有一次有效观测的点判定，始终无效的点（例如持续失配区）不阻塞
        收敛，其 ``delta C`` 为 ``0.0`` 并在 ``stage2_valid_mask`` 中标记为
        无效。调用方可以在外部扫描上限到达时接受当前结果，也可以继续采集。
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
    if isinstance(target_valid_count, (bool, np.bool_)) or not isinstance(
        target_valid_count, (int, np.integer)
    ):
        raise TypeError("target_valid_count 必须是整数")
    if target_valid_count < 1:
        raise ValueError("target_valid_count 必须是正整数")

    scan_count = values.shape[0]

    finite_values = np.all(np.isfinite(values), axis=-1)
    masks_shape = (scan_count,) + values.shape[1:-1]
    if masks is None:
        raise ValueError(
            "阶段2必须显式提供 masks；每次扫描都需要对应的有效点 mask"
        )
    scan_masks = _prepare_boolean_mask(masks, masks_shape, "masks")
    if np.any(scan_masks & ~finite_values):
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
    # 无效点不参与拟合，也不进入后续跨扫描统计；数值占位使用 0，是否参与
    # 统计完全由 scan_valid 控制。
    drift_removed = np.zeros(values.shape, dtype=float)
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

    # delta C 取各点 mask 有效观测去漂移后的算术均值，沿最前面的扫描序号维
    # 统计。不执行任何基于数值分布的时间维异常值剔除：匹配读数近似按像素
    # 量化时，MAD 类判据会把相邻档位的正常读数误判为异常，异常观测的识别
    # 完全由调用方的逐扫描 mask 负责。np.divide 的 where 防止 0 个有效观测
    # 时触发除零；对应位置填 0，有效性由 stage2_valid_mask 表示。
    observation_count = np.sum(scan_valid, axis=0)
    observation_sum = np.sum(
        np.where(scan_valid[..., None], drift_removed, 0.0),
        axis=0,
    )
    residual_table = np.divide(
        observation_sum,
        observation_count[..., None],
        out=np.zeros(values.shape[1:], dtype=float),
        where=observation_count[..., None] > 0,
    )
    # 有效点定义：截至当前至少有一次有效观测。始终无效的点（例如持续失配
    # 区）没有可用的 delta C，数值保持为 0 并标记为无效。
    stage2_valid_mask = observation_count > 0

    # point_precision_reached 是“是否达到目标有效次数”的严格判定，用于决定
    # 是否还要继续扫描；stage2_valid_mask 只表示“是否存在可用于 delta C 的
    # 结果”。调用方负责累计扫描历史并在外部决定是否强制停止。
    point_precision_reached = observation_count >= target_valid_count
    # 整体收敛只对有效点集判定：始终无效的点无法通过增加扫描获得观测，
    # 不能阻塞整体收敛。
    precision_reached = bool(
        np.all(point_precision_reached[stage2_valid_mask])
    )

    if precision_reached:
        return False, residual_table, stage2_valid_mask
    # 外部程序决定何时达到扫描上限；本函数只报告仍有点未达到目标有效次数。
    return True, residual_table, stage2_valid_mask


def _remove_x_group_bias(
    correction_table: np.ndarray,
    valid_mask: np.ndarray,
    x_group_size: int,
) -> np.ndarray:
    """按 X 列序号 mod N 分组，把各组有效点的平均值统一到组平均值的均值。

    最后一个网格维视为 X 列方向；X/Y 两个分量分别独立修正。组平均值只
    使用 ``valid_mask=True`` 的点，修正也只作用于这些点；没有有效点的组
    不参与统计并发出 ``RuntimeWarning``。
    """
    grid_shape = correction_table.shape[:-1]
    x_count = grid_shape[-1]
    leading_count = 1
    for size in grid_shape[:-1]:
        leading_count *= size
    table = correction_table.reshape(leading_count, x_count, 2)
    valid_grid = valid_mask.reshape(leading_count, x_count)
    column_group = np.arange(x_count) % x_group_size

    group_means = np.zeros((x_group_size, 2))
    group_counts = np.zeros(x_group_size, dtype=np.int64)
    for group_index in range(x_group_size):
        columns = column_group == group_index
        selected = valid_grid[:, columns]
        group_counts[group_index] = int(np.sum(selected))
        if group_counts[group_index] > 0:
            group_means[group_index] = np.sum(
                table[:, columns, :] * selected[..., None], axis=(0, 1)
            ) / group_counts[group_index]

    nonempty_groups = group_counts > 0
    if not np.any(nonempty_groups):
        return correction_table
    if not np.all(nonempty_groups):
        warnings.warn(
            f"x_group_size={x_group_size}，但沿 X 方向有 "
            f"{int(np.sum(~nonempty_groups))} 个组没有任何有效点，"
            "这些组不参与本次平均值修正。",
            RuntimeWarning,
            stacklevel=3,
        )
    grand_mean = np.mean(group_means[nonempty_groups], axis=0)

    column_offsets = np.zeros((x_count, 2))
    for group_index in range(x_group_size):
        if nonempty_groups[group_index]:
            column_offsets[column_group == group_index] = (
                grand_mean - group_means[group_index]
            )
    corrected = table + np.where(
        valid_grid[..., None], column_offsets[None, :, :], 0.0
    )
    return corrected.reshape(correction_table.shape)


def combine_correction_tables(
    initial_correction: np.ndarray,
    residual_table: np.ndarray | None,
    stage1_mask: np.ndarray,
    stage2_valid_mask: np.ndarray | None = None,
    x_group_size: int = 1,
) -> tuple[np.ndarray, np.ndarray]:
    """合并阶段1初始表和阶段2残差表，并生成插值有效 mask。

    参数:
        initial_correction: Python 侧阶段2使用的完整初始 map ``C0``，形状为
            ``(..., 2)``。它应当已经通过
            :func:`process_first_measurement` 的 ``fill_value`` 填补无效点，
            因而所有分量都必须是有限数值。此处不要求、也不执行设备下发方向
            的整体取负；调用方在真正下发时按设备约定转换。
        residual_table: 阶段2输出的 ``delta C``，形状必须与
            ``initial_correction`` 相同。``process_stage2_residuals`` 即使某些
            点未达到目标有效观测次数时也会返回当前均值；调用方必须自行决定
            是否接受低于目标次数的结果。显式传入 ``None`` 仍会报错。无效点的
            数值通常为 ``0.0``，不能仅根据数值判断有效性。
        stage1_mask: 阶段1原始有效点 mask，形状为
            ``initial_correction.shape[:-1]``。``True`` 表示该点的 ``C0``
            来自有效的阶段1测量；``False`` 表示 ``C0`` 是填充值。
        stage2_valid_mask: 必须提供的阶段2结果有效点 mask，形状为
            ``initial_correction.shape[:-1]``。``True`` 表示该点的 ``delta C``
            来自至少一次 mask 有效的观测，可以参与合并；
            ``False`` 表示阶段2没有可用结果。目标有效观测次数未达标但已有
            结果的点仍可以为 ``True``；该 mask 是判断阶段2结果是否可合并的
            唯一依据。
        x_group_size: 沿 X 方向的分组数 N，默认 1 表示不分组、不做修正。
            大于 1 时，在合并完成后把最终表的最后一个网格维视为 X 列方向，
            列序号 mod N 相同的点为一组（共 N 组）；对 X/Y 两个分量分别
            计算各组有效点（``interpolation_mask=True``）的平均值，再以 N 个
            组平均值的等权平均值为目标，把每组有效点整体平移，使各组平均值
            都等于该目标值。用于扣除测量数据中沿 X 方向按 N 点周期出现的
            系统性组间偏差；无效点保持 ``0.0``，不参与统计。调用方需要保证
            列序号与数据的 X 分组相位一致，例如每行按 X 排序的点依次放入
            列 ``0, 1, 2, ...``。

    返回:
        ``(final_correction, interpolation_mask)``：

        - ``final_correction``：阶段2有效点使用
          ``C_final = C0 + delta C``；阶段2无效但阶段1有效的点保留
          ``C0``。阶段1和阶段2都无效的点置为 ``0.0``，并由
          ``interpolation_mask`` 标记为不可用。``x_group_size > 1`` 时，
          有效点在此基础上按组平移，使各组平均值等于组平均值的等权平均值；
          无效点仍为 ``0.0``。该结果仍保持 Python 侧 map 符号，设备下发时
          的符号转换由调用方处理。
        - ``interpolation_mask``：后续插值可使用的源点 mask，定义为
          ``stage1_mask | stage2_valid_mask``。阶段1和阶段2都无效的点在
          ``final_correction`` 中为 ``0.0``，不会参与后续插值。
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
            "initial_correction 必须是完整有限的 Python 侧 map"
        )
    if residual_table is None:
        raise ValueError("residual_table 不能为 None；阶段2尚未形成 delta C")
    if stage2_valid_mask is None:
        raise ValueError(
            "stage2_valid_mask 必须提供；不能根据 deltaC 的数值判断阶段2有效性"
        )

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
    stage2_valid = _prepare_boolean_mask(
        stage2_valid_mask,
        initial_values.shape[:-1],
        "stage2_valid_mask",
    )
    if isinstance(x_group_size, (bool, np.bool_)) or not isinstance(
        x_group_size, (int, np.integer)
    ):
        raise TypeError("x_group_size 必须是整数")
    if x_group_size < 1:
        raise ValueError("x_group_size 必须是正整数")
    delta_finite = np.all(np.isfinite(delta_values), axis=-1)
    if np.any(stage2_valid & ~delta_finite):
        raise ValueError(
            "stage2_valid_mask=True 的 residual_table 点必须包含有限数值"
        )

    final_correction = initial_values.copy()
    final_correction[stage2_valid] = (
        initial_values[stage2_valid] + delta_values[stage2_valid]
    )
    interpolation_mask = stage1_valid | stage2_valid
    final_correction[~interpolation_mask] = 0.0
    if x_group_size > 1:
        final_correction = _remove_x_group_bias(
            final_correction, interpolation_mask, int(x_group_size)
        )
    return final_correction, interpolation_mask

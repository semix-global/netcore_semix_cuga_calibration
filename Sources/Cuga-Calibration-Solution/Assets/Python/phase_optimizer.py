"""
贝叶斯优化器——单一函数接口，供外部硬件程序调用。

优化器直接处理两类搜索变量，保持与旧版本相同的归一化表示：

* 前 ``n_phase`` 维是参考频率相位差的归一化表示，范围为 [0, 1]；
  调用方将其解码为 ``phase_ref = 2π * x_phase``。
* 后 ``n_normal`` 维是归一化普通变量（当前用于幅值锚点），范围为 [0, 1]。

相位差的参考频率以及从相位差换算为延时的工作由外部程序完成。例如，
参考频率为 ``f_ref`` 时，``delay = x_phase / f_ref``。

典型用法::

    import phase_optimizer as po

    cost = None
    while True:
        result = po.suggest(
            cost,
            n_phase=n_electrodes - 1,
            n_normal=n_amplitude_anchors,
        )
        if result["done"]:
            break
        physical_parameters = decode(
            result["x_phase"], result["x_normal"]
        )
        cost = hardware(physical_parameters)
"""

import os
import pickle
import time

import numpy as np
from sklearn.base import clone
from skopt import Optimizer
from skopt.learning import GaussianProcessRegressor
from skopt.learning.gaussian_process.kernels import ConstantKernel, Matern
from skopt.space import Real


# 状态文件放在调用程序的当前工作目录。整个公开接口刻意只保留 suggest()，
# 因而优化器、历史观测、待测点和早停计数都必须通过这个文件跨调用保存。
_STATE_FILE = "optimizer_state.pkl"
_STATE_VERSION = 3

# 相位变量保持旧版本的归一化表示；本版本只改变 GP 的距离模型。
_PHASE_LOWER_BOUND = 0.0
_PHASE_UPPER_BOUND = 1.0

# 普通变量目前是归一化幅值锚点。
_NORMAL_LOWER_BOUND = 0.0
_NORMAL_UPPER_BOUND = 1.0

# GP 核超参数的优化比较昂贵。前 50 次拟合每次优化，之后每 5 次优化一次；
# 每 25 次再用多个起点进行一次较完整的校准，兼顾速度和长期稳定性。
_GP_EARLY_OPTIMIZE_FITS = 50
_GP_OPTIMIZE_INTERVAL = 5
_GP_FULL_RESTART_INTERVAL = 25

# 采集函数先从 5000 个候选点中筛选，再选择 3 个起点做 L-BFGS 局部搜索。
# 这两个参数影响“寻找下一点”的耗时，不改变已经采集的 GP 训练集数量。
_ACQ_N_POINTS = 5000
_ACQ_N_RESTARTS = 3

# 只对送入 GP 的代价设置动态范围上限。原始历史、best_cost 和最终结果
# 始终保留真实值，避免极端低分把代理模型的主要分辨率全部占用。
_MODEL_COST_SPAN = 30.0


class CalibratedNoiseGaussianProcessRegressor(GaussianProcessRegressor):
    """校正归一化噪声，并限制极端差 cost 对代理模型的影响。

    sklearn 会先把观测 y 标准化，但 ``alpha`` 不会自动跟随缩放。本类在
    每次 fit 前仅对送入 GP 的 cost 截断到 ``best + model_cost_span``，再按
    截断后历史 cost 的总体方差换算手测噪声：

        alpha_normalized = score_noise_variance / var(model_y)

    Optimizer 的原始 yi、历史最佳、早停和日志不受截断影响。
    """

    def __init__(
        self,
        kernel=None,
        score_noise_variance=1e-4,
        optimizer="fmin_l_bfgs_b",
        n_restarts_optimizer=0,
        normalize_y=True,
        copy_X_train=True,
        random_state=None,
        model_cost_span=_MODEL_COST_SPAN,
    ):
        self.score_noise_variance = score_noise_variance
        self.model_cost_span = model_cost_span
        super().__init__(
            kernel=kernel,
            alpha=1e-10,
            optimizer=optimizer,
            n_restarts_optimizer=n_restarts_optimizer,
            normalize_y=normalize_y,
            copy_X_train=copy_X_train,
            random_state=random_state,
            noise=None,
        )

    def fit(self, X, y):
        # skopt 以“最小化 cost”为约定，因此当前最优值是 min(y)。截断上界
        # 等于 best + span：正常优质样本不变，只有远差于最优的样本被压平。
        y_array = np.asarray(y, dtype=float)
        if self.model_cost_span is None:
            model_y = y_array.copy()
            self.model_cost_ceiling_ = None
        else:
            model_cost_span = float(self.model_cost_span)
            if not np.isfinite(model_cost_span) or model_cost_span <= 0.0:
                raise ValueError("model_cost_span 必须是有限正数或 None")
            self.model_cost_ceiling_ = float(np.min(y_array) + model_cost_span)
            model_y = np.minimum(y_array, self.model_cost_ceiling_)

        # 保存一份实际用于拟合的 y，便于测试和诊断截断是否按预期发生；
        # 它不是 Optimizer.yi，因而不会覆盖外部可见的原始观测历史。
        self.model_y_train_ = model_y.copy()

        # normalize_y=True 时 sklearn 内部会把 y 除以其标准差，而 alpha 的
        # 单位必须与标准化后的 y 一致，所以测得的原始噪声方差也要除以
        # var(y)。若 y 几乎为常数，则不缩放，避免除以接近零的数。
        y_variance = float(np.var(model_y))
        if self.normalize_y and y_variance > np.finfo(float).eps:
            normalized_noise = self.score_noise_variance / y_variance
        else:
            normalized_noise = self.score_noise_variance
        # 极小 jitter 只用于矩阵数值稳定，不代表测量噪声。
        self.alpha = max(float(normalized_noise), 0.0) + 1e-10
        return super().fit(X, model_y)


class MixedPeriodicMatern(Matern):
    """兼容旧方案的混合 Matérn 5/2 核。

    当 ``n_periodic_dimensions`` 大于 0 时，前若干维使用周期距离；当前
    ``suggest`` 固定传入 0，因此实际优化只使用普通线性距离。每一维仍有
    独立 length_scale，可继续通过最大似然做 ARD 参数敏感度学习。
    """

    def __init__(
        self,
        length_scale=1.0,
        length_scale_bounds=(1e-5, 1e5),
        nu=2.5,
        n_periodic_dimensions=0,
    ):
        self.n_periodic_dimensions = n_periodic_dimensions
        super().__init__(
            length_scale=length_scale,
            length_scale_bounds=length_scale_bounds,
            nu=nu,
        )

    def _validated_length_scale(self, n_features):
        # 标量 length_scale 表示所有维度共享尺度；向量表示 ARD，每个参数
        # 拥有独立尺度。统一展开为向量可简化后续距离和梯度计算。
        length_scale = np.asarray(self.length_scale, dtype=float)
        if length_scale.ndim == 0:
            return np.full(n_features, float(length_scale)), True
        if length_scale.shape != (n_features,):
            raise ValueError(
                "各向异性 length_scale 的数量必须与输入维度一致"
            )
        return length_scale, False

    def _distance_components(self, X, Y, length_scale):
        # 仅在核超参数优化需要梯度时创建三维数组。最后一维保存每个输入维
        # 对总平方距离的贡献，随后可直接得到对 log(length_scale) 的导数。
        n_x, n_y = X.shape[0], Y.shape[0]
        components = np.empty((n_x, n_y, X.shape[1]), dtype=float)
        for dimension in range(X.shape[1]):
            delta = X[:, dimension, None] - Y[None, :, dimension]
            if dimension < self.n_periodic_dimensions:
                # 周期为 1 的圆周弦距离。delta=0 和 delta=±1 时距离都为 0，
                # 所以归一化相位 0 与 1 在 GP 看来是同一个物理相位。
                component = 2.0 * np.sin(np.pi * delta)
            else:
                # 幅值锚点等普通变量不首尾相接，继续使用欧氏线性距离。
                component = delta
            components[:, :, dimension] = (
                component / length_scale[dimension]
            ) ** 2
        return components

    def __call__(self, X, Y=None, eval_gradient=False):
        # sklearn 在训练时通常以 Y=None 调用，并可能请求核超参数梯度；
        # 预测交叉协方差时传入 Y，此时 sklearn 约定不计算超参数梯度。
        X = np.atleast_2d(np.asarray(X, dtype=float))
        if self.nu != 2.5:
            raise ValueError("MixedPeriodicMatern 当前仅支持 nu=2.5")
        if not 0 <= self.n_periodic_dimensions <= X.shape[1]:
            raise ValueError("周期维度数必须位于 0 与输入维度之间")

        if Y is None:
            Y_array = X
        else:
            if eval_gradient:
                raise ValueError("Y 非空时不能计算超参数梯度")
            Y_array = np.atleast_2d(np.asarray(Y, dtype=float))
            if Y_array.shape[1] != X.shape[1]:
                raise ValueError("X 与 Y 的输入维度必须一致")

        length_scale, isotropic = self._validated_length_scale(X.shape[1])
        components = None
        if eval_gradient:
            components = self._distance_components(X, Y_array, length_scale)
            dist_sq = np.sum(components, axis=2)
        else:
            # 预测时不需要每维超参数梯度，逐维累加可避免创建
            # (n_x, n_y, n_dim) 大数组，降低采集候选批量预测的内存。
            dist_sq = np.zeros((X.shape[0], Y_array.shape[0]), dtype=float)
            for dimension in range(X.shape[1]):
                delta = X[:, dimension, None] - Y_array[None, :, dimension]
                if dimension < self.n_periodic_dimensions:
                    component = 2.0 * np.sin(np.pi * delta)
                else:
                    component = delta
                dist_sq += (component / length_scale[dimension]) ** 2
        dist = np.sqrt(dist_sq)
        sqrt_5_dist = np.sqrt(5.0) * dist
        exp_term = np.exp(-sqrt_5_dist)

        # Matérn nu=5/2 的闭式表达式。相比 RBF，它允许目标函数不完全光滑，
        # 更适合带测量噪声以及局部变化较快的硬件响应。
        kernel = (1.0 + sqrt_5_dist + (5.0 / 3.0) * dist_sq) * exp_term

        if not eval_gradient:
            return kernel
        if self.hyperparameter_length_scale.fixed:
            return kernel, np.empty((*kernel.shape, 0))

        # 对 log(length_scale) 求导，与 sklearn 核的 theta 约定一致。
        factor = (5.0 / 3.0) * (1.0 + sqrt_5_dist) * exp_term
        if isotropic:
            gradient = (factor * dist_sq)[:, :, None]
        else:
            assert components is not None
            gradient = factor[:, :, None] * components
        return kernel, gradient

    def gradient_x(self, x, X_train):
        """计算 K(x, X_train) 对原始归一化输入 x 的梯度。"""
        # 该梯度供 skopt 的 L-BFGS 采集函数优化使用，方向是候选点 x，
        # 不同于 __call__ 中对核超参数 log(length_scale) 的梯度。
        x = np.asarray(x, dtype=float).reshape(-1)
        X_train = np.atleast_2d(np.asarray(X_train, dtype=float))
        if X_train.shape[1] != x.size:
            raise ValueError("x 与 X_train 的输入维度必须一致")
        if self.nu != 2.5:
            raise ValueError("MixedPeriodicMatern 当前仅支持 nu=2.5")
        if not 0 <= self.n_periodic_dimensions <= x.size:
            raise ValueError("周期维度数必须位于 0 与输入维度之间")

        length_scale, _ = self._validated_length_scale(x.size)
        delta = x[None, :] - X_train
        scaled = np.empty_like(delta)
        periodic = self.n_periodic_dimensions
        if periodic:
            # 周期维先映射到单位圆弦长；普通维保持线性差值。
            scaled[:, :periodic] = (
                2.0 * np.sin(np.pi * delta[:, :periodic])
                / length_scale[:periodic]
            )
        scaled[:, periodic:] = (
            delta[:, periodic:] / length_scale[periodic:]
        )
        dist = np.sqrt(np.sum(scaled**2, axis=1))
        common = (
            -(5.0 / 3.0)
            * (1.0 + np.sqrt(5.0) * dist)
            * np.exp(-np.sqrt(5.0) * dist)
        )

        # 周期距离求导会产生 sin(2πdelta)；线性距离求导则保留 delta。
        # 当候选点与训练点重合时二者都自然趋于 0，不需要单独除以 dist。
        gradient = np.empty_like(delta)
        if periodic:
            gradient[:, :periodic] = (
                common[:, None]
                * 2.0
                * np.pi
                * np.sin(2.0 * np.pi * delta[:, :periodic])
                / (length_scale[:periodic] ** 2)
            )
        gradient[:, periodic:] = (
            common[:, None]
            * delta[:, periodic:]
            / (length_scale[periodic:] ** 2)
        )
        return gradient


def _coerce_dimension_count(value, name):
    """把参数数量校验并转换为非负 Python 整数。"""
    if isinstance(value, bool):
        raise ValueError(f"{name} 必须是非负整数")
    try:
        integer = int(value)
    except (TypeError, ValueError, OverflowError):
        raise ValueError(f"{name} 必须是非负整数") from None
    if integer != value or integer < 0:
        raise ValueError(f"{name} 必须是非负整数")
    return integer


def _resolve_phase_count(n_phase, n_periodic):
    """解析新的相位参数名，并兼容旧的 ``n_periodic`` 调用。"""
    if n_phase is not None and n_periodic is not None:
        raise ValueError("n_phase 与兼容参数 n_periodic 不能同时指定")
    return n_phase if n_phase is not None else n_periodic


def _resolve_initial_dimensions(n_phase, n_normal, n_periodic=None):
    """解析首次调用的相位参数和普通参数数量。"""
    n_phase = _resolve_phase_count(n_phase, n_periodic)
    if n_phase is None or n_normal is None:
        raise ValueError("首次调用必须同时指定 n_phase 和 n_normal")

    n_phase = _coerce_dimension_count(n_phase, "n_phase")
    n_normal = _coerce_dimension_count(n_normal, "n_normal")

    total = n_phase + n_normal
    if total <= 0:
        raise ValueError("n_phase 与 n_normal 不能同时为 0")
    return n_phase, n_normal, total


def _state_dimension_counts(state):
    """读取状态中的相位参数和普通参数数量。"""
    try:
        n_phase = _coerce_dimension_count(
            state["n_phase"], "状态中的 n_phase"
        )
        n_normal = _coerce_dimension_count(
            state["n_normal"], "状态中的 n_normal"
        )
    except KeyError:
        raise RuntimeError("优化状态缺少参数分类信息") from None
    total = n_phase + n_normal
    if total <= 0:
        raise RuntimeError("优化状态中的总参数数量无效")
    return n_phase, n_normal, total


def _validate_resume_dimensions(
    state, n_phase, n_normal, n_periodic=None
):
    """校验恢复调用给出的维度，允许调用方全部省略。"""
    state_phase, state_normal, _ = _state_dimension_counts(state)
    supplied_phase = _resolve_phase_count(n_phase, n_periodic)

    if supplied_phase is not None:
        supplied_phase = _coerce_dimension_count(
            supplied_phase, "n_phase"
        )
        if supplied_phase != state_phase:
            raise ValueError(
                "n_phase 与状态中的相位参数数量不一致；"
                "不能混用不同参数布局的优化状态。"
            )
    if n_normal is not None:
        supplied_normal = _coerce_dimension_count(n_normal, "n_normal")
        if supplied_normal != state_normal:
            raise ValueError(
                "n_normal 与状态中的普通参数数量不一致；"
                "不能混用不同参数布局的优化状态。"
            )

def _make_aod_kernel(
    n_periodic_dimensions, n_normal_dimensions, length_scale=None
):
    """创建兼容旧接口的混合核。

    当前 ``suggest`` 始终以 ``n_periodic_dimensions=0`` 调用，使所有
    优化变量都使用普通线性距离。保留该类和工厂函数，便于读取旧代码和
    对核实现本身做独立回归测试。
    """
    n_periodic_dimensions = _coerce_dimension_count(
        n_periodic_dimensions, "n_periodic_dimensions"
    )
    n_normal_dimensions = _coerce_dimension_count(
        n_normal_dimensions, "n_normal_dimensions"
    )
    n_dim = n_periodic_dimensions + n_normal_dimensions
    if n_dim <= 0:
        raise ValueError("周期维和普通维不能同时为 0")

    if length_scale is None:
        length_scale = np.ones(n_dim)
    else:
        length_scale = np.asarray(length_scale, dtype=float)
        if length_scale.shape != (n_dim,):
            raise ValueError("length_scale 的数量必须与总参数数量一致")

    # ConstantKernel 学习整体信号幅度；MixedPeriodicMatern 学习样本间相关性。
    # 每维独立 length_scale（ARD）反映该维变化多远会显著改变 cost。
    return ConstantKernel(1.0, (0.01, 1000.0)) * MixedPeriodicMatern(
        length_scale=length_scale,
        length_scale_bounds=[(0.01, 100.0)] * n_dim,
        nu=2.5,
        n_periodic_dimensions=n_periodic_dimensions,
    )


def _save_state(state):
    """原子保存状态，避免写入中断后损坏上一份可用状态。"""
    # 先完整写入临时文件，再用同目录原子替换覆盖正式文件。若进程在
    # pickle 写入中途退出，旧的 optimizer_state.pkl 仍然保持完整。
    tmp_file = f"{_STATE_FILE}.tmp"
    with open(tmp_file, "wb") as file_obj:
        pickle.dump(state, file_obj, protocol=pickle.HIGHEST_PROTOCOL)
    # Windows 上杀毒软件、索引器或只读监控可能短暂占用目标文件。
    # 原子替换遇到共享冲突时退避重试，避免长时间优化因瞬时占用丢失。
    for attempt in range(7):
        try:
            os.replace(tmp_file, _STATE_FILE)
            return
        except PermissionError:
            if attempt == 6:
                raise
            time.sleep(0.05 * (2 ** attempt))


def _load_state():
    """载入优化状态。"""
    # pickle 只能载入可信的本地文件；该状态文件不应接收外部来源内容。
    with open(_STATE_FILE, "rb") as file_obj:
        state = pickle.load(file_obj)

    if not isinstance(state, dict):
        raise RuntimeError("优化状态格式无效")
    if state.get("state_version") != _STATE_VERSION:
        raise RuntimeError(
            "optimizer_state.pkl 是旧版或不兼容的优化状态；"
            "请备份后删除该状态文件，再按新的相位变量定义重新开始。"
        )
    return state


def _gp_fit_policy(fit_number):
    """返回第 ``fit_number`` 次 GP 拟合采用的核参数优化策略。"""
    if fit_number <= 0:
        raise ValueError("fit_number 必须是正整数")

    # 首次以及周期校准时从多个起点搜索，降低长期困在局部最优的风险。
    if fit_number == 1 or fit_number % _GP_FULL_RESTART_INTERVAL == 0:
        return "fmin_l_bfgs_b", 2

    # 早期每轮暖启动优化；稳定后每隔若干轮微调一次核参数。
    if (fit_number <= _GP_EARLY_OPTIMIZE_FITS
            or fit_number % _GP_OPTIMIZE_INTERVAL == 0):
        return "fmin_l_bfgs_b", 0

    # 仍用全部历史数据精确更新 GP 后验，仅暂时固定核参数。
    return None, 0


def _configure_gp_fit(opt, n_initial):
    """在本轮 tell 前按拟合序号配置核参数优化节奏。"""
    # tell() 将把当前结果加入历史，因此用 len(Xi)+1 计算加入后的观测数。
    # Sobol 阶段尚未拟合 GP；达到 n_initial 后才把第一次拟合编号为 1。
    observations_after_tell = len(opt.Xi) + 1
    if observations_after_tell < n_initial:
        return None

    fit_number = observations_after_tell - n_initial + 1
    optimizer, n_restarts = _gp_fit_policy(fit_number)
    opt.base_estimator_.optimizer = optimizer
    opt.base_estimator_.n_restarts_optimizer = n_restarts
    return fit_number


def _compact_optimizer(opt):
    """保留最新核参数，但不把完整拟合模型写入状态文件。"""
    # skopt 的 models 会包含训练数据、核矩阵分解等缓存，随迭代增加会让
    # 状态文件迅速膨胀。这里只把最新学到的 kernel_ 移回基础估计器。
    if opt.models:
        latest_model = opt.models[-1]
        if hasattr(latest_model, "kernel_"):
            # 下一轮 clone(base_estimator_) 会从这组参数暖启动。
            opt.base_estimator_.kernel = clone(latest_model.kernel_)
    # 下一建议点已缓存在 Optimizer 且另外保存为 last_x；恢复后收到 cost
    # 时本来就会重新拟合，因此无需持久化 O(N^2) 的 Cholesky 缓存。
    opt.models.clear()


def _dedup(opt, x, max_attempts=10):
    """若建议点与已评估点重合，在各维搜索边界内加入小扰动。"""
    # 重复测量通常不会增加空间信息，还会浪费一次约 20 秒以上的硬件采样。
    # 这里只处理数值上几乎完全重合的点，不强制设置较大的最小点间距。
    candidate = np.asarray(x, dtype=float)
    if not hasattr(opt, "Xi") or len(opt.Xi) == 0:
        return candidate.tolist()

    evaluated = np.asarray(opt.Xi, dtype=float)
    rng = getattr(opt, "rng", np.random)
    bounds = np.asarray(opt.space.bounds, dtype=float)
    lower_bounds = bounds[:, 0]
    upper_bounds = bounds[:, 1]
    spans = upper_bounds - lower_bounds

    for _ in range(max_attempts):
        distances = np.linalg.norm(evaluated - candidate, axis=1)
        if np.all(distances >= 1e-6):
            return candidate.tolist()
        candidate = candidate + rng.uniform(-0.01, 0.01, candidate.shape) * spans
        candidate = np.clip(candidate, lower_bounds, upper_bounds)

    raise RuntimeError("连续多次生成重复建议点，请检查搜索空间或历史状态")


def _split_parameter_values(values, n_phase):
    """把内部的一维参数向量拆成相位段和普通段。"""
    if values is None:
        return None, None
    flat_values = [float(value) for value in values]
    return flat_values[:n_phase], flat_values[n_phase:]


def _format_result(state, x, done=False):
    """格式化分段结果。"""
    # 显式转为内置 float/bool，便于调用方做 JSON 序列化或跨进程传输。
    n_phase, _, _ = _state_dimension_counts(state)
    x_values = [float(value) for value in x]
    x_phase, x_normal = _split_parameter_values(x_values, n_phase)
    best_x = state["best_x"]
    best_phase, best_normal = _split_parameter_values(
        best_x, n_phase
    )
    best_cost = state["best_cost"]
    return {
        "x_phase": x_phase,
        "x_normal": x_normal,
        "best_x_phase": best_phase,
        "best_x_normal": best_normal,
        # 兼容旧调用方：这些键名保留，但其值仍是旧版本的归一化相位变量，
        # 不再表示使用周期核的变量。
        "x_periodic": x_phase,
        "best_x_periodic": best_phase,
        "best_cost": (
            None if not np.isfinite(best_cost) else float(best_cost)
        ),
        "done": bool(done),
    }


def _resolve_early_stop(value, n_dim):
    """解析早停配置；auto 按维度自适应，None 表示显式禁用。"""
    # 默认 20×维度只统计 Sobol 阶段之后的“无显著改善”次数。这里返回
    # 具体整数并存入状态，保证恢复运行时不会因调用参数变化而改变规则。
    if isinstance(value, str):
        if value != "auto":
            raise ValueError("n_early_stop 字符串仅支持 'auto'")
        return 20 * int(n_dim)
    if value is None:
        return None
    if isinstance(value, bool) or int(value) != value or int(value) <= 0:
        raise ValueError("n_early_stop 必须是正整数、'auto' 或 None")
    return int(value)


def suggest(cost=None, n_periodic=None, n_normal=None, n_initial=None,
            noise=None, n_early_stop="auto", acq_func="LCB", *,
            n_phase=None):
    """
    输入上一次实验的 cost，返回下一组相位段和普通段参数。

    参数:
         cost: 上一次实验的代价；首次调用或恢复待测点时传 None。
         n_periodic: 兼容旧调用的参数名，表示相位参数数量；不再启用周期核。
         n_phase: 相位参数数量。建议使用此名称；首次调用时须与 n_normal
                  一起指定；可为 0。不能与 n_periodic 同时指定。
         n_normal: 普通参数数量。首次调用时须与相位参数数量一起指定；可为 0。
         n_initial: Sobol 初始采样点数；None 时按 2×总维度（向上取 2 的幂）自动计算。
        noise: 实测 cost/score 的噪声方差，仅首次调用使用；默认 1e-4。
               其平方根用于判断改善是否显著；同时在每次 GP 拟合时按
               历史 score 方差换算到 normalize_y=True 的归一化尺度，作为
               固定观测噪声使用，不再由 GP 自由估计。
        n_early_stop: 贝叶斯搜索阶段连续无显著改善的早停步数；默认
                      "auto" = 20×维度；None 显式禁用。
        acq_func: skopt 采集函数，常用值为 "LCB"、"EI"、"PI"。

    返回:
         包含 x_phase、x_normal、best_x_phase、best_x_normal、best_cost、done
         的字典。x_phase 和 x_normal 均位于 [0,1]；x_phase 由调用方解码为
         参考频率下的相位弧度。
         为兼容旧调用方，同时返回 x_periodic 和 best_x_periodic；这两个键
         只是旧名称，不代表周期变量。

        若进程重启后仍有一个待评估点，传入 cost=None 会再次返回同一点，
        不会静默跳过该次实验。
    """
    # 接口采用“建议点 -> 外部测量 -> 反馈 cost”的单步状态机：
    # 1. 首次 suggest(None, ...) 创建状态并返回两类参数；
    # 2. 调用方测量两类参数，把结果作为下一次 suggest(cost) 的输入；
    # 3. 函数 tell() 旧点、更新最优和早停，再 ask() 并保存新点。
    # cost 越小越好；若外部使用“score 越大越好”，应传入 -score。
    state_exists = os.path.exists(_STATE_FILE)

    # ---- 首次调用：创建 Optimizer 并返回第一个采样点 ----
    if not state_exists:
        n_phase, n_normal, n_dimensions = _resolve_initial_dimensions(
            n_phase, n_normal, n_periodic
        )
        if n_initial is None:
            # 缺省时按总维度自适应：2×总维度，向上取 2 的幂。
            n_initial = 1 << (2 * n_dimensions - 1).bit_length()
        else:
            if isinstance(n_initial, bool) or int(n_initial) != n_initial \
                    or int(n_initial) <= 0:
                raise ValueError("n_initial 必须是正整数")
            n_initial = int(n_initial)
        configured_early_stop = _resolve_early_stop(
            n_early_stop, n_dimensions
        )

        measurement_noise = 1e-4 if noise is None else float(noise)
        if not np.isfinite(measurement_noise) or measurement_noise < 0:
            raise ValueError("noise 必须是有限的非负方差")

        dimensions = (
            [
                Real(_PHASE_LOWER_BOUND, _PHASE_UPPER_BOUND)
                for _ in range(n_phase)
            ]
            + [
                Real(_NORMAL_LOWER_BOUND, _NORMAL_UPPER_BOUND)
                for _ in range(n_normal)
            ]
        )
        # 与旧版本保持相同的归一化坐标和初始长度尺度；唯一的 GP 建模
        # 变化是把相位维也按普通线性距离处理，而不是周期距离。
        kernel = _make_aod_kernel(0, n_dimensions)

        # normalize_y 消除不同实验 cost 量级对核优化的影响；自定义 GP 会把
        # 外部给出的噪声方差同步换算到归一化后的 y 尺度。
        gp = CalibratedNoiseGaussianProcessRegressor(
            kernel=kernel,
            score_noise_variance=measurement_noise,
            normalize_y=True,
            n_restarts_optimizer=2,
        )
        opt = Optimizer(
            dimensions=dimensions,
            base_estimator=gp,
            n_initial_points=n_initial,
            acq_func=acq_func,
            acq_optimizer="lbfgs",
            acq_optimizer_kwargs={
                "n_points": _ACQ_N_POINTS,
                "n_restarts_optimizer": _ACQ_N_RESTARTS,
                "n_jobs": 1,
            },
            initial_point_generator="sobol",
            # Sobol 序列先做均匀、低差异的空间探索；收集满 n_initial 个点后，
            # Optimizer 才开始依据 GP 和采集函数建议后续参数。
            # 拟合后会提取 kernel_ 并清空 models；这里再限制队列作为保护。
            model_queue_size=1,
        )
        x = _dedup(opt, opt.ask())
        state = {
            "state_version": _STATE_VERSION,
            # opt 内含 Xi/yi、随机数状态和下一点缓存，是恢复优化所需的主体。
            "opt": opt,
            "n_phase": n_phase,
            "n_normal": n_normal,
            "n_initial": n_initial,
            "step": 1,
            # last_x 是已经交给硬件、但尚未通过 cost 确认完成的待测点。
            "last_x": list(x),
            "best_x": None,
            "best_cost": float("inf"),
            "no_improve": 0,
            "noise": measurement_noise,
            # 早停用标准差而不是方差：改善量超过一个噪声标准差才算显著。
            "sigma_noise": float(np.sqrt(measurement_noise)),
            "n_early_stop": configured_early_stop,
            "done": False,
        }
        _save_state(state)
        return _format_result(state, x, done=False)

    # ---- 后续调用：载入状态并校验本次调用 ----
    state = _load_state()
    opt = state["opt"]
    # 提取最新模型的 kernel_，以便下一次拟合使用同一套暖启动参数。
    _compact_optimizer(opt)
    _validate_resume_dimensions(state, n_phase, n_normal, n_periodic)

    if n_early_stop != "auto":
        _, _, n_dimensions = _state_dimension_counts(state)
        supplied_early_stop = _resolve_early_stop(
            n_early_stop, n_dimensions
        )
        if supplied_early_stop != state["n_early_stop"]:
            raise ValueError(
                f"n_early_stop={supplied_early_stop} 与首次调用保存的 "
                f"n_early_stop={state['n_early_stop']} 不一致。"
            )

    if noise is not None:
        supplied_noise = float(noise)
        if not np.isfinite(supplied_noise) or supplied_noise < 0:
            raise ValueError("noise 必须是有限的非负方差")
        if not np.isclose(supplied_noise, state["noise"]):
            raise ValueError(
                f"noise={supplied_noise} 与首次调用保存的 noise="
                f"{state['noise']} 不一致。继续优化时请省略 noise，或传相同值。"
            )

    # 已完成的状态可以安全重复查询，不会再次 tell 同一个结果。
    if state.get("done", False):
        return _format_result(state, state["best_x"], done=True)

    # 进程恢复但尚未取得 last_x 的实验结果时，重新返回同一个待测点。
    if cost is None:
        return _format_result(state, state["last_x"], done=False)

    # ---- 反馈上一次实验结果 ----
    cost_val = float(cost)
    if not np.isfinite(cost_val):
        raise ValueError("cost 必须是有限数值")

    _configure_gp_fit(opt, state["n_initial"])
    # tell() 将 last_x 与本次 cost 成对加入 Xi/yi；不能在同一个 cost 上
    # 重复调用，否则会把一次硬件测量错误地记录成多次观测。
    opt.tell(state["last_x"], cost_val)
    old_best = state["best_cost"]

    # 真实历史最优与“显著改善”判定分开处理。
    if cost_val < old_best:
        state["best_cost"] = cost_val
        state["best_x"] = list(state["last_x"])

    if old_best - cost_val > state["sigma_noise"]:
        state["no_improve"] = 0
    elif state["step"] > state["n_initial"]:
        # Sobol 初始探索不计入早停，以免 GP 尚未开始工作就提前结束。
        state["no_improve"] += 1

    # 早停前必须先持久化最后一次 tell() 及 best/no_improve 更新。
    if (state["n_early_stop"] is not None
            and state["no_improve"] >= state["n_early_stop"]
            and state["best_x"] is not None):
        _compact_optimizer(opt)
        state["opt"] = opt
        state["done"] = True
        _save_state(state)
        return _format_result(state, state["best_x"], done=True)

    # ---- 请求并保存下一个建议点 ----
    # 必须先保存 last_x 再返回给调用方；若程序在硬件测量期间重启，下一次
    # cost=None 会重新给出同一点，从而保证测量结果不会与错误参数配对。
    x = _dedup(opt, opt.ask())
    _compact_optimizer(opt)
    state["opt"] = opt
    state["last_x"] = list(x)
    state["step"] += 1
    _save_state(state)

    return _format_result(state, x, done=False)

"""
贝叶斯优化器——单一函数接口，供外部硬件程序调用。

优化器只处理归一化变量，每一维都位于 [0, 1]。相位、幅值、scale、
slope 等物理量应由外部程序按各自规则解码。

典型用法::

    import phase_optimizer as po

    cost = None
    while True:
        result = po.suggest(cost, n_dim=9)
        if result["done"]:
            break
        physical_parameters = decode(result["x"])
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


_STATE_FILE = "optimizer_state.pkl"
_LOWER_BOUND = 0.0
_UPPER_BOUND = 1.0
_GP_EARLY_OPTIMIZE_FITS = 50
_GP_OPTIMIZE_INTERVAL = 5
_GP_FULL_RESTART_INTERVAL = 25
_ACQ_N_POINTS = 5000
_ACQ_N_RESTARTS = 3
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
        self.model_y_train_ = model_y.copy()

        y_variance = float(np.var(model_y))
        if self.normalize_y and y_variance > np.finfo(float).eps:
            normalized_noise = self.score_noise_variance / y_variance
        else:
            normalized_noise = self.score_noise_variance
        # 极小 jitter 只用于矩阵数值稳定，不代表测量噪声。
        self.alpha = max(float(normalized_noise), 0.0) + 1e-10
        return super().fit(X, model_y)


class MixedPeriodicMatern(Matern):
    """相位维使用周期距离、其余维使用线性距离的 Matérn 5/2 核。

    归一化相位的周期固定为 1，因此 0 与 1 表示同一相位。每一维仍有
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
        length_scale = np.asarray(self.length_scale, dtype=float)
        if length_scale.ndim == 0:
            return np.full(n_features, float(length_scale)), True
        if length_scale.shape != (n_features,):
            raise ValueError(
                "各向异性 length_scale 的数量必须与输入维度一致"
            )
        return length_scale, False

    def _distance_components(self, X, Y, length_scale):
        n_x, n_y = X.shape[0], Y.shape[0]
        components = np.empty((n_x, n_y, X.shape[1]), dtype=float)
        for dimension in range(X.shape[1]):
            delta = X[:, dimension, None] - Y[None, :, dimension]
            if dimension < self.n_periodic_dimensions:
                component = 2.0 * np.sin(np.pi * delta)
            else:
                component = delta
            components[:, :, dimension] = (
                component / length_scale[dimension]
            ) ** 2
        return components

    def __call__(self, X, Y=None, eval_gradient=False):
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


def _make_aod_kernel(n_dim, length_scale=None):
    """创建 AOD 混合核；最后5维为非周期幅值锚点。"""
    if length_scale is None:
        length_scale = np.ones(n_dim)
    return ConstantKernel(1.0, (0.01, 1000.0)) * MixedPeriodicMatern(
        length_scale=length_scale,
        length_scale_bounds=[(0.01, 100.0)] * n_dim,
        nu=2.5,
        n_periodic_dimensions=max(0, n_dim - 5),
    )


def _save_state(state):
    """原子保存状态，避免写入中断后损坏上一份可用状态。"""
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
    with open(_STATE_FILE, "rb") as file_obj:
        state = pickle.load(file_obj)

    if not isinstance(state, dict):
        raise RuntimeError("优化状态格式无效")
    # 兼容曾经写入版本号的旧文件；当前状态不再做版本管理。
    state.pop("state_version", None)
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
    if opt.models:
        latest_model = opt.models[-1]
        if hasattr(latest_model, "kernel_"):
            # 下一轮 clone(base_estimator_) 会从这组参数暖启动。
            opt.base_estimator_.kernel = clone(latest_model.kernel_)
    # 下一建议点已缓存在 Optimizer 且另外保存为 last_x；恢复后收到 cost
    # 时本来就会重新拟合，因此无需持久化 O(N^2) 的 Cholesky 缓存。
    opt.models.clear()


def _apply_optimizer_speed_settings(opt):
    """让新旧状态统一采用当前的采集搜索预算。"""
    opt.acq_optimizer = "lbfgs"
    opt.n_points = _ACQ_N_POINTS
    opt.n_restarts_optimizer = _ACQ_N_RESTARTS
    opt.n_jobs = 1
    opt.max_model_queue_size = 1
    # 旧检查点中的估计器没有这个属性；补齐后 sklearn.clone 才能继续工作。
    opt.base_estimator_.model_cost_span = _MODEL_COST_SPAN


def _ensure_mixed_periodic_kernel(opt, n_dim):
    """让旧检查点在下一次拟合时也使用混合周期核。"""
    kernel = getattr(opt.base_estimator_, "kernel", None)
    matern = getattr(kernel, "k2", None)
    if isinstance(matern, MixedPeriodicMatern):
        return
    if not isinstance(matern, Matern):
        return

    mixed = MixedPeriodicMatern(
        length_scale=np.asarray(matern.length_scale, dtype=float).copy(),
        length_scale_bounds=matern.length_scale_bounds,
        nu=matern.nu,
        n_periodic_dimensions=max(0, n_dim - 5),
    )
    opt.base_estimator_.kernel = clone(kernel.k1) * mixed


def _dedup(opt, x, max_attempts=10):
    """若建议点与已评估点重合，在 [0,1] 内反复加入小扰动。"""
    candidate = np.asarray(x, dtype=float)
    if not hasattr(opt, "Xi") or len(opt.Xi) == 0:
        return candidate.tolist()

    evaluated = np.asarray(opt.Xi, dtype=float)
    rng = getattr(opt, "rng", np.random)

    for _ in range(max_attempts):
        distances = np.linalg.norm(evaluated - candidate, axis=1)
        if np.all(distances >= 1e-6):
            return candidate.tolist()
        candidate = candidate + rng.uniform(-0.01, 0.01, candidate.shape)
        candidate = np.clip(candidate, _LOWER_BOUND, _UPPER_BOUND)

    raise RuntimeError("连续多次生成重复建议点，请检查搜索空间或历史状态")


def _format_result(state, x, done=False):
    """把 numpy 标量统一转换为便于外部程序使用的 Python 标量。"""
    best_x = state["best_x"]
    best_cost = state["best_cost"]
    return {
        "x": [float(value) for value in x],
        "best_cost": (
            None if not np.isfinite(best_cost) else float(best_cost)
        ),
        "best_x": (
            [float(value) for value in best_x]
            if best_x is not None else None
        ),
        "done": bool(done),
    }


def _resolve_early_stop(value, n_dim):
    """解析早停配置；auto 按维度自适应，None 表示显式禁用。"""
    if isinstance(value, str):
        if value != "auto":
            raise ValueError("n_early_stop 字符串仅支持 'auto'")
        return 20 * int(n_dim)
    if value is None:
        return None
    if isinstance(value, bool) or int(value) != value or int(value) <= 0:
        raise ValueError("n_early_stop 必须是正整数、'auto' 或 None")
    return int(value)


def suggest(cost=None, n_dim=None, n_initial=None, noise=None,
            n_early_stop="auto", acq_func="LCB"):
    """
    输入上一次实验的 cost，返回下一组归一化参数。

    参数:
        cost: 上一次实验的代价；首次调用或恢复待测点时传 None。
        n_dim: 搜索空间维度；首次调用必须指定，后续可省略。
        n_initial: Sobol 初始采样点数；None 时按 2×n_dim（向上取 2 的幂）自动计算。
        noise: 实测 cost/score 的噪声方差，仅首次调用使用；默认 1e-4。
               其平方根用于判断改善是否显著；同时在每次 GP 拟合时按
               历史 score 方差换算到 normalize_y=True 的归一化尺度，作为
               固定观测噪声使用，不再由 GP 自由估计。
        n_early_stop: 贝叶斯搜索阶段连续无显著改善的早停步数；默认
                      "auto" = 20×维度；None 显式禁用。
        acq_func: skopt 采集函数，常用值为 "LCB"、"EI"、"PI"。

    返回:
        包含 x、best_cost、best_x、done 的字典。

        x 始终位于 [0,1]^n。若进程重启后仍有一个待评估点，传入
        cost=None 会再次返回同一点，不会静默跳过该次实验。
    """
    state_exists = os.path.exists(_STATE_FILE)

    # ---- 首次调用：创建 Optimizer 并返回第一个采样点 ----
    if not state_exists:
        if n_dim is None:
            raise ValueError("首次调用必须指定 n_dim（维度数）")
        if isinstance(n_dim, bool) or int(n_dim) != n_dim or int(n_dim) <= 0:
            raise ValueError("n_dim 必须是正整数")
        n_dim = int(n_dim)
        if n_initial is None:
            # 缺省时按维度自适应：2×n_dim，向上取 2 的幂。
            n_initial = 1 << (2 * n_dim - 1).bit_length()
        else:
            if isinstance(n_initial, bool) or int(n_initial) != n_initial \
                    or int(n_initial) <= 0:
                raise ValueError("n_initial 必须是正整数")
            n_initial = int(n_initial)
        configured_early_stop = _resolve_early_stop(n_early_stop, n_dim)

        measurement_noise = 1e-4 if noise is None else float(noise)
        if not np.isfinite(measurement_noise) or measurement_noise < 0:
            raise ValueError("noise 必须是有限的非负方差")

        dimensions = [Real(_LOWER_BOUND, _UPPER_BOUND) for _ in range(n_dim)]
        kernel = _make_aod_kernel(n_dim)
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
            # 拟合后会提取 kernel_ 并清空 models；这里再限制队列作为保护。
            model_queue_size=1,
        )
        x = _dedup(opt, opt.ask())
        state = {
            "opt": opt,
            "n_dim": n_dim,
            "n_initial": n_initial,
            "step": 1,
            "last_x": list(x),
            "best_x": None,
            "best_cost": float("inf"),
            "no_improve": 0,
            "noise": measurement_noise,
            "sigma_noise": float(np.sqrt(measurement_noise)),
            "n_early_stop": configured_early_stop,
            "done": False,
        }
        _save_state(state)
        return _format_result(state, x, done=False)

    # ---- 后续调用：载入状态并校验本次调用 ----
    state = _load_state()
    opt = state["opt"]
    _apply_optimizer_speed_settings(opt)
    # 旧状态可能仍保存着完整模型；提取其 kernel_ 后即可使用同一套暖启动。
    _compact_optimizer(opt)
    _ensure_mixed_periodic_kernel(opt, state["n_dim"])

    if n_early_stop != "auto":
        supplied_early_stop = _resolve_early_stop(n_early_stop, state["n_dim"])
        if supplied_early_stop != state["n_early_stop"]:
            raise ValueError(
                f"n_early_stop={supplied_early_stop} 与首次调用保存的 "
                f"n_early_stop={state['n_early_stop']} 不一致。"
            )

    if n_dim is not None:
        if isinstance(n_dim, bool) or int(n_dim) != n_dim:
            raise ValueError("n_dim 必须是正整数")
        if int(n_dim) != state["n_dim"]:
            raise ValueError(
                f"n_dim={int(n_dim)} 与状态中的 n_dim={state['n_dim']} 不一致；"
                "不能混用不同维度的优化状态。"
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
    opt.tell(state["last_x"], cost_val)
    old_best = state["best_cost"]

    # 真实历史最优与“显著改善”判定分开处理。
    if cost_val < old_best:
        state["best_cost"] = cost_val
        state["best_x"] = list(state["last_x"])

    if old_best - cost_val > state["sigma_noise"]:
        state["no_improve"] = 0
    elif state["step"] > state["n_initial"]:
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
    x = _dedup(opt, opt.ask())
    _compact_optimizer(opt)
    state["opt"] = opt
    state["last_x"] = list(x)
    state["step"] += 1
    _save_state(state)

    return _format_result(state, x, done=False)

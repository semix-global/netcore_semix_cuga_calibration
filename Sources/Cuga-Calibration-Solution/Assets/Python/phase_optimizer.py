"""
贝叶斯优化器——单一函数接口，供外部硬件程序调用。

优化器只处理归一化变量，每一维都位于 [0, 1]。相位、幅值、scale、
slope 等物理量应由外部程序按各自规则解码。

典型用法::

    import phase_optimizer as po

    cost = None
    while True:
        result = po.suggest(cost, n_dim=15)
        if result["done"]:
            break
        physical_parameters = decode(result["x"])
        cost = hardware(physical_parameters)
"""

import os
import pickle

import numpy as np
from skopt import Optimizer
from skopt.learning import GaussianProcessRegressor
from skopt.learning.gaussian_process.kernels import ConstantKernel, Matern
from skopt.space import Real


_STATE_VERSION = 2
_STATE_FILE = "optimizer_state.pkl"
_LOWER_BOUND = 0.0
_UPPER_BOUND = 1.0


def _save_state(state):
    """原子保存状态，避免写入中断后损坏上一份可用状态。"""
    tmp_file = f"{_STATE_FILE}.tmp"
    with open(tmp_file, "wb") as file_obj:
        pickle.dump(state, file_obj, protocol=pickle.HIGHEST_PROTOCOL)
    os.replace(tmp_file, _STATE_FILE)


def _load_state():
    """载入并校验当前版本的优化状态。"""
    with open(_STATE_FILE, "rb") as file_obj:
        state = pickle.load(file_obj)

    if not isinstance(state, dict) or state.get("state_version") != _STATE_VERSION:
        raise RuntimeError(
            "优化状态版本不兼容。新版搜索空间为 [0,1]，不能续用旧状态；"
            "请保留旧文件作备份，并重新开始一次优化。"
        )
    return state


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


def suggest(cost=None, n_dim=None, n_initial=32, noise=None,
            n_early_stop=25, acq_func="LCB"):
    """
    输入上一次实验的 cost，返回下一组归一化参数。

    参数:
        cost: 上一次实验的代价；首次调用或恢复待测点时传 None。
        n_dim: 搜索空间维度；首次调用必须指定，后续可省略。
        n_initial: Sobol 初始采样点数。
        noise: 实测 cost 的噪声方差，仅首次调用使用；默认 1e-4。
               其平方根用于判断改善是否显著。GP 内部的 Gaussian noise
               由数据自行拟合，避免与 normalize_y=True 的尺度混用。
        n_early_stop: 贝叶斯搜索阶段连续无显著改善的早停步数；None 禁用。
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
        if isinstance(n_initial, bool) or int(n_initial) != n_initial \
                or int(n_initial) <= 0:
            raise ValueError("n_initial 必须是正整数")
        n_initial = int(n_initial)

        measurement_noise = 1e-4 if noise is None else float(noise)
        if not np.isfinite(measurement_noise) or measurement_noise < 0:
            raise ValueError("noise 必须是有限的非负方差")

        dimensions = [Real(_LOWER_BOUND, _UPPER_BOUND) for _ in range(n_dim)]
        kernel = ConstantKernel(1.0, (0.01, 1000.0)) * Matern(
            length_scale=np.ones(n_dim),
            length_scale_bounds=[(0.01, 100.0)] * n_dim,
            nu=2.5,
        )
        gp = GaussianProcessRegressor(
            kernel=kernel,
            noise="gaussian",
            normalize_y=True,
            n_restarts_optimizer=2,
        )
        opt = Optimizer(
            dimensions=dimensions,
            base_estimator=gp,
            n_initial_points=n_initial,
            acq_func=acq_func,
            initial_point_generator="sobol",
            # ask() 只依赖最新模型；限制队列可防止 pickle 随迭代次数暴涨。
            model_queue_size=1,
        )
        x = _dedup(opt, opt.ask())
        state = {
            "state_version": _STATE_VERSION,
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
            "done": False,
        }
        _save_state(state)
        return _format_result(state, x, done=False)

    # ---- 后续调用：载入状态并校验本次调用 ----
    state = _load_state()
    opt = state["opt"]

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
    if (n_early_stop is not None
            and state["no_improve"] >= n_early_stop
            and state["best_x"] is not None):
        state["opt"] = opt
        state["done"] = True
        _save_state(state)
        return _format_result(state, state["best_x"], done=True)

    # ---- 请求并保存下一个建议点 ----
    x = _dedup(opt, opt.ask())
    state["opt"] = opt
    state["last_x"] = list(x)
    state["step"] += 1
    _save_state(state)

    return _format_result(state, x, done=False)

"""
贝叶斯优化器、幅值解码与评分接口，供外部硬件程序调用。

优化器直接处理两类按固定顺序排列的搜索变量：

* 前 ``n_phase`` 维保留历史字段名称，范围为 [0, 1]；当前调用方用于
  延时控制，实际延时范围与换算由外部程序负责。
* 后 ``n_normal`` 维是归一化普通变量（当前用于幅值控制量），范围为 [0, 1]；
  内部状态和 ``suggest()`` 返回值均保留这些原始普通参数，不在优化器层
  解释为锚点或执行其他幅值变换。当前四参数单 Sigmoid 曲线由
  ``single_sigmoid_amplitude_curve()`` 单独解码。

典型用法::

    import phase_optimizer as po

    cost = None
    while True:
        result = po.suggest(
            cost,
            n_electrodes - 1,
            4,
        )
        if result["done"]:
            break
        delay_parameters = result["x_phase"]
        normal_parameters = result["x_normal"]
        amplitude = po.single_sigmoid_amplitude_curve(
            normal_parameters, measured_frequencies
        )
        physical_parameters = decode(delay_parameters, amplitude)
        efficiencies = hardware(physical_parameters)
        score = po.calculate_score(
            measured_frequencies, efficiencies, "bandwidth", threshold, epsilon
        )
        cost = -score
"""

import os
import pickle
import time
import warnings
from typing import Iterable

import numpy as np
from scipy.linalg import cholesky, cho_solve
from scipy.optimize import minimize
from scipy.spatial.distance import pdist, squareform
from scipy.special import expit
from scipy.stats import qmc
from sklearn.base import clone
from sklearn.utils import check_array
from skopt import Optimizer
from skopt.learning import GaussianProcessRegressor
from skopt.learning.gaussian_process.kernels import ConstantKernel, Matern, Product
from skopt.space import Real


# 状态文件放在调用程序的当前工作目录。优化器状态接口刻意只保留
# suggest()；因此优化器、历史观测、待测点和早停计数都必须通过这个文件跨调用保存。
_STATE_FILE = "optimizer_state.pkl"

# 保留历史相位字段；当前调用方将这组 [0,1] 参数用于延时控制。
_PHASE_LOWER_BOUND = 0.0
_PHASE_UPPER_BOUND = 1.0

# 普通变量是归一化幅值控制量；suggest() 对外原样返回 [0,1] 搜索坐标。
_NORMAL_LOWER_BOUND = 0.0
_NORMAL_UPPER_BOUND = 1.0

# 单 Sigmoid 的中心覆盖输入频段；10%--90% 过渡宽度按频段跨度的
# 1%--100% 对数映射。正下限避免零宽度，不代表硬件分辨率。
_SINGLE_SIGMOID_MIN_WIDTH_FRACTION = 0.01
_SINGLE_SIGMOID_FACTOR = 2.0 * np.log(9.0)

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
# 始终保留真实值；线性效率 score 的正常范围约为 1，因此这里用 1.0
# 作为极端差点的代理模型截断跨度。
_MODEL_COST_SPAN = 1.0

# 新任务的停止判据。第六个位置参数继续兼容整数窗口，也可传 (0,1)
# 内的容差；不会增加外部接口参数。实际配置随任务保存，恢复时不读新默认值。
_STOP_TOLERANCE = 0.01
_STOP_WINDOW = 100
_STOP_INTERVAL = 10
_STOP_CONFIRMATIONS = 3
_STOP_CONFIDENCE = 0.0
# 停止潜力与预测异常检查各自使用系数；降低潜力上界时不收紧异常检查。
_STOP_VALIDATION_CONFIDENCE = 3.0
_STOP_CANDIDATES = 4096
_STOP_LOCAL_STARTS = 3
_STOP_VALIDATION_WINDOW = 100
_STOP_MIN_PREDICTIONS = 25
_STOP_MAX_OUTLIER_FRACTION = 0.10


def _resolve_stop_options(value, n_dim):
    """保留旧整数/auto/None入口；小于1的正数表示score绝对容差。"""
    if not isinstance(value, (str, bool, np.bool_)) and value is not None:
        try:
            number = float(value)
        except (TypeError, ValueError, OverflowError):
            number = float("nan")
        if np.isfinite(number) and 0 < number < 1:
            return "tolerance", _STOP_WINDOW, number
    window = _resolve_early_stop(value, n_dim)
    return "window", window, _STOP_TOLERANCE


def _new_stopping_state(value, n_dim):
    kind, window, tolerance = _resolve_stop_options(value, n_dim)
    return {
        "rule": "gp_gap", "input_kind": kind, "window": window,
        "tolerance": tolerance, "interval": _STOP_INTERVAL,
        "confidence": _STOP_CONFIDENCE, "confirmations": _STOP_CONFIRMATIONS,
        "validation_confidence": _STOP_VALIDATION_CONFIDENCE,
        "candidate_points": _STOP_CANDIDATES, "local_starts": _STOP_LOCAL_STARTS,
        "validation_window": _STOP_VALIDATION_WINDOW,
        "minimum_predictions": _STOP_MIN_PREDICTIONS,
        "max_outlier_fraction": _STOP_MAX_OUTLIER_FRACTION,
        "search_seed": 20260930, "confirmation_count": 0,
        "pending_prediction": None, "prediction_errors": [], "checks": [],
    }


def _validate_stopping_argument(state, value, n_dim):
    if value == "auto":
        return
    stopping = state.get("stopping")
    if stopping is None:
        # 未带新判据的历史检查点继续原来的patience语义，不静默迁移。
        matches = _resolve_early_stop(value, n_dim) == state["n_early_stop"]
    else:
        kind, window, tolerance = _resolve_stop_options(value, n_dim)
        matches = kind == stopping["input_kind"]
        if kind == "tolerance":
            matches = matches and tolerance == stopping["tolerance"]
        else:
            matches = matches and window == stopping["window"]
    if not matches:
        raise ValueError("n_early_stop 与首次调用保存的停止配置不一致；继续时可省略。")


def _stop_predictions(model, points):
    """分批预测潜在cost，不叠加下一次观测的随机噪声。"""
    means, deviations = [], []
    for start in range(0, len(points), 512):
        mean, std = model.predict(points[start:start + 512], return_std=True)
        mean, std = np.asarray(mean), np.asarray(std)
        if (not np.all(np.isfinite(mean)) or not np.all(np.isfinite(std))
                or np.any(std < 0)):
            raise ValueError("停止检查遇到无效的GP预测")
        means.append(mean)
        deviations.append(std)
    return np.concatenate(means), np.concatenate(deviations)


def _remember_stop_prediction(state, model, point):
    stopping = state.get("stopping")
    if stopping is None or stopping["window"] is None or model is None:
        return
    pending = {"evaluation": len(state["opt"].Xi) + 1, "x": list(point)}
    try:
        mean, std = _stop_predictions(model, state["opt"].space.transform([point]))
        pending.update(mean=float(mean[0]), std=float(std[0]))
    except (ValueError, RuntimeError, np.linalg.LinAlgError, FloatingPointError) as error:
        pending["error"] = str(error)
    stopping["pending_prediction"] = pending


def _record_stop_prediction_error(state, cost):
    """使用实际测量前的预测，不以训练后残差冒充预测校准。"""
    stopping = state.get("stopping")
    if stopping is None or stopping["window"] is None:
        return
    pending = stopping.get("pending_prediction")
    stopping["pending_prediction"] = None
    if pending is None:
        return
    valid = ("error" not in pending
             and pending["evaluation"] == len(state["opt"].Xi) + 1
             and pending["x"] == list(state["last_x"]))
    if valid:
        scale = max(np.sqrt(pending["std"]**2 + state["noise"]), 1e-12)
        standardized = min(abs(cost - pending["mean"]) / scale, 1e12)
    else:
        standardized = 1e12
    errors = stopping["prediction_errors"]
    errors.append({"evaluation": len(state["opt"].Xi) + 1,
                   "standardized_error": float(standardized), "valid": bool(valid)})
    del errors[:-stopping["validation_window"]]


def _stop_search_points(state, seed):
    """独立Sobol候选、边界及已知好点；不消耗BO或全局NumPy随机状态。"""
    opt, stopping = state["opt"], state["stopping"]
    bounds = np.asarray(opt.space.transformed_bounds, dtype=float)
    low, high = bounds[:, 0], bounds[:, 1]
    dimensions = len(bounds)
    count = stopping["candidate_points"]
    unit = qmc.Sobol(dimensions, scramble=True, seed=seed).random_base2(
        int(np.ceil(np.log2(count)))
    )[:count]
    points = [low + unit * (high - low)]
    best = np.asarray(opt.space.transform([state["best_x"]])[0])
    faces = np.tile(best, (2 * dimensions, 1))
    for axis in range(dimensions):
        faces[2 * axis, axis] = low[axis]
        faces[2 * axis + 1, axis] = high[axis]
    points.extend([faces, np.asarray([low, high, (low + high) / 2, best])])
    if dimensions <= 10:
        corners = ((np.arange(2**dimensions)[:, None]
                    >> np.arange(dimensions)) & 1)
        points.append(low + corners * (high - low))
    top = np.argsort(opt.yi, kind="stable")[:32]
    points.append(opt.space.transform([opt.Xi[index] for index in top]))
    # 也复核本轮LCB/EI已经提出的下一点。
    if hasattr(opt, "_next_x"):
        points.append(opt.space.transform([opt._next_x]))
    return np.unique(np.vstack(points), axis=0), bounds


def _refine_stop_candidate(model, start, bounds, confidence, tolerance, retry=False):
    """有界重试只查询GP；始终保留搜索中已经找到的更乐观点。"""
    best_point = np.asarray(start, dtype=float).copy()
    best_value = float("inf")
    attempts = []

    def observe(point, gradients=False):
        nonlocal best_point, best_value
        point = np.asarray(point, dtype=float)
        if (point.shape != best_point.shape or not np.all(np.isfinite(point))
                or np.any(point < bounds[:, 0]-1e-10)
                or np.any(point > bounds[:, 1]+1e-10)):
            raise ValueError("停止搜索返回无效或越界的候选点")
        point = np.clip(point, bounds[:, 0], bounds[:, 1])
        if gradients:
            mean, std, mean_grad, std_grad = model.predict(
                point[None, :], return_std=True,
                return_mean_grad=True, return_std_grad=True)
            gradient = np.asarray(mean_grad) - confidence * np.asarray(std_grad)
            if not np.all(np.isfinite(gradient)):
                raise ValueError("停止检查遇到无效的采集梯度")
        else:
            mean, std = _stop_predictions(model, point[None, :])
        value = float(mean[0] - confidence * std[0])
        if not np.isfinite(value) or not np.isfinite(std[0]) or std[0] < 0:
            raise ValueError("停止搜索遇到无效的GP预测")
        if value < best_value:
            best_value, best_point = value, point.copy()
        return (value, gradient) if gradients else value

    observe(best_point)
    # 数值重试精度至少比用户的绝对停止容差细一万倍，且不粗于1e-7。
    accuracy = max(1e-12, min(1e-7, tolerance * 1e-4))
    relative_accuracy = accuracy / max(1., abs(best_value))
    stages = ([('L-BFGS-B', {'maxiter': 100, 'ftol': 1e-12})] if not retry else [
        ('L-BFGS-B', {'maxiter': 150, 'maxfun': 1500, 'maxls': 40,
                      'ftol': relative_accuracy}),
        ('Powell', {'maxiter': 100, 'maxfev': 1200, 'xtol': 1e-5,
                    'ftol': relative_accuracy})])
    for method, options in stages:
        attempt = {'method': method, 'retry': retry, 'success': False}
        try:
            gradients = method == 'L-BFGS-B'
            result = minimize(lambda point: observe(point, gradients), best_point.copy(),
                              method=method, jac=True if gradients else False,
                              bounds=bounds, options=options)
            # 对求解器报告的返回点独立复算；失败路径找到的好点也已保留。
            observe(result.x)
            attempt.update(success=bool(result.success), status=int(getattr(result, 'status', -1)),
                           message=str(getattr(result, 'message', '')),
                           iterations=int(getattr(result, 'nit', 0)),
                           evaluations=int(getattr(result, 'nfev', 0)))
        except (ValueError, RuntimeError, np.linalg.LinAlgError, FloatingPointError) as error:
            attempt['error'] = str(error)
        attempts.append(attempt)
        if attempt['success']:
            break
    return {'resolved': attempts[-1]['success'], 'point': best_point,
            'value': best_value, 'attempts': attempts}


def _estimate_stop_gap(state, model):
    """近似搜索cost的LCB；对应score乐观上界，不声称全域证书。"""
    stopping, opt = state["stopping"], state["opt"]
    b, tolerance = stopping["confidence"], stopping["tolerance"]
    validation_b = stopping.get("validation_confidence", b)
    best_point = opt.space.transform([state["best_x"]])
    mean_best, std_best = _stop_predictions(model, best_point)
    mean_best, std_best = float(mean_best[0]), float(std_best[0])
    numerical_slack = 1e-10 * max(1.0, abs(state["best_cost"]))
    if abs(mean_best - state["best_cost"]) > (
            validation_b * np.sqrt(std_best**2 + state["noise"]) + numerical_slack):
        return {"reliable": False, "reason": "incumbent_prediction_mismatch"}
    # noise>0时以潜在cost的保守上界评价实际要返回的点，防止含噪最高分虚高。
    incumbent_upper_cost = (state["best_cost"] if state["noise"] == 0
                            else mean_best + b * std_best)
    minimum_lcb = mean_best - b * std_best
    minimum_point = np.asarray(best_point[0])
    searched, searches = 0, []

    def keep(result):
        nonlocal minimum_lcb, minimum_point
        if result['value'] < minimum_lcb:
            minimum_lcb, minimum_point = result['value'], result['point'].copy()

    def diagnostics():
        return {'candidates': searched, 'search_rounds': round_number + 1,
                'search_revision': 'bounded_retry_v1', 'local_searches': searches,
                'local_retry_count': sum(int(a['retry']) for s in searches for a in s['attempts'])}

    # 第二组独立候选仅在初筛允许停止时计算；任一复核发现潜力都继续BO。
    for round_number in range(2):
        seed = (stopping["search_seed"] + 2 * len(opt.Xi) + round_number) % (2**32)
        candidates, bounds = _stop_search_points(state, seed)
        means, stds = _stop_predictions(model, candidates)
        values = means - b * stds
        order = np.argsort(values, kind="stable")
        if values[order[0]] < minimum_lcb:
            minimum_lcb = float(values[order[0]])
            minimum_point = candidates[order[0]]
        local_results = []
        for start_number, index in enumerate(order[:stopping["local_starts"]]):
            result = _refine_stop_candidate(model, candidates[index], bounds, b, tolerance)
            keep(result)
            detail = {'round': round_number + 1, 'start': start_number + 1,
                      'resolved': result['resolved'], 'attempts': result['attempts']}
            searches.append(detail)
            local_results.append((result, detail))
        searched += len(candidates)
        gap = float(incumbent_upper_cost - minimum_lcb)
        if gap < -numerical_slack:
            return {"reliable": False, "reason": "optimism_below_observed_best"}
        if gap > tolerance:
            break
        # 只有潜力已小到可能停止时才重试失败起点，避免无谓增加检查开销。
        for previous, detail in local_results:
            if previous['resolved']:
                continue
            retried = _refine_stop_candidate(model, previous['point'], bounds, b, tolerance, retry=True)
            keep(retried)
            detail['resolved'] = retried['resolved']
            detail['attempts'].extend(retried['attempts'])
            gap = float(incumbent_upper_cost - minimum_lcb)
            if gap > tolerance:
                break
        if gap > tolerance:
            break
        if not all(detail['resolved'] for _, detail in local_results):
            return {"reliable": False, "reason": "candidate_search_incomplete",
                    "gap": gap, **diagnostics()}
    return {"reliable": True, "gap": max(0.0, gap),
            "optimistic_score": float(-minimum_lcb),
            "incumbent_lower_score": float(-incumbent_upper_cost),
            "optimistic_x": opt.space.inverse_transform([minimum_point])[0],
            **diagnostics(),
            "bound_kind": "approximate_gp_gap"}


def _check_model_stopping(state, model):
    stopping, opt = state["stopping"], state["opt"]
    window = stopping["window"]
    if window is None:
        return False
    bo_observations = len(opt.yi) - state["n_initial"]
    if bo_observations < window or bo_observations % stopping["interval"]:
        return False
    gain = float(min(opt.yi[:-window]) - state["best_cost"])
    check = {"evaluation": len(opt.yi), "window_gain": gain, "interval": stopping["interval"],
             "tolerance": stopping["tolerance"], "confidence": stopping["confidence"],
             "validation_confidence": stopping.get("validation_confidence", stopping["confidence"]),
             "decision": "continue"}
    if gain > stopping["tolerance"]:
        check["reason"] = "recent_improvement"
    else:
        errors = stopping["prediction_errors"]
        outliers = sum(not item["valid"] or item["standardized_error"] > check["validation_confidence"]
                       for item in errors)
        check.update(predictions_checked=len(errors), prediction_outliers=outliers)
        if len(errors) < stopping["minimum_predictions"]:
            check["reason"] = "insufficient_prediction_checks"
        elif outliers / len(errors) > stopping["max_outlier_fraction"]:
            check["reason"] = "prediction_intervals_unreliable"
        elif model is None:
            check["reason"] = "model_unavailable"
        else:
            try:
                evidence = _estimate_stop_gap(state, model)
                check.update(evidence)
                if evidence["reliable"] and evidence["gap"] <= stopping["tolerance"]:
                    check["decision"] = "confirm"
                    check["reason"] = "small_improvement_and_gap"
                elif evidence["reliable"]:
                    check["reason"] = "remaining_potential"
            except (ValueError, RuntimeError, np.linalg.LinAlgError, FloatingPointError) as error:
                # 停止诊断失败只能继续优化，不能丢弃已测结果或退回patience停止。
                check.update(reason="diagnostic_error", error=str(error))
                warnings.warn(f"停止检查未通过，继续BO：{error}", RuntimeWarning)
    stopping["confirmation_count"] = (stopping["confirmation_count"] + 1
                                       if check["decision"] == "confirm" else 0)
    done = stopping["confirmation_count"] >= stopping["confirmations"]
    if done:
        check["decision"] = "stop"
        state["done_reason"] = "model_gap_converged"
    check["confirmation_count"] = stopping["confirmation_count"]
    stopping["last_check"] = check
    stopping["checks"].append(check)
    del stopping["checks"][:-20]
    return done


def calculate_score(
    frequencies: Iterable[float],
    efficiencies: Iterable[float],
    method: str = "legacy",
    threshold: float | None = None,
    epsilon: float | None = None,
    lam: float = 1.0,
    gamma: float = 0.5,
    /,
) -> float:
    """从频率及对应效率计算标量 score；越大越好，suggest() 接收 -score。

    固定位置参数顺序：frequencies, efficiencies, method, threshold,
    epsilon, lam, gamma。两数组必须一维、等长、至少两个点且全部有限。
    效率使用比值（60% 传 0.6），不自动转换百分数或功率。与旧评分一致，
    拒绝负效率，但不自动截断大于 1 的观测值。输入数组不会被修改。

    method="legacy"（默认）：原有线性效率评分
    mean(eta) - lam*std(eta) - gamma*(max(eta)-min(eta))。
    std 为总体标准差，频点等权；lam/gamma 为有限非负数，默认 1/0.5。
    threshold/epsilon 在本模式不使用。原 scoring.py 的接口继续保留。

    method="bandwidth"：必须显式传 threshold∈(0,1]、epsilon∈[0,1]。
    按频率配对排序，重复频率拒绝（重复测量应由调用方先汇总）。在相邻
    点间将效率作线性插值，F=max(f)-min(f)，B 是 eta>=threshold 的
    最长连续区间宽度，中间不达标的低谷会切断区间，不累加分离的频带。
    C = integral(min(eta(f)/threshold, 1), df)/F；先插值效率，再封顶积分，
    阈值交点处拆段，支持不等间隔频率。返回 (1-epsilon)*B/F+epsilon*C，
    范围 [0,1]。epsilon=0 只看带宽，epsilon=1 只看达标进度。
    lam/gamma 在本模式不使用。带宽仅在已测频域内估计，不向带外外推。

    本函数无优化状态副作用。一次优化中保持方法、参数和测量频段固定；
    更改评分应新开优化状态，noise 也应按新 score 的重复测量方差设置。
    """
    try:
        frequency_array = np.asarray(frequencies, dtype=float)
    except TypeError:
        frequency_array = np.asarray(list(frequencies), dtype=float)
    try:
        efficiency_array = np.asarray(efficiencies, dtype=float)
    except TypeError:
        efficiency_array = np.asarray(list(efficiencies), dtype=float)
    if (
        frequency_array.ndim != 1
        or efficiency_array.ndim != 1
        or frequency_array.size < 2
        or frequency_array.shape != efficiency_array.shape
    ):
        raise ValueError("frequencies 和 efficiencies 必须是一维等长数组，且至少有两个点")
    if not np.all(np.isfinite(frequency_array)):
        raise ValueError("frequencies 必须是有限数值")
    if not np.all(np.isfinite(efficiency_array)) or np.any(efficiency_array < 0.0):
        raise ValueError("efficiencies 必须是有限非负数，使用效率比值而非百分数")

    if method == "legacy":
        lam, gamma = float(lam), float(gamma)
        if not np.isfinite(lam) or lam < 0.0:
            raise ValueError("lam 必须是有限非负数")
        if not np.isfinite(gamma) or gamma < 0.0:
            raise ValueError("gamma 必须是有限非负数")
        return float(
            np.mean(efficiency_array)
            - lam * np.std(efficiency_array)
            - gamma * (np.max(efficiency_array) - np.min(efficiency_array))
        )
    if method != "bandwidth":
        raise ValueError("method 必须是 'legacy' 或 'bandwidth'")
    if threshold is None or epsilon is None:
        raise ValueError("bandwidth 模式必须显式提供 threshold 和 epsilon")
    threshold, epsilon = float(threshold), float(epsilon)
    if not np.isfinite(threshold) or not 0.0 < threshold <= 1.0:
        raise ValueError("threshold 必须是 (0,1] 内的有限效率比值")
    if not np.isfinite(epsilon) or not 0.0 <= epsilon <= 1.0:
        raise ValueError("epsilon 必须是 [0,1] 内的有限数值")

    order = np.argsort(frequency_array)
    frequencies_sorted = frequency_array[order]
    efficiencies_sorted = efficiency_array[order]
    if np.any(frequencies_sorted[1:] == frequencies_sorted[:-1]):
        raise ValueError("bandwidth 模式不接受重复频率，请先汇总重复测量")
    frequency_min = float(frequencies_sorted[0])
    span = float(frequencies_sorted[-1]) - frequency_min
    if not np.isfinite(span) or span <= 0.0:
        raise ValueError("频段跨度必须是有限正数")

    # 在 [0,1] 频率坐标上累积 B/F 和 C，避免 MHz/Hz 单位改变分数。
    positions = (frequencies_sorted - frequency_min) / span
    longest_width = current_width = progress = 0.0
    for left, right, y_left, y_right in zip(
        positions[:-1], positions[1:], efficiencies_sorted[:-1], efficiencies_sorted[1:]
    ):
        width = float(right - left)
        left_passes, right_passes = y_left >= threshold, y_right >= threshold
        if left_passes and right_passes:
            current_width += width
            progress += width
        elif not left_passes and not right_passes:
            current_width = 0.0
            progress += width * (y_left / threshold + y_right / threshold) / 2.0
        elif not left_passes:
            below_width = width * (threshold - y_left) / (y_right - y_left)
            current_width = width - below_width
            progress += below_width * (y_left / threshold + 1.0) / 2.0 + current_width
        else:
            above_width = width * (y_left - threshold) / (y_left - y_right)
            current_width += above_width
            progress += above_width + (width - above_width) * (1.0 + y_right / threshold) / 2.0
        longest_width = max(longest_width, current_width)
        if not right_passes:
            current_width = 0.0
    score = (1.0 - epsilon) * longest_width + epsilon * progress
    return float(np.clip(score, 0.0, 1.0))


def single_sigmoid_amplitude_curve(
    normal_parameters: Iterable[float],
    frequencies: Iterable[float],
    /,
) -> np.ndarray:
    """将四个 [0,1] 参数解码为低频低、高频高的单 Sigmoid 幅值数组。

    顺序为 ``[u_A_low, u_A_high, u_center, u_width]``。解码公式：

    * ``A_low = u_A_low``；
    * ``A_high = A_low + (1 - A_low) * u_A_high``；
    * ``B = max(frequencies) - min(frequencies)``；
    * ``C = min(frequencies) + B * u_center``；
    * ``W = B * 0.01 * 100**u_width``（对数宽度映射）；
    * ``A(f) = A_low + (A_high - A_low) * sigmoid(2*ln(9)*(f-C)/W)``。

    ``frequencies``、C 和 W 使用相同频率单位。W 是幅值变化
    的 10%--90% 过渡宽度，不是效率达标带宽。输出单调不减且在 [0,1]；
    高频增量为 0 或低平台为 1 时允许常值曲线，硬件 scale 由外部处理。

    范围由本次完整测量频率数组确定，不接受可选范围参数。宽度范围
    为频段跨度的 1%--100%，是内部搜索配置，不是硬件限制。同一优化
    任务须保持频段端点不变；只传子区间会重新解码曲线。乱序和重复
    频率可用，空数组返回空数组；非空数组需有至少两个不同频率，且
    跨度有限。返回一维 float 数组，与输入频率顺序一致。

    外部调用 suggest(None, n_phase, 4, ...) 取得原始 normal 参数，再调用
    本函数。更换参数数量、解码范围或模型后需新开优化状态；不能用新
    解码规则解释旧实验。延时变量、GP 和评分逻辑不由本函数处理。
    """
    try:
        parameters = np.asarray(normal_parameters, dtype=float)
    except TypeError:
        parameters = np.asarray(list(normal_parameters), dtype=float)
    if parameters.shape != (4,):
        raise ValueError("单 Sigmoid 幅值曲线需要 4 个一维 normal 参数")
    if (
        not np.all(np.isfinite(parameters))
        or np.any(parameters < 0.0)
        or np.any(parameters > 1.0)
    ):
        raise ValueError("单 Sigmoid normal 参数必须是 [0,1] 内的有限数值")

    try:
        frequency_array = np.asarray(frequencies, dtype=float)
    except TypeError:
        frequency_array = np.asarray(list(frequencies), dtype=float)
    if frequency_array.ndim != 1:
        raise ValueError("frequencies 必须是一维数组")
    if not np.all(np.isfinite(frequency_array)):
        raise ValueError("frequencies 必须是有限数值")

    if frequency_array.size == 0:
        return np.empty(0, dtype=float)
    frequency_min = float(np.min(frequency_array))
    bandwidth = float(np.max(frequency_array)) - frequency_min
    if not np.isfinite(bandwidth) or bandwidth <= 0.0:
        raise ValueError("frequencies 至少需要两个不同频率，且频段跨度必须有限")

    u_low, u_high, u_center, u_width = parameters
    low = u_low
    high = low + (1.0 - low) * u_high
    # 在归一化频率上求值，避免物理宽度乘法在极小跨度下下溢。
    normalized_frequency = (frequency_array - frequency_min) / bandwidth
    width_fraction = np.exp(
        (1.0 - u_width) * np.log(_SINGLE_SIGMOID_MIN_WIDTH_FRACTION)
    )
    z = (
        (normalized_frequency - u_center) / width_fraction
    ) * _SINGLE_SIGMOID_FACTOR
    return low + (high - low) * expit(z)


def _supports_fast_matern(kernel, X, y, alpha):
    """仅接受单输出、全可学习 Constant×ARD Matérn 3/2 或 5/2。

    严格类型检查避免绕过自定义核子类的计算语义；不支持的情况由原库处理。
    此检查不用 assert，Python 优化模式也保留回退判断。
    """
    if (type(kernel) is not Product
            or type(kernel.k1) is not ConstantKernel
            or type(kernel.k2) is not Matern):
        return False
    if (not isinstance(X, np.ndarray) or X.ndim != 2
            or not isinstance(y, np.ndarray) or y.ndim != 1
            or X.dtype != np.float64 or y.dtype != np.float64
            or X.shape[0] != y.shape[0]):
        return False
    if (kernel.k2.nu not in (1.5, 2.5)
            or kernel.k1.hyperparameter_constant_value.fixed
            or kernel.k2.hyperparameter_length_scale.fixed
            or kernel.n_dims != X.shape[1] + 1):
        return False
    lengths = np.atleast_1d(kernel.k2.length_scale)
    amplitude = kernel.k1.constant_value
    return (
        lengths.shape == (X.shape[1],)
        and np.all(np.isfinite(lengths)) and np.all(lengths > 0)
        and np.ndim(amplitude) == 0 and np.isfinite(amplitude) and amplitude > 0
        and np.ndim(alpha) == 0 and np.isfinite(alpha) and alpha >= 0
    )


class _Matern52DistanceCache:
    """仅在一次 fit 内复用未缩放的逐维距离，按严格上三角存储。"""

    def __init__(self, X):
        # 保存输入副本，以识别同形状数据替换、重排或 copy_X_train=False 下的修改。
        self.X = np.array(X, dtype=np.float64, copy=True)
        n_samples, n_dimensions = self.X.shape
        self.squared = np.empty(
            (n_dimensions, n_samples * (n_samples - 1) // 2), dtype=np.float64
        )
        for dimension in range(n_dimensions):
            pdist(self.X[:, dimension:dimension + 1], metric="sqeuclidean",
                  out=self.squared[dimension])

    def matches(self, X):
        return self.X.shape == X.shape and np.array_equal(self.X, X)

    def covariance(self, kernel):
        weights = 1.0 / np.atleast_1d(kernel.k2.length_scale)**2
        radius = weights @ self.squared
        radius *= 5.0
        np.sqrt(radius, out=radius)
        exponential = np.exp(-radius)
        amplitude = kernel.k1.constant_value
        condensed_K = amplitude * (1.0 + radius + radius**2 / 3.0) * exponential
        K = squareform(condensed_K)
        np.fill_diagonal(K, amplitude)
        common_gradient = (5.0 / 3.0) * amplitude * (radius + 1.0) * exponential
        return K, condensed_K, common_gradient, weights


class _Matern32DistanceCache(_Matern52DistanceCache):
    """复用上三角距离，按 Matérn 3/2 的解析式构造核及长度尺度梯度。"""

    def covariance(self, kernel):
        weights = 1.0 / np.atleast_1d(kernel.k2.length_scale)**2
        radius = weights @ self.squared
        radius *= 3.0
        np.sqrt(radius, out=radius)
        exponential = np.exp(-radius)
        amplitude = kernel.k1.constant_value
        condensed_K = amplitude * (1.0 + radius) * exponential
        K = squareform(condensed_K)
        np.fill_diagonal(K, amplitude)
        common_gradient = 3.0 * amplitude * exponential
        return K, condensed_K, common_gradient, weights


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
        # 只在本次拟合的梯度评价中保留距离缓存；成功或异常都释放，避免写入状态。
        self.__dict__.pop("_matern_distance_cache", None)
        self._matern_fit_active = True
        try:
            return super().fit(X, model_y)
        finally:
            self.__dict__.pop("_matern_distance_cache", None)
            self.__dict__.pop("_matern_fit_active", None)

    def log_marginal_likelihood(self, theta=None, eval_gradient=False, clone_kernel=True):
        """拟合期按条件加速似然梯度，其余调用保持原库的接口及计算行为。"""
        if (theta is None or not eval_gradient or self.optimizer is None
                or not getattr(self, "_matern_fit_active", False)
                or not _supports_fast_matern(
                    self.kernel_, self.X_train_, self.y_train_, self.alpha
                )):
            return super().log_marginal_likelihood(theta, eval_gradient, clone_kernel)

        if clone_kernel:
            kernel = self.kernel_.clone_with_theta(theta)
        else:
            kernel = self.kernel_
            kernel.theta = theta
        # 参数更新后再核对有限性，异常参数继续遵循原实现的处理。
        if _supports_fast_matern(kernel, self.X_train_, self.y_train_, self.alpha):
            try:
                if kernel.k2.nu == 1.5:
                    value, gradient = self._matern32_likelihood_gradient(kernel)
                else:
                    value, gradient = self._matern52_likelihood_gradient(kernel)
                if np.isfinite(value) and np.all(np.isfinite(gradient)):
                    return value, gradient
            except np.linalg.LinAlgError:
                # 舍入可能影响接近奇异的矩阵；用原算法在同一 theta、同一噪声下复核。
                # 原算法仍失败时保留其 -inf/零梯度语义，不在这里增加 jitter。
                pass
        return super().log_marginal_likelihood(theta, eval_gradient, clone_kernel)

    def _matern52_likelihood_gradient(self, kernel):
        """保留 5/2 的计算路径及已有验证工具使用的私有入口。"""
        return self._cached_matern_likelihood_gradient(kernel, _Matern52DistanceCache)

    def _matern32_likelihood_gradient(self, kernel):
        """使用 3/2 解析核及梯度，复用同一套似然矩阵运算。"""
        return self._cached_matern_likelihood_gradient(kernel, _Matern32DistanceCache)

    def _cached_matern_likelihood_gradient(self, kernel, cache_type):
        """直接累计对 log(constant)、log(length_scale) 的似然梯度。

        对照 sklearn 1.9.0 / skopt 0.10.2 验证；升级依赖需运行拟合回归检查。
        最终后验构造与预测仍使用原核，避免把专用缓存引入 kernel/pickle 接口。
        """
        cache = getattr(self, "_matern_distance_cache", None)
        if cache is None or not cache.matches(self.X_train_):
            cache = self._matern_distance_cache = cache_type(self.X_train_)
        K, condensed_K, common, weights = cache.covariance(kernel)
        K[np.diag_indices_from(K)] += self.alpha
        L = cholesky(K, lower=True, check_finite=False)
        y = self.y_train_[:, None]
        solved_y = cho_solve((L, True), y, check_finite=False)
        value = -0.5 * np.einsum("ik,ik->k", y, solved_y)
        value -= np.log(np.diag(L)).sum()
        value -= len(K) / 2 * np.log(2 * np.pi)

        inverse = cho_solve((L, True), np.eye(len(K)), check_finite=False)
        inner = np.outer(solved_y[:, 0], solved_y[:, 0])
        inner -= inverse
        diagonal_sum = np.trace(inner)
        # 合并两个三角的贡献，兼顾数值逆矩阵不完全对称的情况。
        inner += inner.T.copy()
        inner *= 0.5
        condensed_inner = squareform(inner, checks=False)
        gradient = np.empty(kernel.n_dims)
        gradient[0] = (np.dot(condensed_inner, condensed_K)
                       + 0.5 * kernel.k1.constant_value * diagonal_sum)
        gradient[1:] = (cache.squared @ (condensed_inner * common)) * weights
        return value.sum(axis=-1), gradient

    def predict(
        self,
        X,
        return_std=False,
        return_cov=False,
        return_mean_grad=False,
        return_std_grad=False,
    ):
        """沿用 skopt 的预测语义，仅优化后验方差的 einsum 收缩路径。"""
        # 本地覆盖 skopt 0.10.2 的后验标准差分支，不修改第三方库或全局 NumPy。
        # 均值、完整协方差、未拟合先验及其参数校验继续由父类处理。
        if not return_std or return_cov or not hasattr(self, "X_train_"):
            return super().predict(
                X,
                return_std=return_std,
                return_cov=return_cov,
                return_mean_grad=return_mean_grad,
                return_std_grad=return_std_grad,
            )

        X = check_array(X)
        if X.shape[0] != 1 and (return_mean_grad or return_std_grad):
            raise ValueError("Not implemented for n_samples > 1")

        cross = self.kernel_(X, self.X_train_)
        mean = self.y_train_std_ * cross.dot(self.alpha_) + self.y_train_mean_
        variance = self.kernel_.diag(X)
        variance -= np.einsum(
            "ki,kj,ij->k", cross, cross, self.K_inv_, optimize=True
        )
        negative = variance < 0
        if np.any(negative):
            warnings.warn(
                "Predicted variances smaller than 0. Setting those variances to 0."
            )
            variance[negative] = 0.0
        variance = variance * self.y_train_std_**2
        std = np.sqrt(variance)

        if return_mean_grad:
            gradient = self.kernel_.gradient_x(X[0], self.X_train_)
            mean_gradient = np.dot(gradient.T, self.alpha_) * self.y_train_std_
            if return_std_grad:
                std_gradient = np.zeros(X.shape[1])
                if not np.allclose(std, std_gradient):
                    std_gradient = -np.dot(
                        cross, np.dot(self.K_inv_, gradient)
                    )[0] / std
                    std_gradient = std_gradient * self.y_train_std_**2
                return mean, std, mean_gradient, std_gradient
            return mean, std, mean_gradient
        return mean, std


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


def _resolve_initial_dimensions(n_phase, n_normal):
    """解析首次调用的相位参数和普通参数数量。"""
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


def _validate_resume_dimensions(state, n_phase, n_normal):
    """校验恢复调用给出的维度，允许调用方全部省略。"""
    state_phase, state_normal, _ = _state_dimension_counts(state)

    if n_phase is not None:
        supplied_phase = _coerce_dimension_count(n_phase, "n_phase")
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

def _make_aod_kernel(n_dimensions, length_scale=None):
    """创建所有维度均使用普通线性距离的 Matérn 5/2 核。"""
    n_dim = _coerce_dimension_count(n_dimensions, "n_dimensions")
    if n_dim <= 0:
        raise ValueError("n_dimensions 必须为正数")

    if length_scale is None:
        length_scale = np.ones(n_dim)
    else:
        length_scale = np.asarray(length_scale, dtype=float)
        if length_scale.shape != (n_dim,):
            raise ValueError("length_scale 的数量必须与总参数数量一致")

    # ConstantKernel 学习整体信号幅度；Matern 学习样本间的线性空间相关性。
    # 每维独立 length_scale（ARD）反映该维变化多远会显著改变 cost。
    return ConstantKernel(1.0, (0.01, 1000.0)) * Matern(
        length_scale=length_scale,
        length_scale_bounds=[(0.01, 100.0)] * n_dim,
        nu=2.5,
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
    return state


def _gp_fit_policy(fit_number):
    """返回第 ``fit_number`` 次 GP 拟合采用的核参数优化策略。"""
    if fit_number <= 0:
        raise ValueError("fit_number 必须是正整数")

    # 首次以及定期校准时从多个起点搜索，降低长期困在局部最优的风险。
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
    x_phase, x_normal_parameters = _split_parameter_values(
        x_values, n_phase
    )
    best_x = state["best_x"]
    best_phase, best_normal_parameters = _split_parameter_values(
        best_x, n_phase
    )
    # 普通参数对外保持优化器的原始 [0,1] 坐标；幅值曲线由调用方显式
    # 调用相应的幅值曲线函数解码。
    x_normal = x_normal_parameters
    best_normal = best_normal_parameters
    best_cost = state["best_cost"]
    return {
        "x_phase": x_phase,
        "x_normal": x_normal,
        "best_x_phase": best_phase,
        "best_x_normal": best_normal,
        "best_cost": (
            None if not np.isfinite(best_cost) else float(best_cost)
        ),
        "done": bool(done),
    }


def _resolve_early_stop(value, n_dim):
    """解析兼容整数窗口；历史任务仍将它解释为patience。"""
    # 保留默认 20×维度的数值映射；新任务将它用作改善窗口，旧任务用作
    # Sobol 阶段之后的无显著改善次数。具体整数随任务保存。
    if isinstance(value, str):
        if value != "auto":
            raise ValueError("n_early_stop 字符串仅支持 'auto'")
        return 20 * int(n_dim)
    if value is None:
        return None
    try:
        integer = int(value)
    except (TypeError, ValueError, OverflowError):
        raise ValueError("n_early_stop 必须是正整数、'auto' 或 None") from None
    if isinstance(value, (bool, np.bool_)) or integer != value or integer <= 0:
        raise ValueError("n_early_stop 必须是正整数、'auto' 或 None")
    return integer


def suggest(
    cost=None,
    n_phase=None,
    n_normal=None,
    n_initial=None,
    noise=None,
    n_early_stop="auto",
    acq_func="LCB",
    /,
):
    """
    按固定顺序输入上一次实验的 cost，返回下一组相位段和普通段参数。

    外部程序只能按下面的顺序传参，不使用关键字参数：
    ``suggest(cost, n_phase, n_normal, n_initial, noise, n_early_stop,
    acq_func)``。首次调用至少传入前三个参数；恢复已有状态时，除
    ``cost`` 外的参数可以从后往前省略。

    参数:
         cost: 上一次实验的代价；首次调用或恢复待测点时传 None。
         n_phase: 相位参数数量。首次调用时须与 n_normal 一起指定；可为 0。
         n_normal: 普通参数数量。首次调用时须与相位参数数量一起指定；可为 0。
         n_initial: Sobol 初始采样点数；None 时按 2×总维度（向上取 2 的幂）自动计算。
        noise: 实测 cost/score 的噪声方差，仅首次调用使用；默认 1e-4。
               每次 GP 拟合时按
               历史 score 方差换算到 normalize_y=True 的归一化尺度，作为
               固定观测噪声使用；新任务的改善/停止容差与noise独立。
        n_early_stop: 保留第六个位置及名称。新任务传正整数时指定累计改善
                      窗口（如100），停止容差默认0.001；传(0,1)内的小数时
                      指定score绝对容差，窗口固定100。"auto"窗口为20×维度；
                      None禁用自动停止。新任务每10次检查累计改善和GP剩余
                      提升估计，经模型检查且连续3次通过才结束。旧检查点保留
                      原连续无显著改善的停止规则，不能用小数中途切换。
        acq_func: skopt 采集函数，常用值为 "LCB"、"EI"、"PI"。

    返回:
         包含 x_phase、x_normal、best_x_phase、best_x_normal、best_cost、done
         的字典。x_phase 和 x_normal 均为优化器的原始 [0,1] 坐标；当
         n_normal=4 时，x_normal 可直接作为
         single_sigmoid_amplitude_curve() 的第一个参数。

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
            n_phase, n_normal
        )
        if n_initial is None:
            # 缺省时按总维度自适应：2×总维度，向上取 2 的幂。
            n_initial = 1 << (2 * n_dimensions - 1).bit_length()
        else:
            if isinstance(n_initial, bool) or int(n_initial) != n_initial \
                    or int(n_initial) <= 0:
                raise ValueError("n_initial 必须是正整数")
            n_initial = int(n_initial)
        stopping = _new_stopping_state(n_early_stop, n_dimensions)
        configured_early_stop = stopping["window"]

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
        # 所有参数均使用同一套归一化坐标和普通线性距离。
        kernel = _make_aod_kernel(n_dimensions)

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
            # 保留历史计数字段以兼容诊断；新停止判据使用独立的tolerance。
            "sigma_noise": float(np.sqrt(measurement_noise)),
            "n_early_stop": configured_early_stop,
            "stopping": stopping,
            "done": False,
        }
        _save_state(state)
        return _format_result(state, x, done=False)

    # ---- 后续调用：载入状态并校验本次调用 ----
    state = _load_state()
    opt = state["opt"]
    # 提取最新模型的 kernel_，以便下一次拟合使用同一套暖启动参数。
    _compact_optimizer(opt)
    _validate_resume_dimensions(state, n_phase, n_normal)

    _, _, n_dimensions = _state_dimension_counts(state)
    _validate_stopping_argument(state, n_early_stop, n_dimensions)

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

    _record_stop_prediction_error(state, cost_val)
    _configure_gp_fit(opt, state["n_initial"])
    # tell() 将 last_x 与本次 cost 成对加入 Xi/yi；不能在同一个 cost 上
    # 重复调用，否则会把一次硬件测量错误地记录成多次观测。
    opt.tell(state["last_x"], cost_val)
    latest_model = opt.models[-1] if opt.models else None
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

    if "stopping" in state:
        should_stop = _check_model_stopping(state, latest_model)
    else:
        should_stop = (state["n_early_stop"] is not None
                       and state["no_improve"] >= state["n_early_stop"]
                       and state["best_x"] is not None)
    # 停止前持久化最后一次tell、真实最优和检查证据，恢复不重复计数。
    if should_stop:
        _compact_optimizer(opt)
        state["opt"] = opt
        state["done"] = True
        _save_state(state)
        return _format_result(state, state["best_x"], done=True)

    # ---- 请求并保存下一个建议点 ----
    # 必须先保存 last_x 再返回给调用方；若程序在硬件测量期间重启，下一次
    # cost=None 会重新给出同一点，从而保证测量结果不会与错误参数配对。
    x = _dedup(opt, opt.ask())
    _remember_stop_prediction(state, latest_model, x)
    _compact_optimizer(opt)
    state["opt"] = opt
    state["last_x"] = list(x)
    state["step"] += 1
    _save_state(state)

    return _format_result(state, x, done=False)

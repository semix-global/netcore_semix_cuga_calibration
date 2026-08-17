"""
贝叶斯优化器 —— 单一函数接口，供外部程序调用

用法:
    import phase_optimizer as po

    cost = None
    while True:
        # 首次调用需指定 n_dim，后续自动从状态文件读取
        r = po.suggest(cost, n_dim=11)
        # 外部自行判断何时停止
        cost = hardware(r['x'])
"""

import os
import pickle
import numpy as np
from skopt import Optimizer
from skopt.space import Real
from skopt.learning import GaussianProcessRegressor

# 状态持久化文件名
_STATE_FILE = "optimizer_state.pkl"
# 2π，参数上界
PI2 = 6.283185307179586


def _dedup(opt, x):
    """如果建议点离已评估点太近，加微扰避免重复"""
    # 尚无已评估点，直接返回
    if not hasattr(opt, 'Xi') or len(opt.Xi) == 0:
        return x
    # 检查是否与任一已评估点重合（距离 < 1e-6）
    X_arr = np.array(opt.Xi)
    for xi in X_arr:
        if np.linalg.norm(x - xi) < 1e-6:
            # 加微小随机扰动，并裁剪到 [0, 2π)（维度自适应）
            x = x + np.random.uniform(-0.01, 0.01, len(x))
            x = np.clip(x, 0.0, PI2)
            break
    return x


def suggest(cost=None, n_dim=None, n_initial=32, noise=1e-4,
            n_early_stop=25, acq_func="LCB"):
    """
    唯一对外接口。输入 cost 返回下一组参数。

    参数:
        cost: 上一次实验的代价（首次调用传 None）
        n_dim: 搜索空间维度（首次调用必须指定，后续从状态文件读取）
        n_initial: 初始采样步数（Sobol 低差异序列），此阶段不做贝叶斯推理
        noise: GP 观测噪声方差（σ_noise²），测量噪声大则调大（如 1e-3 ~ 1e-2）；
               早停阈值取其平方根 σ_noise = sqrt(noise)
        n_early_stop: 连续无改善步数阈值，触发早停（设为 None 禁用）
        acq_func: 采集函数，可选 "LCB"（默认）、"EI"、"PI"

    返回 dict: {'x', 'best_cost', 'best_x', 'done'}
        - x: 当前建议的参数列表
        - best_cost: 历史最优代价，首次为 None
        - best_x: 历史最优参数，首次为 None
        - done: 是否已早停，外部据此终止循环
    """
    # ---- 首次调用：创建 Optimizer 并返回第一个采样点 ----
    if not os.path.exists(_STATE_FILE):
        if n_dim is None:
            raise ValueError("首次调用必须指定 n_dim（维度数）")
        # 根据 n_dim 动态创建搜索空间，每个参数 ∈ [0, 2π)
        dimensions = [Real(0, PI2) for _ in range(n_dim)]
        opt = Optimizer(
            dimensions=dimensions,
            base_estimator=GaussianProcessRegressor(noise=noise),
            n_initial_points=n_initial,    # 前 n_initial 步初始采样
            acq_func=acq_func,             # 采集函数（默认 LCB）
            initial_point_generator="sobol",  # 初始采样用 Sobol 低差异序列
        )
        x = _dedup(opt, opt.ask())          # 获取第一个建议点并去重
        # 初始化状态字典，后续通过 pickle 持久化
        state = {
            'opt': opt,
            'n_dim': n_dim,                 # 记录维度
            'n_initial': n_initial,         # 记录初始采样步数（早停判断用）
            'step': 1,
            'last_x': list(x),              # 最近一次建议的参数
            'best_x': None,                 # 当前最优参数
            'best_cost': float('inf'),      # 当前最优代价
            'no_improve': 0,                # 连续无改善计数
        }
        pickle.dump(state, open(_STATE_FILE, 'wb'))
        return {'x': [float(v) for v in x],
                'best_cost': None,
                'best_x': None,
                'done': False}

    # ---- 后续调用：读入状态，反馈 cost，检查早停 ----
    state = pickle.load(open(_STATE_FILE, 'rb'))
    opt = state['opt']

    # 反馈上一次实验的代价到 GP 模型
    if cost is not None:
        opt.tell(state['last_x'], float(cost))
        # 改善量 = 历史最优代价 − 当前代价（正值表示更优）
        improve = state['best_cost'] - float(cost)
        # 改善超过噪声水平（标准差 σ_noise = sqrt(noise)）才算有效改善
        if improve > np.sqrt(noise):
            state['best_cost'] = float(cost)
            state['best_x'] = state['last_x']
            state['no_improve'] = 0          # 有效改善，重置计数器
        elif state['step'] > state['n_initial']:
            # 仅在贝叶斯搜索阶段（初始采样结束之后）才计数无改善
            state['no_improve'] += 1

    # 早停检查：连续 n_early_stop 步无改善且已有初始探索数据
    if (n_early_stop is not None
            and state['no_improve'] >= n_early_stop
            and state['best_x'] is not None):
        best = [float(v) for v in state['best_x']]
        return {'x': best, 'best_cost': state['best_cost'],
                'best_x': best, 'done': True}

    # 向 GP 请求下一个建议点
    x = _dedup(opt, opt.ask())

    # 保存状态，供下次进程调用
    state['opt'] = opt
    state['last_x'] = list(x)
    state['step'] += 1
    pickle.dump(state, open(_STATE_FILE, 'wb'))

    return {'x': [float(v) for v in x],
            'best_cost': state['best_cost'],
            'best_x': ([float(v) for v in state['best_x']]
                       if state['best_x'] is not None else None),
            'done': False}

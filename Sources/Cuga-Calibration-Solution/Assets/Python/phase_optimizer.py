"""
相位优化器 —— 单一函数接口，供外部程序调用

用法:
    import phase_optimizer as po

    cost = None
    while True:
        # 首次调用需指定 n_phases，后续自动从状态文件读取
        r = po.suggest(cost, n_phases=3)
        # 外部自行判断何时停止
        cost = hardware(r['phases'])
"""

import os
import pickle
import numpy as np
from skopt import Optimizer
from skopt.space import Real
from skopt.learning import GaussianProcessRegressor

# 状态持久化文件名
_STATE_FILE = "optimizer_state.pkl"
# 2π，相位差参数的上界
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


def suggest(cost=None, n_phases=None, n_initial=10, noise=1e-4,
            n_early_stop=25, random_state=42):
    """
    唯一对外接口。输入 cost 返回下一组参数。

    参数:
        cost: 上一次实验的代价（首次调用传 None）
        n_phases: 相位差个数（首次调用必须指定，后续从状态文件读取）
        n_initial: 初始随机探索步数，此阶段不做贝叶斯推理，纯随机采样
        noise: GP 对观测噪声的假设，测量噪声大则调大（如 1e-3 ~ 1e-2）
        n_early_stop: 连续无改善步数阈值，触发早停（设为 None 禁用）
        random_state: 随机种子，固定可复现

    返回 dict: {'phases', 'best_cost', 'best_phases', 'done'}
        - phases: 当前建议的相位差列表
        - best_cost: 历史最优代价，首次为 None
        - best_phases: 历史最优参数，首次为 None
        - done: 是否已早停，外部据此终止循环
    """
    # ---- 首次调用：创建 Optimizer 并返回第一个随机采样点 ----
    if not os.path.exists(_STATE_FILE):
        if n_phases is None:
            raise ValueError("首次调用必须指定 n_phases（相位差个数）")
        # 根据 n_phases 动态创建搜索空间，每个相位差 ∈ [0, 2π)
        dimensions = [Real(0, PI2) for _ in range(n_phases)]
        opt = Optimizer(
            dimensions=dimensions,
            base_estimator=GaussianProcessRegressor(
                noise=noise,              # GP 观测噪声假设
                random_state=random_state,
            ),
            n_initial_points=n_initial,    # 前 n_initial 步随机探索
            acq_func="EI",                 # 期望提升采集函数
            random_state=random_state,
        )
        x = _dedup(opt, opt.ask())          # 获取第一个建议点并去重
        # 初始化状态字典，后续通过 pickle 持久化
        state = {
            'opt': opt,
            'n_phases': n_phases,           # 记录维度
            'n_initial': n_initial,         # 记录初始探索步数（早停判断用）
            'step': 1,
            'last_x': list(x),              # 最近一次建议的参数
            'best_x': None,                 # 当前最优参数
            'best_cost': float('inf'),      # 当前最优代价
            'no_improve': 0,                # 连续无改善计数
        }
        pickle.dump(state, open(_STATE_FILE, 'wb'))
        return {'phases': [float(v) for v in x],
                'best_cost': None,
                'best_phases': None,
                'done': False}

    # ---- 后续调用：读入状态，反馈 cost，检查早停 ----
    state = pickle.load(open(_STATE_FILE, 'rb'))
    opt = state['opt']

    # 反馈上一次实验的代价到 GP 模型
    if cost is not None:
        opt.tell(state['last_x'], float(cost))
        # 更新历史最优，并追踪连续无改善步数
        if float(cost) < state['best_cost']:
            state['best_cost'] = float(cost)
            state['best_x'] = state['last_x']
            state['no_improve'] = 0          # 有改善，重置计数器
        elif state['step'] > state['n_initial']:
            # 仅在贝叶斯搜索阶段（初始随机探索结束之后）才计数无改善
            state['no_improve'] += 1

    # 早停检查：连续 n_early_stop 步无改善且已有初始探索数据
    if (n_early_stop is not None
            and state['no_improve'] >= n_early_stop
            and state['best_x'] is not None):
        phases = [float(v) for v in state['best_x']]
        best = state['best_cost']
        return {'phases': phases, 'best_cost': best,
                'best_phases': phases, 'done': True}

    # 向 GP 请求下一个建议点
    x = _dedup(opt, opt.ask())

    # 保存状态，供下次进程调用
    state['opt'] = opt
    state['last_x'] = list(x)
    state['step'] += 1
    pickle.dump(state, open(_STATE_FILE, 'wb'))

    return {'phases': [float(v) for v in x],
            'best_cost': state['best_cost'],
            'best_phases': ([float(v) for v in state['best_x']]
                            if state['best_x'] is not None else None),
            'done': False}

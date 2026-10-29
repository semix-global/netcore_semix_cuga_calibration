# ControllerGuard / ControllerGuardScope 使用说明

`ControllerGuard` 管理一个受管控制器参数的快照, 下发和回滚. 用于下发防呆。`ControllerGuardScope` 支持把多个 Guard 包进一次会话.

当前覆盖: `GantryOffset`, `TScale`, `XYScale`, 明场中心.

相关代码:

- `Sources/Cuga-Calibration-Solution/Core/Services/Implements/ControllerGuard.cs`
- `Sources/Cuga-Calibration-Solution/Core/Services/Implements/ControllerGuardScope.cs`
- `Sources/Cuga-Calibration-Solution/Core/Services/Interfaces/IControllerGuard.cs`

现网接入: Chuck Gantry, Center And Theta, Global Scale. Prealigner 没有受管参数, 不走 Guard.

## 1. Guard: 一个参数

由 ViewModel 直接构造, 状态属于调用方. 写函数必须自带回读 (现网走 `StageViewModel.SetXxx(..., HtmlLogUniqueId)`).

```csharp
_tScaleGuard = new ControllerGuard<double>(
    "TScale",
    () => StageViewModel.GetTScale(),
    value => StageViewModel.SetTScale(value, HtmlLogUniqueId),
    Logger,
    message => DialogWindowProvider.ShowDialog(message, DialogButtonsEnum.OK, DialogIconEnum.Warning));
```

### 1.1 状态

| 状态 | 含义 |
| --- | --- |
| `Clean` | 控制器当前值和初始值缓存一致 |
| `Covered` | `WriteController` 下发成功 |
| `Dirty` | 回滚失败, 控制器上可能是脏值 |

### 1.2 API

| 方法 | 作用 |
| --- | --- |
| `WriteController(value)` | 非 Clean 时先回滚, 再读当前值当快照, 再下发. 失败立刻 `Rollback` 再 throw |
| `WriteController()` | 重新下发上一次记录的历史下发值. 已是 `Covered` 则直接返回 |
| `RestoreControllerIfNotClean()` | 校准Load / Cancel 重载使用, 重置 |
| `RestoreController()` | 强制重置 |
| `RecordControllerInitialCache()` | 非 Clean 先回滚, 再读当前值覆盖快照（最终记录的一定是控制器最初的初始值） |
| `CacheValue` | 初始值缓存 |

## 2. Scope: 一次会话

```csharp
await ControllerGuardScope.RunAsync(
    guards,
    async context => { /* 业务 */ return result; },
    mode,
    recordCache);
```

执行顺序:

1. `recordCache == true` 时按注册顺序再记录一次控制器初始值
2. 执行 `func`.
3. `finally` 里按逆序恢复, 最后执行 `context.Defer`.

### 2.1 重置模式

| 模式 | 成功 `true` | 成功 `false` | 异常 |
| --- | --- | --- | --- |
| `AlwaysRestore` 强行重置 | 重置 | 重置 | 重置 |
| `RestoreOnFailure `失败时重置 | 不重置 | 重置 | 重置 |

### 2.2 Defer

`context.Defer(action)` 把弹窗等 UI 动作推迟到重置动作之后, 避免恢复还没做完就卡住界面.



## 3. 标准用法

### 3.1 Calibration

```
Step0:       WriteController(中性值)       //根据实际业务情况，非必要    
最后一步校准:  WriteController()            // 已 Covered 则空操作
             RunAsync(..., AlwaysRestore, recordCache: false) //校准无论成功与否，最终都需要重置，并且不改变控制器初始值
Load/Cancel: RestoreControllerIfNotClean 或 RestoreControllers //进入时重置本轮状态机，离开时收口（除了verify成功/Clean状态，其余一律重置）
```

`recordCache: false` 表示回滚到 Step0 那份快照, 校准禁止置true重新记录, 否则会把下发值覆盖为初始值.

 不建议在load和calibration重载中使用Scope和Guard服务, 此时logger还没开启，无法打印日志到html.

### 3.2 Verify

```
RunAsync(..., RestoreOnFailure)   // 默认 recordCache: true
func中下发接口用 SetXxx 直写, 无需经过WriteController（scope靠重置模式和recordCache管理）
成功: 不恢复历史初始值, 下发验证值
失败/异常: 回滚到 Verify 开头记录的初始值
```

Verify 必须直写 `SetXxx`. `WriteController` 会把 State 改成 `Covered`, Cancel 时 `RestoreIfNotClean` 会把刚 Verify 成功的值清掉.

### 3.3 各校准落点

| 校准 | Guard | Step0 中性值 | Verify |
| --- | --- | --- | --- |
| Gantry | `_gantryGuard` | `0` | `SetGantryOffset`, `RestoreOnFailure` |
| Center And Theta | `_tScaleGuard`, `_bfMachineCenterGuard` | TScale `1` | 直写明场中心和测量用 T, `RestoreOnFailure` |
| Global Scale | `_xyScaleGuard` | `(1, 1)` | `SetXYGlobalScale`, `RestoreOnFailure` |

## 4. 注意点

1. **底层直写接口比如 `SetTScale` 等不更新 Guard 状态.** **scope生效必须保证在RunAsync内部调用直写接口,谨慎类似debug即使窗口单步调用直发接口的场景，或者在外部用直发接口污染控制器初始值的场景。**
2. **多 Guard 顺序.** 注册顺序拍照, 逆序恢复. Center Verify 是 `[TScale, BFCenter]`, 恢复先中心再 T.
3. **初始值快照过期.** Verify 成功后 `_cacheValue` 还是 Verify 前的旧值, State 仍是 `Clean`. 下次校准 Step0 会重新读当前控制器再拍照, 所以现在逻辑自洽
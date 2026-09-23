# 中心通道光阑仿真图片

图片提取自 `HandleCalibration_Vela80_PupilCenterChannelFlexibleApertureViewModel_Calibrate_OK-2026042715030847223.html`. 原日志使用旧手动校准流程, 此处只复用图片, 不沿用其操作步骤或计算结果.

由 HTML 内嵌图像转换得到的 JPG 保持原始分辨率, JPEG 质量为 95, 不进行色度降采样. `images.json` 记录原始路径, 图片尺寸和 SHA256. 图片身份使用各自 `openViewer` 标识核对, 不依赖同一日志段中多个路径的出现顺序. 文件名与 `FourierCenterChannelFlexibleApertureViewModel` 的开发环境取图一致: `Channel3_{CenterChannelMotionKindEnum}_{DefaultAngleMotorAbsoluteValue}_Step{stepIndex}.jpg`, 例如 `Channel3_XRod_120_Step0.jpg` 和 `Channel3_XPush_0_Step1.jpg`. 清单同时保留原始 PNG 名称及哈希, 用于追溯转换来源. JPG 为有损压缩, 不再与内嵌 PNG 字节相同.

## 回放方式

开发环境沿用 `FourierViewModel.UseSimulatorImage` 和 Fourier Mock 取图接口. 生产环境仍按真实相机校准的 Channel 3 ROI 裁图. 日志图片已经是通道图, 开发环境直接使用完整回放图片, 不再套用当前相机 ROI 二次裁剪, 也不要求先完成相机 ROI 校准.

开发环境按运动类型和默认角度取图, 不按当前输入的电机位置换图. 光孔使用 `Channel3_XAperture_60_Step0.jpg` / `Channel3_XAperture_60_Step1.jpg` 和 `Channel3_YAperture_60_Step0.jpg` / `Channel3_YAperture_60_Step1.jpg`. 原始日志只有一张光孔图, Step1 是 Step0 的副本. Step 0 把椭圆贴到光孔边缘并调整宽高, Step 1 只沿对应轴移动同一尺寸的 ROI.

挡杆和 Push 的 Step0/Step1 使用下表位置对应的样本. 文件名使用默认角度, 不随界面上改过的角度或电机位置变化. 缺少对应文件时取图失败, 不回退到随机图, 不插值或伪造图片.

| 文件 | 样本来源 |
| --- | --- |
| `Channel3_XAperture_60_Step0.jpg` / `Step1.jpg` | X 光孔; Step1 是 Step0 副本 |
| `Channel3_XRod_120_Step0.jpg` / `Step1.jpg` | 电机 -6, -8 |
| `Channel3_XRod_180_Step0.jpg` / `Step1.jpg` | 电机 -8, -10 |
| `Channel3_XRod_240_Step0.jpg` / `Step1.jpg` | 电机 -8, -10 |
| `Channel3_XRod_300_Step0.jpg` / `Step1.jpg` | 电机 -8, -10 |
| `Channel3_XPush_0_Step0.jpg` / `Step1.jpg` | 电机 42, 43 |
| `Channel3_YAperture_60_Step0.jpg` / `Step1.jpg` | Y 光孔; Step1 是 Step0 副本 |
| `Channel3_YRod_120_Step0.jpg` / `Step1.jpg` | 电机 2, 4 |
| `Channel3_YRod_180_Step0.jpg` / `Step1.jpg` | 电机 4, 6 |
| `Channel3_YRod_240_Step0.jpg` / `Step1.jpg` | 电机 6, 8 |
| `Channel3_YRod_300_Step0.jpg` / `Step1.jpg` | 电机 8, 10 |

未使用的参考图、20260416 光孔图和 `XPush` 300 度样本已删除. XPush 的默认角度是 0, 开发环境只加载 `Channel3_XPush_0_Step0.jpg` 和 `Channel3_XPush_0_Step1.jpg`. Mock 的 X 电机范围包含负位置. 界面保留宽范围输入, 避免 Cache 尚未加载时的零边界覆盖输入值, 执行前按实际电机范围校验.

## 校准约束

- Step0 的 XAperture/YAperture 可任意移动, 用四角锚点修改宽高. XRod/XPush 只能左右移动, 用左右中心锚点修改宽度. YRod 只能上下移动, 用上下中心锚点修改高度.
- Step1 复制 Step0 的 ROI 尺寸, 不允许缩放. XAperture/XRod/XPush 只沿 X 移动, YAperture/YRod 只沿 Y 移动.
- 执行期间禁用角度和电机位置输入, 避免取图位置和计算使用的位置不一致. Reset 只清空结果.
- PixelSize = (Step1 电机位置 - Step0 电机位置) / (Step1 ROI 中心坐标 - Step0 ROI 中心坐标). XAperture/XRod/XPush 使用中心 X, YAperture/YRod 使用中心 Y. 结果必须是正的有限值.
- 旧日志的正值像素/电机单位不能直接当作当前 PixelSize. 例如旧 X120 记录电机 -6 到 -8, 中心 X 为 1249.5 到 1370.5, 当前公式得到 -2/121. Y120 为 2 到 4, 中心 Y 为 1344.5 到 1215.5, 当前公式得到 -2/129.

## 验证边界

已核对转换后图片的 JPG 编码与哈希, 原始身份标识, 尺寸, 默认参数样本覆盖, XAML 绑定及编辑器方向和手柄配置. XAML 使用仓库配置的 XAML Styler 格式化. 按用户要求未执行编译或构建, 未进行交互及硬件验证.

CUGA 的 MotorRelation 字段现有注释写为像素/毫米, 当前 DTO 直接映射 PixelSize. 本次保留原映射, 此单位差异需与 CUGA 接口确认后再调整.
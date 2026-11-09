using algocv_sharp;

namespace Net.Utilities.Algorithms.AlgoCSharp;

/// <summary>
/// algo Image 域内操作扩展（与 Halcon 侧 HImage 扩展对应的包装层）。
/// <para>全部返回新图像（用 using 管理释放），源图不被修改；绘制类接口输出恒为 3 通道 BGR（1 通道输入会先复制成 BGR）。</para>
/// <para>Rotate/Mirror 不支持 Float16；Subtract16S 为 16 位有符号差分（不钳位），与 Halcon SubImage 的 (g1-g2)*mult+add 钳位显示域语义不同。</para>
/// </summary>
public static class AlgoCVImageExtensions
{
    /// <param name="image"></param>
    extension(Image image)
    {
        /// <summary>
        /// 镜像（对齐 Halcon HorizontalFlip/VerticalFlip：Horizontal=水平镜像，Vertical=垂直镜像，Both=双向）。尺寸、类型和通道数不变。
        /// </summary>
        /// <param name="mode">镜像模式</param>
        /// <returns>镜像后的新图像</returns>
        public Image Mirror(MirrorMode mode) => ImageProc.Mirror(image, mode);

        /// <summary>
        /// 旋转（对齐 Halcon RotateCounterClockwise90Degree 等）。尺寸和类型随旋转方向交换或不变。
        /// </summary>
        /// <param name="angle">旋转角度枚举</param>
        /// <returns>旋转后的新图像</returns>
        public Image Rotate(RotateAngle angle) => ImageProc.Rotate(image, angle);

        /// <summary>
        /// BGR 彩色图转灰度图（对齐 Halcon rgb1_to_gray）。
        /// </summary>
        /// <returns>灰度图</returns>
        public Image BgrToGray() => ImageProc.BgrToGray(image);

        /// <summary>
        /// 灰度图转 BGR 彩色图（对齐 Halcon gen_image3 复制通道）。
        /// </summary>
        /// <returns>3 通道 BGR 图</returns>
        public Image GrayToBgr() => ImageProc.GrayToBgr(image);

        /// <summary>
        /// 16 位有符号差分（本图 - other）。两图须为 UInt16 或 Int16，输出恒为 Int16（结果带正负，仅差值越界时饱和）。
        /// </summary>
        /// <param name="other">减图，尺寸与通道数须与本图一致</param>
        /// <returns>与输入同通道数的 Int16 差分图</returns>
        public Image Subtract16S(Image other) => ImageProc.Subtract16S(image, other);

        /// <summary>
        /// 绘制十字（对齐 Halcon gen_cross_contour_xld 显示语义）。输出恒为 3 通道 BGR；线型固定，无抗锯齿。
        /// </summary>
        /// <param name="x">中心列</param>
        /// <param name="y">中心行</param>
        /// <param name="radius">臂长，每条臂含 2*radius+1 个像素；0 表示单点</param>
        /// <param name="thickness">线宽，须 &gt;= 1</param>
        /// <param name="bgr">BGR 颜色分量（3 个），或 1 个分量表示三通道同值</param>
        /// <returns>3 通道 BGR 图像</returns>
        public Image DrawCross(int x, int y, int radius, int thickness, double[] bgr) => ImageProc.DrawCross(image, x, y, radius, thickness, bgr);

        /// <summary>
        /// 绘制矩形（对齐 Halcon disp_rectangle2 显示语义）。
        /// </summary>
        /// <param name="x">左上角列</param>
        /// <param name="y">左上角行</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="thickness">线宽；&gt;= 1 线框，&lt; 0 填充</param>
        /// <param name="bgr">BGR 颜色分量（3 个），或 1 个分量表示三通道同值</param>
        /// <returns>3 通道 BGR 图像</returns>
        public Image DrawRectangle(int x, int y, int width, int height, int thickness, double[] bgr) => ImageProc.DrawRectangle(image, x, y, width, height, thickness, bgr);
    }
}

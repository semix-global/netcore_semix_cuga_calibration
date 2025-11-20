using HalconDotNet;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Models.Geometries;

namespace Core.Utilities;

public static class TempRawImageFactory
{
    public static HImage CreateImage(byte[] bodyBytes, SizeI sizeI)
    {
        var (width, height) = sizeI;

        // 16位单通道raw body bytes((16位图片0-65535, 并且是单通道), 实际上我们线扫相机是12bit(0-4095)[为了明暗差别大], 为了解析方便解析16bit浪费多余的传输带宽)
        /* 线扫相机扫图是一列一列的拼接上去的(从上到下垂直扫描的16位单通道数组)
         * 1. 原始数据
         * Begin -------→ -------→ ......  -------→ 一维数组 [height * width]
         */

        /* 2. 一列一列的拼接上去的, [转换为图片宽度: height, 高度: width]
         *
         * Begin  -------→
         *        -------→
         *        ......
         *        -------→ 二维数组 [width行, height列]
         */
        using var rawImage = HalconFactory.CreateImage(bodyBytes, height, width, 1, 12);

        /* 3. 按照常数[不会改变像素值]逆时针旋转90度, [图片宽度: width, 高度: height]
         *
         *       ↑ ↑ ↑ ↑          ↑
         *       | | | |          |
         *       | | | |          |
         *       | | | |          |
         *       | | | |  ......  |
         *       | | | |          |
         *       | | | |          |
         * Begin | | | |          | 二维数组 [height行, width列]
         */
        using var rotateImage = rawImage.RotateCounterClockwise90Degree();

        /* 4. Begin跑到下面去了, 垂直翻转, [图片宽度: width, 高度: height]
         *
         * Begin | | | |          |
         *       | | | |          |
         *       | | | |          |
         *       | | | |          |
         *       | | | |  ......  |
         *       | | | |          |
         *       | | | |          |
         *       ↓ ↓ ↓ ↓          ↓ 二维数组 [height行, width列]
         */
        return rotateImage.VerticalFlip();
    }
}
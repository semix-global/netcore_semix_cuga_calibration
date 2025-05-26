using CommunityToolkit.Diagnostics;
using HalconDotNet;
using Net.Utilities.Algorithm.Halcon.Helper;
using Net.Utilities.Algorithm.MathNet.Helper;
using Net.Utilities.Models;
using System.Runtime.InteropServices;

namespace Net.Utilities.Helper.File;

public static class RawImageHelper
{
    /// <summary>
    /// 获取raw bytes尺寸
    /// </summary>
    /// <code>
    /// struct Raw
    /// {
    ///     u16  header_type; // must always be 0x534D. ASCII for "SM"
    ///     u32  header_reserved1; // must always be 0
    ///     u32  header_reserved2; // must always be 0
    ///     u32  header_width; // width of the image
    ///     u32  header_reserved3; // must always be 0
    ///     u32  header_height; // height of the image
    ///     u32  header_reserved4; // must always be 0
    ///     u16  header_random_size； // random data length
    ///     u8[] body; // image data
    ///     u8[] footer; // random data
    /// }
    /// </code>
    /// <param name="rawBytes">raw bytes</param>
    /// <returns>尺寸</returns>
    public static (Size Size, int BodyBytesStartIndex, int BodyBytesLength) GetSize(byte[] rawBytes)
    {
        const int rawImageHeaderLength = 28;

        using var stream = new MemoryStream(rawBytes);
        using var binaryReader = new BinaryReader(stream);

        binaryReader.BaseStream.Seek(10, SeekOrigin.Begin);
        var width = binaryReader.ReadInt32();
        binaryReader.BaseStream.Seek(18, SeekOrigin.Begin);
        var height = binaryReader.ReadInt32();
        binaryReader.BaseStream.Seek(26, SeekOrigin.Begin);
        var randomSize = binaryReader.ReadInt16();

        var imageRawBytesLength = width * height * 2;

        if (imageRawBytesLength != binaryReader.BaseStream.Length - rawImageHeaderLength - randomSize) ThrowHelper.ThrowArgumentOutOfRangeException("Invalid image raw raw bytes length");

        return (new Size(width, height), rawImageHeaderLength, imageRawBytesLength);
    }

    /// <summary>
    /// 获取raw bytes矩阵
    /// </summary>
    /// <param name="rawBytes">raw bytes</param>
    /// <returns>(矩阵, Size)</returns>
    public static (short[,] Matrix, Size size) ToMatrix(byte[] rawBytes)
    {
        var (size, bodyBytesStartIndex, bodyBytesLength) = GetSize(rawBytes);
        var (width, height) = size;
        var bodySpan = rawBytes.AsSpan().Slice(bodyBytesStartIndex, bodyBytesLength);

        /* 线扫相机扫图是一列一列的拼接上去的
         * 1. 原始数据
         * Begin -------→ -------→ ......  -------→ 一维数组 [height * width]
         *
         */
        var bodyShorts = MemoryMarshal.Cast<byte, short>(bodySpan).ToArray();

        #region Matrix

        /* 2. 一列一列的拼接上去的, [转换为宽度: height, 高度: width]
         *
         * Begin  -------→
         *        -------→
         *        ......
         *        -------→ 二维数组 [width行, height列]
         */
        var matrix = MatrixHelper.ToMatrixByRow(bodyShorts, width, height);

        /* 3. 转置, 行变为列, 列变为行
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
        var resultMatrix = MatrixHelper.Transpose(matrix);

        #endregion Matrix

        return (resultMatrix, size);
    }

    /// <summary>
    /// 获取raw bytes矩阵
    /// </summary>
    /// <param name="bodyBytes">raw body bytes</param>
    /// <param name="size">图片尺寸</param>
    /// <returns>(矩阵, raw bytes, Size)</returns>
    public static (short[,] Matrix, byte[] RawBytes, Size size) ToMatrix(byte[] bodyBytes, Size size)
    {
        var rawBytes = BodyAddHeaderFooter(bodyBytes, size);

        var (matrix, newSize) = ToMatrix(rawBytes);

        Guard.IsEqualTo(size, newSize, "Size is not equal to new raw bytes Size");

        return (matrix, rawBytes, newSize);
    }

    /// <summary>
    /// 获取raw bytes水平翻转矩阵
    /// </summary>
    /// <param name="rawBytes">raw bytes</param>
    /// <returns>(水平翻转矩阵, 水平翻转raw bytes, Size)</returns>
    public static (short[,] Matrix, byte[] RawBytes, Size size) ToHorizontalFlipMatrix(byte[] rawBytes)
    {
        var (size, bodyBytesStartIndex, bodyBytesLength) = GetSize(rawBytes);
        var (width, height) = size;
        var bodySpan = rawBytes.AsSpan().Slice(bodyBytesStartIndex, bodyBytesLength);

        /* 线扫相机扫图是一列一列的拼接上去的
         * 1. 原始数据
         * Begin -------→ -------→ ......  -------→ 一维数组 [height * width]
         *
         */
        var bodyShorts = MemoryMarshal.Cast<byte, short>(bodySpan).ToArray();

        #region Matrix

        /* 2. 一列一列的拼接上去的, [转换为宽度: height, 高度: width]
         *
         * Begin  -------→
         *        ......
         *        -------→ 二维数组 [width行, height列]
         */
        var matrix = MatrixHelper.ToMatrixByRow(bodyShorts, width, height);

        /* 3. 垂直翻转, 先采图的到后面去了
         *
         *        -------→
         *        -------→
         *        ......
         * Begin  -------→ 二维数组 [width行, height列]
         */
        var verticalFlipMatrix = MatrixHelper.VerticalFlip(matrix);

        /* 4. 转置, 行变为列, 列变为行
         *
         *       | | | |        Begin  |
         *       | | | |               |
         *       | | | |               |
         *       | | | |               |
         *       | | | |  ......       |
         *       | | | |               |
         *       | | | |               |
         *       ↓ ↓ ↓ ↓               ↓ 二维数组 [height行, width列]
         */
        var resultMatrix = MatrixHelper.Transpose(verticalFlipMatrix);

        #endregion Matrix

        var resultRawBytes = BodyAddHeaderFooter(MemoryMarshal.Cast<short, byte>(MatrixHelper.ToArrayByRow(verticalFlipMatrix)).ToArray(), size);
        var (newSize, _, _) = GetSize(resultRawBytes);
        Guard.IsEqualTo(size, newSize, "Size is not equal to new raw bytes Size");

        return (resultMatrix, resultRawBytes, size);
    }

    /// <summary>
    /// 获取raw bytes水平翻转矩阵
    /// </summary>
    /// <param name="bodyBytes">raw body bytes</param>
    /// <param name="size">图片尺寸</param>
    /// <returns>(水平翻转矩阵, 水平翻转raw bytes, Size)</returns>
    public static (short[,] Matrix, byte[] RawBytes, Size size) ToHorizontalFlipMatrix(byte[] bodyBytes, Size size)
    {
        var rawBytes = BodyAddHeaderFooter(bodyBytes, size);

        var (matrix, newRawBytes, newSize) = ToHorizontalFlipMatrix(rawBytes);
        Guard.IsEqualTo(size, newSize, "Size is not equal to new raw bytes Size");

        return (matrix, newRawBytes, newSize);
    }

    /// <summary>
    /// 16位单通道raw bytes(从左到右, 从上到下扫描) to hObject
    /// </summary>
    /// <param name="rawBytes">16位单通道raw bytes((16位图片0-65535, 并且是单通道), 实际上我们线扫相机是12bit(0-4095)[为了明暗差别大], 为了解析方便解析16bit浪费多余的传输带宽)</param>
    /// <returns>HObject</returns>
    public static HObject ToHObject(byte[] rawBytes)
    {
        var ((width, height), bodyBytesStartIndex, _) = GetSize(rawBytes);

        /* 线扫相机扫图是一列一列的拼接上去的(从上到下垂直扫描的16位单通道数组)
         * 1. 原始数据
         * Begin -------→ -------→ ......  -------→ 一维数组 [height * width]
         */
        var pixelPointer = Marshal.UnsafeAddrOfPinnedArrayElement(rawBytes, bodyBytesStartIndex);

        /* 2. 一列一列的拼接上去的, [转换为图片宽度: height, 高度: width]
         *
         * Begin  -------→
         *        -------→
         *        ......
         *        -------→ 二维数组 [width行, height列]
         */
        HOperatorSet.GenImage1(out var rawImage, "int2", height, width, pixelPointer);
        using var _0 = rawImage;

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
        using var rotateImage = HalconHelper.RotateCounterClockwise90Degree(rawImage);

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
        return HalconHelper.VerticalFlip(rotateImage);
    }

    /// <summary>
    /// 16位单通道raw body bytes(从左到右, 从上到下扫描) to HObject
    /// </summary>
    /// <param name="bodyBytes">16位单通道raw body bytes((16位图片0-65535, 并且是单通道), 实际上我们线扫相机是12bit(0-4095)[为了明暗差别大], 为了解析方便解析16bit浪费多余的传输带宽)</param>
    /// <param name="size">图片尺寸</param>
    /// <returns>HObject</returns>
    public static (HObject Image, byte[] RawBytes) ToHObject(byte[] bodyBytes, Size size)
    {
        var rawBytes = BodyAddHeaderFooter(bodyBytes, size);

        return (ToHObject(rawBytes), rawBytes);
    }

    /// <summary>
    /// raw body bytes add header and footer
    /// </summary>
    /// <param name="bodyBytes">raw body bytes</param>
    /// <param name="size">图片尺寸</param>
    /// <returns>raw bytes</returns>
    public static byte[] BodyAddHeaderFooter(byte[] bodyBytes, Size size)
    {
        var (width, height) = size;
        if (bodyBytes.Length != width * height * 2) ThrowHelper.ThrowArgumentOutOfRangeException("Invalid data block bytes length");

        var random = new Random();
        var randomSize = Convert.ToInt16(random.Next(1, 100));
        var randomBlock = new byte[randomSize];
        random.NextBytes(randomBlock);

        var header = (byte[])
        [
            0x53, 0x4D, // bf_type
            0x00, 0x00, 0x00, 0x00, // bf_reserved1
            0x00, 0x00, 0x00, 0x00, // bf_reserved2
            .. BitConverter.GetBytes(width), // bi_width
            0x00, 0x00, 0x00, 0x00, // bf_reserved3
            .. BitConverter.GetBytes(height), // bi_height
            0x00, 0x00, 0x00, 0x00, // bf_reserved4
            .. BitConverter.GetBytes(randomSize), // bf_random_size
        ];

        return
        [
            ..header,
            ..bodyBytes,
            ..randomBlock
        ];
    }
}
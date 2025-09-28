using CommunityToolkit.Diagnostics;
using HalconDotNet;
using Net.Utilities.Models;
using Net.Utilities.Models.Enums.Files;
using Net.Utilities.Models.Geometries;
using System.Runtime.InteropServices;

namespace Core.Utilities;

public static class RawImageHelper
{
    /// <summary>
    /// 获取raw bytes尺寸
    /// </summary>
    /// <param name="filePath">raw image file path</param>
    /// <returns>尺寸</returns>
    public static (Size Size, long BodyBytesStartIndex, long BodyBytesLength) GetSize(string filePath)
    {
        using var fileStream = File.OpenRead(filePath);
#if NET
        using var binaryReader = new BinaryReader(fileStream);
#else
        using var binaryReader = new BinaryReader(fileStream, System.Text.Encoding.UTF8, false);
#endif

        return GetSize(binaryReader);
    }

    /// <summary>
    /// <see cref="GetSize(string)"/>
    /// </summary>
    /// <param name="rawBytes">raw image bytes</param>
    /// <returns>尺寸</returns>
    public static (Size Size, long BodyBytesStartIndex, long BodyBytesLength) GetSize(byte[] rawBytes)
    {
        using var memoryStream = new MemoryStream(rawBytes, false);
#if NET
        using var binaryReader = new BinaryReader(memoryStream);
#else
        using var binaryReader = new BinaryReader(memoryStream, System.Text.Encoding.UTF8, false);
#endif

        return GetSize(binaryReader);
    }

    /// <summary>
    /// <see cref="GetSize(string)"/>
    /// </summary>
    /// <param name="binaryReader">raw image bytes binary reader</param>
    /// <returns>尺寸</returns>
    public static (Size Size, long BodyBytesStartIndex, long BodyBytesLength) GetSize(BinaryReader binaryReader)
    {
        var (_, size, bodyBytesStartIndex, bodyBytesLength) = DecodeNanoRaw(binaryReader);

        return (size, bodyBytesStartIndex, bodyBytesLength);
    }

    /// <summary>
    /// raw body bytes add header and footer
    /// </summary>
    /// <param name="bodyBytes">raw image body bytes</param>
    /// <param name="size">图片尺寸</param>
    /// <returns>raw bytes</returns>
    public static byte[] BodyAddHeaderFooter(byte[] bodyBytes, Size size)
    {
        var (width, height) = (SizeI)size;
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
            .. BitConverter.GetBytes(randomSize) // bf_random_size
        ];

        return
        [
            ..header,
            ..bodyBytes,
            ..randomBlock
        ];
    }

    /// <summary>
    /// 获取raw bytes矩阵
    /// </summary>
    /// <param name="filePath">raw image file path</param>
    /// <returns>(矩阵, Size)</returns>
    public static (short[,] Matrix, Size size) ToMatrix(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);

        return ToMatrix(bytes);
    }

    /// <summary>
    /// <see cref="ToMatrix(string)"/>
    /// </summary>
    /// <param name="bodyBytes">raw image body bytes</param>
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
    /// <see cref="ToMatrix(string)"/>
    /// </summary>
    /// <param name="rawBytes">raw image bytes</param>
    /// <returns>(矩阵, Size)</returns>
    public static (short[,] Matrix, Size size) ToMatrix(byte[] rawBytes)
    {
        var (size, bodyBytesStartIndex, bodyBytesLength) = GetSize(rawBytes);
        var (width, height) = (SizeI)size;
        var bodySpan = rawBytes.AsSpan().Slice(Convert.ToInt32(bodyBytesStartIndex), Convert.ToInt32(bodyBytesLength));

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
        var matrix = MatrixUtils.ToMatrixByRow(bodyShorts, width, height);

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
        var resultMatrix = MatrixUtils.Transpose(matrix);

        #endregion Matrix

        return (resultMatrix, size);
    }

    /// <summary>
    /// 获取raw bytes水平翻转矩阵
    /// </summary>
    /// <param name="filePath">raw image file path</param>
    /// <returns>(水平翻转矩阵, 水平翻转raw bytes, Size)</returns>
    public static (short[,] Matrix, byte[] RawBytes, Size size) ToHorizontalFlipMatrix(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);

        return ToHorizontalFlipMatrix(bytes);
    }

    /// <summary>
    /// <see cref="ToHorizontalFlipMatrix(string)"/>
    /// </summary>
    /// <param name="bodyBytes">raw image body bytes</param>
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
    /// <see cref="ToHorizontalFlipMatrix(string)"/>
    /// </summary>
    /// <param name="rawBytes">raw image bytes</param>
    /// <returns>(水平翻转矩阵, 水平翻转raw bytes, Size)</returns>
    public static (short[,] Matrix, byte[] RawBytes, Size size) ToHorizontalFlipMatrix(byte[] rawBytes)
    {
        var (size, bodyBytesStartIndex, bodyBytesLength) = GetSize(rawBytes);
        var (width, height) = (SizeI)size;
        var bodySpan = rawBytes.AsSpan().Slice(Convert.ToInt32(bodyBytesStartIndex), Convert.ToInt32(bodyBytesLength));

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
        var matrix = MatrixUtils.ToMatrixByRow(bodyShorts, width, height);

        /* 3. 垂直翻转, 先采图的到后面去了
         *
         *        -------→
         *        -------→
         *        ......
         * Begin  -------→ 二维数组 [width行, height列]
         */
        var verticalFlipMatrix = MatrixUtils.VerticalFlip(matrix);

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
        var resultMatrix = MatrixUtils.Transpose(verticalFlipMatrix);

        #endregion Matrix

        var resultRawBytes = BodyAddHeaderFooter(MemoryMarshal.Cast<short, byte>(MatrixUtils.ToArrayByRow(verticalFlipMatrix)).ToArray(), size);
        var (newSize, _, _) = GetSize(resultRawBytes);

        Guard.IsEqualTo(size, newSize, "Size is not equal to new raw bytes Size");

        return (resultMatrix, resultRawBytes, size);
    }

    /// <summary>
    /// 12位单通道raw bytes(从左到右, 从上到下扫描) to HImage
    /// </summary>
    /// <param name="filePath">raw image file path</param>
    /// <returns>HImage</returns>
    public static HImage CreateImage(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);

        return CreateImage(bytes);
    }

    /// <summary>
    /// <see cref="CreateImage(string)"/>
    /// </summary>
    /// <param name="bodyBytes">raw image body bytes</param>
    /// <param name="size">图片尺寸</param>
    /// <returns>HImage</returns>
    public static (HImage Image, byte[] RawBytes) CreateImage(byte[] bodyBytes, Size size)
    {
        var rawBytes = BodyAddHeaderFooter(bodyBytes, size);

        return (CreateImage(rawBytes), rawBytes);
    }

    /// <summary>
    /// <see cref="CreateImage(string)"/>
    /// </summary>
    /// <param name="rawBytes">raw image bytes</param>
    /// <returns>HImage</returns>
    public static HImage CreateImage(byte[] rawBytes)
    {
        var (size, bodyBytesStartIndex, _) = GetSize(rawBytes);
        var (width, height) = (SizeI)size;

        // 16位单通道raw body bytes((16位图片0-65535, 并且是单通道), 实际上我们线扫相机是12bit(0-4095)[为了明暗差别大], 为了解析方便解析16bit浪费多余的传输带宽)
        /* 线扫相机扫图是一列一列的拼接上去的(从上到下垂直扫描的16位单通道数组)
         * 1. 原始数据
         * Begin -------→ -------→ ......  -------→ 一维数组 [height * width]
         */
        var pixelPointer = Marshal.UnsafeAddrOfPinnedArrayElement(rawBytes, Convert.ToInt32(bodyBytesStartIndex));

        /* 2. 一列一列的拼接上去的, [转换为图片宽度: height, 高度: width]
         *
         * Begin  -------→
         *        -------→
         *        ......
         *        -------→ 二维数组 [width行, height列]
         */
        using var rawImage = CreateImage(pixelPointer, height, width, 1, 12);

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

    internal static (ImageTypeEnum ImageTypeEnum, Size Size, long BodyBytesStartIndex, long BodyBytesLength) DecodeNanoRaw(BinaryReader binaryReader)
    {
        /*
         * struct Raw
         * {
         *     u16  header_type; // must always be 0x534D. ASCII for "SM"
         *     u32  header_reserved1; // must always be 0
         *     u32  header_reserved2; // must always be 0
         *     u32  header_width; // width of the image
         *     u32  header_reserved3; // must always be 0
         *     u32  header_height; // height of the image
         *     u32  header_reserved4; // must always be 0
         *     u16  header_random_size； // random data length
         *     u8[] body; // image data
         *     u8[] footer; // random data
         * };
         */

        binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);

        const long rawImageHeaderLength = 28;

        binaryReader.BaseStream.Seek(10, SeekOrigin.Begin);
        var width = binaryReader.ReadInt32();
        binaryReader.BaseStream.Seek(18, SeekOrigin.Begin);
        var height = binaryReader.ReadInt32();
        binaryReader.BaseStream.Seek(26, SeekOrigin.Begin);
        var randomSize = binaryReader.ReadInt16();

        var imageRawBytesLength = 2L * width * height;

        return imageRawBytesLength == binaryReader.BaseStream.Length - rawImageHeaderLength - randomSize
            ? (ImageTypeEnum.Bmp, new Size(width, height), rawImageHeaderLength, imageRawBytesLength)
            : ThrowHelper.ThrowArgumentOutOfRangeException<(ImageTypeEnum ImageTypeEnum, Size Size, long BodyBytesStartIndex, long BodyBytesLength)>(nameof(binaryReader));
    }

    internal static int GetBytesPerPixel(this int bitsPerPixel)
    {
        // 表示每个像素所占用的字节数(最小满足8的倍数的bit值), eg: 4bit (4+7)/8=1 return 1 byte
        return (bitsPerPixel + 7) / 8;
    }

    internal static HImage Copy(this HImage @this) => @this.CopyImage();

    internal static HImage RotateCounterClockwise90Degree(this HImage @this) => @this.RotateImage(90d, "constant");

    internal static HImage VerticalFlip(this HImage @this) => @this.MirrorImage("row");

    internal static HImage CreateImage(IntPtr intPtr, int width, int height, int channels, int bitsPerPixel)
    {
        using var image = new HImage();

        var type = (bitsPerPixel.GetBytesPerPixel() / channels) switch
        {
            1 => "byte",
            2 => "uint2",
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(intPtr))
        };

        switch (channels)
        {
            case 1:
                image.GenImage1(type, width, height, intPtr);

                return image.Copy();

            case 3:
                image.GenImageInterleaved(intPtr, "bgr", width, height, 0, type, 0, 0, 0, 0, -1, 0);

                return image.Copy();

            case 4:
                image.GenImageInterleaved(intPtr, "bgrx", width, height, 0, type, 0, 0, 0, 0, -1, 0);

                return image.Copy();

            default:
                return ThrowHelper.ThrowArgumentOutOfRangeException<HImage>(nameof(intPtr));
        }
    }
}
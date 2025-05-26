using CommunityToolkit.Diagnostics;
using Net.Utilities.Enums;
using Net.Utilities.Extensions;
using Net.Utilities.Models;

namespace Net.Utilities.Helper.File;

public static class ImageHelper
{
    private static readonly Dictionary<byte[], Func<BinaryReader, (ImageTypeEnum imageTypeEnum, Size size)>> ImageFormatDecoders = new()
    {
        { "BM"u8.ToArray(), DecodeBitmap }, // UTF-8 字符串字面量, 表示数组: [0x42, 0x4D]
        { "GIF87a"u8.ToArray(), DecodeGif }, // [0x47, 0x49, 0x46, 0x38, 0x37, 0x61] 1987年5月发布的GIF规范
        { "GIF89a"u8.ToArray(), DecodeGif }, // [0x47, 0x49, 0x46, 0x38, 0x39, 0x61] 1989年7月发布的GIF规范
        { [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], DecodePng },
        { [0xFF, 0xD8], DecodeJpeg },
        { [0x00, 0x00, 0x01, 0x00], DecodeIco }
    };

    public static (ImageTypeEnum imageTypeEnum, Size size) GetImageInfo(string filePath)
    {
        using var fileStream = System.IO.File.OpenRead(filePath);

        return GetImageInfo(fileStream);
    }

    public static (ImageTypeEnum imageTypeEnum, Size size) GetImageInfo(byte[] bytes)
    {
        using var memoryStream = new MemoryStream(bytes, false);

        return GetImageInfo(memoryStream);
    }

    private static (ImageTypeEnum imageTypeEnum, Size size) GetImageInfo(Stream stream)
    {
        using var binaryReader = new BinaryReader(stream);

        var maxMagicBytesLength = ImageFormatDecoders.Keys.Max(k => k.Length);
        var magicBytes = new byte[maxMagicBytesLength];

        for (var i = 0; i < maxMagicBytesLength; i++)
        {
            magicBytes[i] = binaryReader.ReadByte();

            foreach (var kvPair in ImageFormatDecoders.Where(kvPair => magicBytes.Take(kvPair.Key.Length).SequenceEqual(kvPair.Key)))
                return kvPair.Value(binaryReader);
        }

        return ThrowHelper.ThrowArgumentException<(ImageTypeEnum imageTypeEnum, Size size)>("Unsupported image format");
    }

    private static (ImageTypeEnum imageTypeEnum, Size size) DecodeBitmap(BinaryReader binaryReader)
    {
        /*
         * struct Bitmap
         * {
         *     u16  bmp_file_header_type; // must always be 0x4D42. ASCII for "BM"
         *     u32  bmp_file_header_image_data_size; // size of the image data
         *     u16  bmp_file_header_reserved; // must always be 0
         *     u16  bmp_file_header_reserved; // must always be 0
         *     u32  bmp_file_header_off_bits; // image data pointer address offset. 数据指针地址偏移
         *     s32  bitmap_information_size; // bitmap information size
         *     s32  bitmap_information_width; // width of the image
         *     u32  bitmap_information_height; // height of the image
         *     u16  bitmap_information_planes;
         *     u16  bitmap_information_bit_count; // 比特数/像素数
         *     u32  bitmap_information_compression; // compression type
         *     u32  bitmap_information_ize_image;
         *     s32  bitmap_information_pels_per_meter;
         *     s32  bitmap_information_pels_per_meter;
         *     u32  bitmap_information_clr_used;
         *     u32  bitmap_information_clr_important;
         *     u8[] data;
         * }
         */

        binaryReader.BaseStream.Seek(18, SeekOrigin.Begin);

        var width = binaryReader.ReadInt32();
        var height = binaryReader.ReadInt32();

        // 同时如果为正，说明位图倒立（即数据表示从图像的左下角到右上角），如果为负说明正向
        return width switch
        {
            >= 0 when height >= 0 => (ImageTypeEnum.Bmp, new Size(width, height)),
            < 0 when height < 0 => (ImageTypeEnum.Bmp, new Size(-width, -height)),
            _ => ThrowHelper.ThrowArgumentException<(ImageTypeEnum imageTypeEnum, Size size)>("Unsupported image format")
        };
    }

    private static (ImageTypeEnum imageTypeEnum, Size size) DecodeJpeg(BinaryReader binaryReader)
    {
        /*
         * 段标识	  1			FF		每个新段的开始标识
         * 段类型	  1					类型编码（称作“标记码”） SOF0=0xC0
         * 段长度	  2					包括段内容和段长度本身,不包括段标识和段类型
         * 段内容						≤65533字节
         */

        // 开头两位 FF D8 (SOI文件头, 类型SOI, 没有长度和内容 JPEG协议规定）
        binaryReader.BaseStream.Seek(2, SeekOrigin.Begin);
        while (binaryReader.ReadByte() == 0xFF) // 新段的开始标识
        {
            var marker = binaryReader.ReadByte();
            var chunkLength = binaryReader.ReadLittleEndianInt16();

            if (marker == 0xC0) // SOF0
            {
                binaryReader.BaseStream.Seek(1, SeekOrigin.Current);

                int height = binaryReader.ReadLittleEndianInt16();
                int width = binaryReader.ReadLittleEndianInt16();

                return (ImageTypeEnum.Jpeg, new Size(width, height));
            }

            binaryReader.BaseStream.Seek(chunkLength - 2, SeekOrigin.Current);
        }

        return ThrowHelper.ThrowArgumentException<(ImageTypeEnum imageTypeEnum, Size size)>("Unsupported image format");
    }

    private static (ImageTypeEnum imageTypeEnum, Size size) DecodePng(BinaryReader binaryReader)
    {
        /*
         struct Png
         {
            u8  header_high_bit_byte; // must always be 0x89
            u24 header_signature[3]; // ASCII for "PNG"
            u16 header_dosLineEnding[2]; // DOS line ending("\r\n")
            u8  header_dosEOF; // DOS EOF("\x1A")
            u8  header_unixLineEnding; // UNIX line ending("\n")
            u32 chunk_i_hdr_length;
            u32 chunk_i_hdr_name;
            u32 chunk_i_hdr_i_hdr_width; // width of the image
            u32 chunk_i_hdr_i_hdr_height; // height of the image
            u8  chunk_i_hdr_i_hdr_bit_depth;
            u8  chunk_i_hdr_i_hdr_color_type;
            u8  chunk_i_hdr_i_hdr_compression_method;
            u8  chunk_i_hdr_i_hdr_filter_method;
            u8  chunk_i_hdr_i_hdr_interlacing;
         }
         */

        binaryReader.BaseStream.Seek(16, SeekOrigin.Begin);

        var width = binaryReader.ReadLittleEndianUInt32();
        var height = binaryReader.ReadLittleEndianUInt32();

        return (ImageTypeEnum.Png, new Size(width, height));
    }

    private static (ImageTypeEnum imageTypeEnum, Size size) DecodeGif(BinaryReader binaryReader)
    {
        /*
         * struct Gif
         * {
         *     u24 header_magic; // must always be 0x474946. ASCII for "GIF"
         *     u24 header_version; // version of the gif format. ASCII for "87a" or "89a"(1987年5月发布的GIF规范, 1989年7月发布的GIF规范)
         *     u16 logical_screen_descriptor_width; // width of the image
         *     u16 logical_screen_descriptor_height; // height of the image
         *     ...
         * }
         */

        binaryReader.BaseStream.Seek(6, SeekOrigin.Begin);

        var width = binaryReader.ReadUInt16();
        var height = binaryReader.ReadUInt16();

        return (ImageTypeEnum.Gif, new Size(width, height));
    }

    private static (ImageTypeEnum imageTypeEnum, Size size) DecodeIco(BinaryReader binaryReader)
    {
        /*
         * struct Icon
         * {
         *     u16  header_reserved; // must always be 0
         *     u16  header_type; // imageType: Icon = 1, Cursor = 2
         *     u16  num_images; // number of images in the file
         *     u8   images_width; // width of the image
         *     u8   images_height; // height of the image
         *     u8   images_num_colors;
         *     u8   images_reserved; // must always be 0
         *     u16  images_color_planes;
         *     u16  images_bits_per_pixel; // 比特数/像素数
         *     u32  images_image_data_size; // size of the image data
         *     u32  images_image_data_pointer_address; // image data pointer address
         *     u8[] data;
         * };
         */

        binaryReader.BaseStream.Seek(4, SeekOrigin.Begin);

        var numOfImages = binaryReader.ReadInt16(); // seek 6
        if (numOfImages == 0) return (ImageTypeEnum.Ico, new Size(0, 0));

        var width = binaryReader.ReadByte();
        var height = binaryReader.ReadByte();

        return (ImageTypeEnum.Ico, new Size(width, height));
    }
}
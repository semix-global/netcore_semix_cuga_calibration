namespace Net.Utilities.Extensions;

public static class BinaryReaderExtension
{
    public static short ReadLittleEndianInt16(this BinaryReader binaryReader)
    {
        const int shortLength = sizeof(short);

        var bytes = new byte[shortLength];
        for (var i = 0; i < shortLength; i++)
        {
            bytes[shortLength - 1 - i] = binaryReader.ReadByte();
        }

        return BitConverter.ToInt16(bytes, 0);
    }

    public static ushort ReadLittleEndianUInt16(this BinaryReader binaryReader)
    {
        const int ushortLength = sizeof(ushort);

        var bytes = new byte[ushortLength];
        for (var i = 0; i < ushortLength; i++)
        {
            bytes[ushortLength - 1 - i] = binaryReader.ReadByte();
        }

        return BitConverter.ToUInt16(bytes, 0);
    }

    public static int ReadLittleEndianInt32(this BinaryReader binaryReader)
    {
        const int intLength = sizeof(int);

        var bytes = new byte[intLength];
        for (var i = 0; i < intLength; i++)
        {
            bytes[intLength - 1 - i] = binaryReader.ReadByte();
        }

        return BitConverter.ToInt32(bytes, 0);
    }

    public static uint ReadLittleEndianUInt32(this BinaryReader binaryReader)
    {
        const int uintLength = sizeof(uint);

        var bytes = new byte[uintLength];
        for (var i = 0; i < uintLength; i++)
        {
            bytes[uintLength - 1 - i] = binaryReader.ReadByte();
        }

        return BitConverter.ToUInt32(bytes, 0);
    }

    public static long ReadLittleEndianInt64(this BinaryReader binaryReader)
    {
        const int longLength = sizeof(long);

        var bytes = new byte[longLength];
        for (var i = 0; i < longLength; i++)
        {
            bytes[longLength - 1 - i] = binaryReader.ReadByte();
        }

        return BitConverter.ToInt64(bytes, 0);
    }

    public static ulong ReadLittleEndianUInt64(this BinaryReader binaryReader)
    {
        const int ulongLength = sizeof(ulong);

        var bytes = new byte[ulongLength];
        for (var i = 0; i < ulongLength; i++)
        {
            bytes[ulongLength - 1 - i] = binaryReader.ReadByte();
        }

        return BitConverter.ToUInt64(bytes, 0);
    }
}
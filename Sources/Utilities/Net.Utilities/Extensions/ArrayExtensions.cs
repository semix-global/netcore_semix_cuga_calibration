using CommunityToolkit.Diagnostics;
using System.Runtime.InteropServices;

namespace Net.Utilities.Extensions;

public static class ArrayExtensions
{
    public static unsafe Span<T> AsSpan<T>(this T[,] @this) where T : unmanaged
    {
        fixed (T* pThis = &@this[0, 0])
        {
            return new Span<T>(pThis, @this.Length);
        }
    }

    public static unsafe Span<T> AsSpan<T>(this Array array) where T : unmanaged
    {
        var elementType = array.GetType().GetElementType();
        if (elementType != typeof(T)) ThrowHelper.ThrowArgumentException($"Expected array of {typeof(T)}, but got {elementType}");

        if (array.Length == 0) return Span<T>.Empty;

        /*var handle = GCHandle.Alloc(array, GCHandleType.Pinned);

        try
        {
            var intPtr = handle.AddrOfPinnedObject();
        }
        finally
        {
            handle.Free();
        }*/

        var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(array, 0);

        return new Span<T>(ptr.ToPointer(), array.Length);
    }
}
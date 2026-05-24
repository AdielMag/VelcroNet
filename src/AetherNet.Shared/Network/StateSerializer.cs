using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AetherNet.Network;

/// <summary>
/// Zero-allocation binary serialization for EntityState arrays.
/// Writes/reads directly into caller-supplied byte arrays — no intermediate objects.
/// </summary>
public static class StateSerializer
{
    private static readonly int EntityStateSize = Marshal.SizeOf<EntityState>();

    /// <summary>
    /// Serializes <paramref name="count"/> states into <paramref name="dst"/> starting at <paramref name="offset"/>.
    /// Returns the number of bytes written.
    /// </summary>
    public static int Serialize(EntityState[] states, int count, byte[] dst, int offset)
    {
        int bytesNeeded = count * EntityStateSize;
        if (dst.Length - offset < bytesNeeded)
            throw new ArgumentException("Destination buffer too small.");

        unsafe
        {
            fixed (EntityState* src = states)
            fixed (byte* dstPtr = dst)
            {
                Buffer.MemoryCopy(src, dstPtr + offset, bytesNeeded, bytesNeeded);
            }
        }
        return bytesNeeded;
    }

    /// <summary>
    /// Deserializes states from <paramref name="src"/> into <paramref name="dst"/>.
    /// Returns the number of EntityState entries written.
    /// </summary>
    public static int Deserialize(byte[] src, int offset, int byteCount, EntityState[] dst)
    {
        int count = byteCount / EntityStateSize;
        if (count > dst.Length)
            throw new ArgumentException("Destination array too small.");

        unsafe
        {
            fixed (byte* srcPtr = src)
            fixed (EntityState* dstPtr = dst)
            {
                Buffer.MemoryCopy(srcPtr + offset, dstPtr, byteCount, byteCount);
            }
        }
        return count;
    }

    /// <summary>Byte size for a snapshot containing <paramref name="entityCount"/> entities plus the header.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PayloadSize(int entityCount)
        => sizeof(uint) + sizeof(float) + sizeof(int) + sizeof(uint) // StateSnapshot header
           + entityCount * EntityStateSize;
}

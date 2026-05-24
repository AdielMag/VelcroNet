using System.Runtime.InteropServices;

namespace VelcroNet;

/// <summary>
/// FNV-1a hash over the raw bytes of an EntityState array.
/// Include this hash in server tick broadcasts so clients can detect desync.
/// Uses unsafe pointer walk — zero allocation.
/// </summary>
public static class DeterminismChecker
{
    private const uint FnvOffset = 2166136261u;
    private const uint FnvPrime  = 16777619u;

    private static readonly int EntityStateSize = Marshal.SizeOf<EntityState>();

    public static unsafe uint ComputeHash(EntityState[] states, int count)
    {
        uint hash = FnvOffset;
        fixed (EntityState* ptr = states)
        {
            byte* bytes     = (byte*)ptr;
            int   byteCount = count * EntityStateSize;
            for (int i = 0; i < byteCount; i++)
                hash = (hash ^ bytes[i]) * FnvPrime;
        }
        return hash;
    }
}

using System.Runtime.CompilerServices;

namespace VelcroNet
{
    /// <summary>
    /// Zero-cost conversions between UnityEngine.Vector2/3 and System.Numerics.Vector2.
    /// Both types are sequential float x, y — the unsafe reinterpret cast is bitwise safe.
    /// </summary>
    public static unsafe class MathBridge
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector2 ToNumerics(UnityEngine.Vector2 v)
        {
            // Identical memory layout: float X, float Y
            return *(System.Numerics.Vector2*)&v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Vector2 ToUnity(System.Numerics.Vector2 v)
        {
            return *(UnityEngine.Vector2*)&v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Vector3 ToUnity3(System.Numerics.Vector2 v)
            => new UnityEngine.Vector3(v.X, v.Y, 0f);

        /// <summary>
        /// Convert a Unity world position (pixels) to simulation position (meters),
        /// handling the pixel-per-meter scale and coordinate system.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector2 WorldToSim(UnityEngine.Vector2 worldPos)
        {
            var n = ToNumerics(worldPos);
            return MathExtensions.ToSimulation(in n);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Vector2 SimToWorld(System.Numerics.Vector2 simPos)
        {
            var world = MathExtensions.ToWorld(in simPos);
            return ToUnity(world);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Vector3 SimToWorld3(System.Numerics.Vector2 simPos)
        {
            var world = MathExtensions.ToWorld(in simPos);
            return ToUnity3(world);
        }
    }
}

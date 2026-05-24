using System.Runtime.CompilerServices;
using SNV2 = System.Numerics.Vector2;

namespace AetherNet;

public static class MathExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SNV2 ToSimulation(in SNV2 worldPos)
        => worldPos / SimulationConstants.PixelsPerMeter;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SNV2 ToWorld(in SNV2 simPos)
        => simPos * SimulationConstants.PixelsPerMeter;

    // Unity uses degrees CW around Z; Box2D uses radians CCW.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToSimAngle(float unityZDegrees)
        => -unityZDegrees * (float)(System.Math.PI / 180.0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToWorldAngle(float simAngleRad)
        => -simAngleRad * (float)(180.0 / System.Math.PI);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float LerpAngle(float a, float b, float t)
    {
        float delta = ((b - a + 540f) % 360f) - 180f;
        return a + delta * t;
    }
}

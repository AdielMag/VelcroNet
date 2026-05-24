using System.Runtime.InteropServices;
using SNV2 = System.Numerics.Vector2;

namespace AetherNet;

[StructLayout(LayoutKind.Sequential)]
public struct TransformState
{
    public SNV2  Position;        // simulation meters
    public float Angle;           // radians, CCW positive
    public SNV2  LinearVelocity;  // m/s
    public float AngularVelocity; // rad/s
}

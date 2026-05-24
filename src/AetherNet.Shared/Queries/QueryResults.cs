using System.Runtime.InteropServices;
using SNV2 = System.Numerics.Vector2;

namespace AetherNet.Queries;

[StructLayout(LayoutKind.Sequential)]
public struct RaycastHit
{
    public int   EntityId;
    public int   FixtureIndex;
    public SNV2  Point;
    public SNV2  Normal;
    public float Fraction; // 0–1 along ray direction
    public bool  IsTrigger;
}

[StructLayout(LayoutKind.Sequential)]
public struct OverlapResult
{
    public int EntityId;
    public int FixtureIndex;
}

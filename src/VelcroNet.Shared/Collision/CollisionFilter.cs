using System.Runtime.InteropServices;

namespace VelcroNet.Collision;

/// <summary>
/// Maps to VelcroPhysics Fixture.CollisionCategories / CollidesWith / CollisionGroup.
/// LayerIndex helpers convert a 0–15 layer int to the correct bitmask.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct CollisionFilter
{
    public ushort CategoryBits; // which layer this fixture is on
    public ushort MaskBits;     // which layers it collides with (0xFFFF = all)
    public short  GroupIndex;   // <0 = never collide same group; >0 = always; 0 = use masks

    public static CollisionFilter Default => new CollisionFilter
    {
        CategoryBits = 0x0001,
        MaskBits     = 0xFFFF,
        GroupIndex   = 0,
    };

    public static CollisionFilter FromLayer(int layer, int collisionMask = 0xFFFF)
        => new CollisionFilter
        {
            CategoryBits = (ushort)(1 << layer),
            MaskBits     = (ushort)collisionMask,
            GroupIndex   = 0,
        };
}

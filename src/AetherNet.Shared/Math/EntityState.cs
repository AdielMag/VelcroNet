using System.Runtime.InteropServices;

namespace AetherNet;

[StructLayout(LayoutKind.Sequential)]
public struct EntityState
{
    public int            EntityId;
    public TransformState Transform;
    public bool           IsAwake;
}

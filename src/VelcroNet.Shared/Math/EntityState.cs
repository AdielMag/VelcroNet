using System.Runtime.InteropServices;

namespace VelcroNet;

[StructLayout(LayoutKind.Sequential)]
public struct EntityState
{
    public int            EntityId;
    public TransformState Transform;
    public bool           IsAwake;
}

using System.Runtime.InteropServices;

namespace AetherNet.Collision;

[StructLayout(LayoutKind.Sequential)]
public struct TriggerData
{
    public int  TriggerEntityId; // the entity whose fixture has IsSensor = true
    public int  OtherEntityId;
    public uint TickNumber;
}

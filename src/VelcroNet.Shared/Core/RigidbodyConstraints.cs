using System;

namespace VelcroNet;

[Flags]
public enum RigidbodyConstraints
{
    None            = 0,
    FreezePositionX = 1 << 0,
    FreezePositionY = 1 << 1,
    FreezeRotation  = 1 << 2,
    FreezePosition  = FreezePositionX | FreezePositionY,
    FreezeAll       = FreezePosition  | FreezeRotation,
}

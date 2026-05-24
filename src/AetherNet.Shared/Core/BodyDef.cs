using nkast.Aether.Physics2D.Dynamics;
using SNV2 = System.Numerics.Vector2;

namespace AetherNet;

public struct BodyDef
{
    public BodyType              BodyType;
    public SNV2                  Position;
    public float                 Angle;
    public float                 LinearDamping;
    public float                 AngularDamping;
    public float                 GravityScale;
    public bool                  FixedRotation;
    public bool                  IsBullet;
    public RigidbodyConstraints  Constraints;

    public static BodyDef Dynamic => new BodyDef
    {
        BodyType      = BodyType.Dynamic,
        GravityScale  = 1f,
    };

    public static BodyDef Static => new BodyDef
    {
        BodyType = BodyType.Static,
    };
}

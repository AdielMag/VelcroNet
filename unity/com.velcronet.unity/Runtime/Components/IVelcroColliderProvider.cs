using nkast.Aether.Physics2D.Dynamics;

namespace VelcroNet
{
    /// <summary>
    /// Implemented by VelcroBoxCollider, VelcroCircleCollider, VelcroPolygonCollider.
    /// Called once by VelcroViewManager during scene initialization.
    /// After AttachToBody returns, the component goes dormant — no further runtime work.
    /// </summary>
    public interface IVelcroColliderProvider
    {
        void AttachToBody(Body body, PhysicsWorldManager world);
    }
}

using Genbox.VelcroPhysics.Collision.Filtering;
using Genbox.VelcroPhysics.Dynamics;
using Genbox.VelcroPhysics.Factories;
using UnityEngine;
using VelcroNet.Collision;

namespace VelcroNet
{
    [AddComponentMenu("VelcroNet/Box Collider")]
    public sealed class VelcroBoxCollider : MonoBehaviour, IVelcroColliderProvider
    {
        [SerializeField] private Vector2               _size      = Vector2.one;
        [SerializeField] private Vector2               _offset    = Vector2.zero;
        [SerializeField] private bool                  _isTrigger;
        [SerializeField] private int                   _layer;
        [SerializeField] [Range(0, 0xFFFF)] private int _collisionMask = 0xFFFF;
        [SerializeField] private VelcroPhysicsMaterial _material;

        void IVelcroColliderProvider.AttachToBody(Body body, PhysicsWorldManager world)
        {
            float density     = _material != null ? _material.Density     : 1f;
            float friction    = _material != null ? _material.Friction    : 0.2f;
            float restitution = _material != null ? _material.Restitution : 0f;

            // Convert from pixel units to simulation meters
            var simSize   = MathBridge.ToNumerics(_size)   / SimulationConstants.PixelsPerMeter;
            var simOffset = MathBridge.ToNumerics(_offset) / SimulationConstants.PixelsPerMeter;

            Fixture fixture = FixtureFactory.AttachRectangle(
                body, simSize.X, simSize.Y, density, simOffset);

            fixture.Friction    = friction;
            fixture.Restitution = restitution;
            fixture.IsSensor    = _isTrigger;

            var filter = CollisionFilter.FromLayer(_layer, _collisionMask);
            fixture.CollisionCategories = (Category)filter.CategoryBits;
            fixture.CollidesWith        = (Category)filter.MaskBits;
            fixture.CollisionGroup      = filter.GroupIndex;

            world.SubscribeFixtureEvents(fixture);
        }
    }
}

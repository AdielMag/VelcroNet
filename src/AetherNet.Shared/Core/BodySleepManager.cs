using nkast.Aether.Physics2D.Dynamics;
using SNV2 = System.Numerics.Vector2;

namespace AetherNet;

/// <summary>
/// Optionally disables Body objects (removes them from AetherPhysics broad-phase) when
/// they are far from any active focus point (e.g., camera, player position).
/// Compact index lists — zero Dictionary allocations.
/// </summary>
public sealed class BodySleepManager
{
    private readonly Body[]  _registry;         // same reference as PhysicsWorldManager._bodyRegistry
    private readonly int[]   _deactivated;      // entityIds currently disabled
    private int              _deactivatedCount;
    private readonly float   _deactivationRadiusSq;

    public BodySleepManager(Body[] bodyRegistry, float deactivationRadius = 50f)
    {
        _registry              = bodyRegistry;
        _deactivated           = new int[bodyRegistry.Length];
        _deactivationRadiusSq  = deactivationRadius * deactivationRadius;
    }

    /// <summary>
    /// Call once per second (not per tick) from the server loop.
    /// Disables bodies beyond the radius; re-enables those that come back in range.
    /// </summary>
    public void Update(in SNV2 focusPointSimUnits)
    {
        // Re-enable deactivated bodies that are now in range
        for (int i = _deactivatedCount - 1; i >= 0; i--)
        {
            int id   = _deactivated[i];
            Body body = _registry[id];
            if (body == null) { RemoveAt(i); continue; }

            float dx = body.Position.X - focusPointSimUnits.X;
            float dy = body.Position.Y - focusPointSimUnits.Y;
            if (dx * dx + dy * dy <= _deactivationRadiusSq)
            {
                body.Enabled = true;
                RemoveAt(i);
            }
        }

        // Deactivate dynamic bodies outside radius
        for (int id = 0; id < _registry.Length; id++)
        {
            Body body = _registry[id];
            if (body == null || !body.Enabled || body.BodyType != BodyType.Dynamic) continue;

            float dx = body.Position.X - focusPointSimUnits.X;
            float dy = body.Position.Y - focusPointSimUnits.Y;
            if (dx * dx + dy * dy > _deactivationRadiusSq)
            {
                body.Enabled = false;
                _deactivated[_deactivatedCount++] = id;
            }
        }
    }

    private void RemoveAt(int idx)
    {
        _deactivated[idx] = _deactivated[--_deactivatedCount];
    }
}

using System;
using SNV2 = System.Numerics.Vector2;

namespace VelcroNet.Network;

/// <summary>
/// Client-side visual smoother. Stores received authoritative snapshots and
/// lerps transform state for rendering between network ticks.
/// Zero allocation after construction.
/// </summary>
public sealed class StateInterpolator
{
    private readonly SnapshotBuffer _buffer;
    private float _renderDelay; // seconds behind server — tuned to cover jitter

    public StateInterpolator(float renderDelaySeconds = 0.1f, int bufferSlots = 8)
    {
        _buffer      = new SnapshotBuffer(bufferSlots);
        _renderDelay = renderDelaySeconds;
    }

    public void ReceiveSnapshot(uint tick, float serverTimestamp, EntityState[] states, int count)
        => _buffer.Write(tick, serverTimestamp, states, count);

    /// <summary>
    /// Samples the interpolated state for render time = <paramref name="serverTime"/> - renderDelay.
    /// Writes into <paramref name="output"/>; returns number of valid entries.
    /// </summary>
    public int Sample(float serverTime, EntityState[] output)
    {
        float renderTime = serverTime - _renderDelay;

        if (!_buffer.TryGetBracketing(renderTime, out var before, out var after)
            || before == null || after == null)
        {
            var latest = _buffer.Latest;
            if (latest == null) return 0;
            int n = latest.EntityCount;
            Array.Copy(latest.States, output, n);
            return n;
        }

        float duration = after.Timestamp - before.Timestamp;
        float alpha    = duration < 0.0001f ? 1f
                         : (renderTime - before.Timestamp) / duration;
        alpha = alpha < 0f ? 0f : alpha > 1f ? 1f : alpha;

        // Build lookup for the "after" snapshot keyed by entityId
        // We iterate "before" and match by entityId in "after"
        // This is O(n*m) worst case but n is bounded and this runs once per render frame
        int count = before.EntityCount;
        for (int i = 0; i < count; i++)
        {
            ref readonly EntityState b = ref before.States[i];
            int afterIdx = FindEntity(after.States, after.EntityCount, b.EntityId);
            if (afterIdx < 0)
            {
                output[i] = b;
                continue;
            }
            ref readonly EntityState a = ref after.States[afterIdx];
            output[i] = new EntityState
            {
                EntityId = b.EntityId,
                IsAwake  = a.IsAwake,
                Transform = new TransformState
                {
                    Position        = Lerp(b.Transform.Position, a.Transform.Position, alpha),
                    Angle           = MathExtensions.Lerp(b.Transform.Angle, a.Transform.Angle, alpha),
                    LinearVelocity  = Lerp(b.Transform.LinearVelocity, a.Transform.LinearVelocity, alpha),
                    AngularVelocity = MathExtensions.Lerp(b.Transform.AngularVelocity, a.Transform.AngularVelocity, alpha),
                },
            };
        }
        return count;
    }

    private static int FindEntity(EntityState[] arr, int count, int entityId)
    {
        for (int i = 0; i < count; i++)
            if (arr[i].EntityId == entityId) return i;
        return -1;
    }

    private static SNV2 Lerp(SNV2 a, SNV2 b, float t)
        => new SNV2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
}

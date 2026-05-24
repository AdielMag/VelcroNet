using System;
using System.Diagnostics;
using System.Threading;
using VelcroNet;
using VelcroNet.Collision;

namespace VelcroNet.Server;

/// <summary>
/// Tight fixed-rate tick loop. Drives PhysicsWorldManager.Advance() at
/// SimulationConstants.FixedTimestep and hands snapshots to an optional broadcaster.
/// </summary>
public sealed class ServerTickLoop : IFullCollisionSink
{
    private readonly PhysicsWorldManager _world;
    private readonly EntityState[]       _snapshotBuffer;
    private Action<EntityState[], int, uint>? _onSnapshot;

    public ServerTickLoop(PhysicsWorldManager world)
    {
        _world          = world;
        _snapshotBuffer = new EntityState[SimulationConstants.MaxBodies];
    }

    /// <summary>
    /// Optional delegate called after each physics tick with the current snapshot.
    /// The array reference is the manager's internal buffer — copy data before returning.
    /// </summary>
    public void SetSnapshotCallback(Action<EntityState[], int, uint> callback)
        => _onSnapshot = callback;

    public void Run(CancellationToken ct)
    {
        long targetTicks = (long)(SimulationConstants.FixedTimestep * Stopwatch.Frequency);
        var  sw          = Stopwatch.StartNew();
        long lastTicks   = sw.ElapsedTicks;

        while (!ct.IsCancellationRequested)
        {
            long  now = sw.ElapsedTicks;
            float dt  = (float)((now - lastTicks) / (double)Stopwatch.Frequency);
            lastTicks = now;

            _world.Advance(dt);

            // Drain collision events (server-side handlers can respond here)
            _world.Events.DrainAll(this);

            _world.CopyStateTo(_snapshotBuffer, out int count);
            _onSnapshot?.Invoke(_snapshotBuffer, count, _world.TickNumber);

            // Budget the remaining time in this tick
            long elapsed   = sw.ElapsedTicks - now;
            long remaining = targetTicks - elapsed;
            if (remaining > 0)
            {
                int sleepMs = (int)(remaining * 1000L / Stopwatch.Frequency);
                if (sleepMs > 1) Thread.Sleep(sleepMs - 1);
            }
        }
    }

    // IFullCollisionSink — server can respond to collision events here or subclass to override
    void ICollisionEnterSink.OnCollisionEnter(ref CollisionData data) { }
    void ICollisionExitSink .OnCollisionExit (ref CollisionData data) { }
    void ITriggerEnterSink  .OnTriggerEnter  (ref TriggerData   data) { }
    void ITriggerExitSink   .OnTriggerExit   (ref TriggerData   data) { }
}

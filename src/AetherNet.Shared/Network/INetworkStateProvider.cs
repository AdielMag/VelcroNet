namespace AetherNet.Network;

/// <summary>
/// Transport-agnostic contract. Implement this to bridge AetherNet with any networking
/// library (Mirror, FishNet, LiteNetLib, raw sockets, etc.).
/// See examples/LiteNetLibExample for a complete reference implementation.
/// </summary>
public interface INetworkStateProvider
{
    /// <summary>
    /// Called by PhysicsWorldManager after each physics tick.
    /// The <paramref name="states"/> array is the manager's internal buffer —
    /// read from it immediately; do not store the reference.
    /// </summary>
    void OnTickComplete(uint tick, EntityState[] states, int count);

    /// <summary>
    /// Feed an authoritative server snapshot into the simulation.
    /// Implement rollback / reconciliation here.
    /// </summary>
    void ApplySnapshot(uint tick, EntityState[] states, int count);
}

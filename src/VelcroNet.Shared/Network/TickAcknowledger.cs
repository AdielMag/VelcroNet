namespace VelcroNet.Network;

/// <summary>
/// Tracks which ticks have been acknowledged per connection.
/// Uses a sliding window of 64 ticks encoded as a ulong bitmask — zero allocation.
/// Useful for delta-compression: only send state diff since last acknowledged tick.
/// </summary>
public sealed class TickAcknowledger
{
    private readonly ulong[] _ackMasks;     // one per connection slot
    private readonly uint[]  _baseTicks;    // base tick for each connection's window
    private readonly int     _maxConns;

    public TickAcknowledger(int maxConnections = 64)
    {
        _maxConns  = maxConnections;
        _ackMasks  = new ulong[maxConnections];
        _baseTicks = new uint[maxConnections];
    }

    public void Acknowledge(int connectionId, uint tick)
    {
        if ((uint)connectionId >= (uint)_maxConns) return;

        uint  baseTick = _baseTicks[connectionId];
        ulong mask     = _ackMasks[connectionId];

        if (tick >= baseTick + 64)
        {
            // Advance window
            uint shift = tick - baseTick - 63;
            mask       = mask >> (int)shift;
            baseTick  += shift;
            _baseTicks[connectionId] = baseTick;
        }

        if (tick >= baseTick)
        {
            int bit = (int)(tick - baseTick);
            _ackMasks[connectionId] = mask | (1UL << bit);
        }
    }

    /// <summary>Returns the highest consecutively acknowledged tick for this connection.</summary>
    public uint GetAcknowledgedUpTo(int connectionId)
    {
        if ((uint)connectionId >= (uint)_maxConns) return 0;
        ulong mask = _ackMasks[connectionId];
        uint  base_ = _baseTicks[connectionId];
        int   run   = 0;
        while (run < 64 && (mask & (1UL << run)) != 0) run++;
        return base_ + (uint)(run > 0 ? run - 1 : 0);
    }

    public bool IsAcknowledged(int connectionId, uint tick)
    {
        if ((uint)connectionId >= (uint)_maxConns) return false;
        uint base_ = _baseTicks[connectionId];
        if (tick < base_ || tick >= base_ + 64) return false;
        return (_ackMasks[connectionId] & (1UL << (int)(tick - base_))) != 0;
    }

    public void Reset(int connectionId)
    {
        if ((uint)connectionId >= (uint)_maxConns) return;
        _ackMasks[connectionId]  = 0;
        _baseTicks[connectionId] = 0;
    }
}

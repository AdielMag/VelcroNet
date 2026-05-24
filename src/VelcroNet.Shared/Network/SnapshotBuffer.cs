namespace VelcroNet.Network;

/// <summary>
/// Fixed-size circular buffer of AuthoritativeSnapshot instances.
/// Each slot owns its EntityState array — allocated once at construction.
/// </summary>
public sealed class SnapshotBuffer
{
    public sealed class AuthoritativeSnapshot
    {
        public uint          TickNumber;
        public float         Timestamp;
        public EntityState[] States;
        public int           EntityCount;
        public bool          IsValid;

        public AuthoritativeSnapshot(int capacity)
            => States = new EntityState[capacity];
    }

    private readonly AuthoritativeSnapshot[] _slots;
    private int _head;
    private int _count;
    private readonly int _capacity;

    public SnapshotBuffer(int slotCount = 8, int entitiesPerSlot = SimulationConstants.MaxBodies)
    {
        _capacity = slotCount;
        _slots    = new AuthoritativeSnapshot[slotCount];
        for (int i = 0; i < slotCount; i++)
            _slots[i] = new AuthoritativeSnapshot(entitiesPerSlot);
    }

    /// <summary>Write a new snapshot. Overwrites oldest when full.</summary>
    public void Write(uint tick, float timestamp, EntityState[] states, int count)
    {
        int slot = (_head + _count) % _capacity;
        if (_count == _capacity)
            _head = (_head + 1) % _capacity; // overwrite oldest
        else
            _count++;

        var s = _slots[slot];
        s.TickNumber   = tick;
        s.Timestamp    = timestamp;
        s.EntityCount  = count;
        s.IsValid      = true;
        System.Array.Copy(states, s.States, count);
    }

    /// <summary>Returns the two snapshots that bracket <paramref name="renderTime"/>, or null if unavailable.</summary>
    public bool TryGetBracketing(float renderTime,
        out AuthoritativeSnapshot? before,
        out AuthoritativeSnapshot? after)
    {
        before = after = null;
        if (_count < 2) return false;

        for (int i = 0; i < _count - 1; i++)
        {
            int idxA = (_head + i)     % _capacity;
            int idxB = (_head + i + 1) % _capacity;
            if (_slots[idxA].Timestamp <= renderTime && _slots[idxB].Timestamp >= renderTime)
            {
                before = _slots[idxA];
                after  = _slots[idxB];
                return true;
            }
        }
        return false;
    }

    public AuthoritativeSnapshot? Latest
        => _count == 0 ? null : _slots[(_head + _count - 1) % _capacity];
}

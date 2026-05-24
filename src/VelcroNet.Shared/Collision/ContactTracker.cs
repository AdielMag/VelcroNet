namespace VelcroNet.Collision;

/// <summary>
/// Open-addressing hash table for tracking active contacts (solid) and active triggers.
/// Converts VelcroPhysics per-step OnCollision callbacks into true Enter/Exit events.
/// Pre-allocated at construction — zero heap allocation during gameplay.
/// </summary>
public sealed class ContactTracker
{
    private readonly ulong[] _solidKeys;
    private readonly ulong[] _triggerKeys;
    private readonly bool[]  _solidOccupied;
    private readonly bool[]  _triggerOccupied;
    private readonly int     _capacity;

    private int _solidCount;
    private int _triggerCount;

    public ContactTracker(int capacity = SimulationConstants.MaxContacts)
    {
        _capacity        = capacity;
        _solidKeys       = new ulong[capacity];
        _solidOccupied   = new bool[capacity];
        _triggerKeys     = new ulong[capacity];
        _triggerOccupied = new bool[capacity];
    }

    // Returns true if the pair was newly added (first contact this step).
    public bool TryAddNew(int idA, int idB, uint tick, out bool isNew)
    {
        ulong key = MakeKey(idA, idB);
        int slot  = FindSlot(_solidKeys, _solidOccupied, key);
        if (slot < 0) { isNew = false; return false; }

        if (_solidOccupied[slot])
        {
            isNew = false;
            return true;
        }

        _solidKeys[slot]     = key;
        _solidOccupied[slot] = true;
        _solidCount++;
        isNew = true;
        return true;
    }

    public bool Remove(int idA, int idB)
    {
        ulong key  = MakeKey(idA, idB);
        int   slot = FindExisting(_solidKeys, _solidOccupied, key);
        if (slot < 0) return false;

        _solidOccupied[slot] = false;
        _solidKeys[slot]     = 0;
        _solidCount--;
        return true;
    }

    public bool TryAddTrigger(int idA, int idB, uint tick, out bool isNew)
    {
        ulong key = MakeKey(idA, idB);
        int slot  = FindSlot(_triggerKeys, _triggerOccupied, key);
        if (slot < 0) { isNew = false; return false; }

        if (_triggerOccupied[slot])
        {
            isNew = false;
            return true;
        }

        _triggerKeys[slot]     = key;
        _triggerOccupied[slot] = true;
        _triggerCount++;
        isNew = true;
        return true;
    }

    public bool RemoveTrigger(int idA, int idB)
    {
        ulong key  = MakeKey(idA, idB);
        int   slot = FindExisting(_triggerKeys, _triggerOccupied, key);
        if (slot < 0) return false;

        _triggerOccupied[slot] = false;
        _triggerKeys[slot]     = 0;
        _triggerCount--;
        return true;
    }

    private int FindSlot(ulong[] keys, bool[] occupied, ulong key)
    {
        int hash    = (int)((key ^ (key >> 32)) & 0x7FFFFFFF);
        int start   = hash % _capacity;
        int current = start;

        do
        {
            if (!occupied[current] || keys[current] == key)
                return current;
            current = (current + 1) % _capacity;
        } while (current != start);

        return -1; // table full
    }

    private int FindExisting(ulong[] keys, bool[] occupied, ulong key)
    {
        int hash    = (int)((key ^ (key >> 32)) & 0x7FFFFFFF);
        int start   = hash % _capacity;
        int current = start;

        do
        {
            if (occupied[current] && keys[current] == key)
                return current;
            if (!occupied[current])
                return -1;
            current = (current + 1) % _capacity;
        } while (current != start);

        return -1;
    }

    private static ulong MakeKey(int idA, int idB)
    {
        int lo = idA < idB ? idA : idB;
        int hi = idA < idB ? idB : idA;
        return ((ulong)(uint)lo << 32) | (uint)hi;
    }
}

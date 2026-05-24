using System;
using System.Diagnostics;

namespace VelcroNet.Collision;

/// <summary>
/// Four pre-allocated ring buffers (solid enter/exit, trigger enter/exit).
/// Events are written inline during World.Step and drained after by the caller.
/// Thread-safe assumptions: single-threaded only — no locks.
/// </summary>
public sealed class CollisionEventQueue
{
    private const int DefaultCapacity = 512;

    private readonly CollisionData[] _enterBuffer;
    private readonly CollisionData[] _exitBuffer;
    private readonly TriggerData[]   _trigEnterBuffer;
    private readonly TriggerData[]   _trigExitBuffer;

    private int _enterHead,    _enterCount;
    private int _exitHead,     _exitCount;
    private int _trigEnterHead, _trigEnterCount;
    private int _trigExitHead,  _trigExitCount;

    private readonly int _capacity;

    public CollisionEventQueue(int capacity = DefaultCapacity)
    {
        _capacity        = capacity;
        _enterBuffer     = new CollisionData[capacity];
        _exitBuffer      = new CollisionData[capacity];
        _trigEnterBuffer = new TriggerData[capacity];
        _trigExitBuffer  = new TriggerData[capacity];
    }

    public void EnqueueEnter(in CollisionData data)     => Enqueue(_enterBuffer,     ref _enterHead,     ref _enterCount,     in data);
    public void EnqueueExit(in CollisionData data)      => Enqueue(_exitBuffer,      ref _exitHead,      ref _exitCount,      in data);
    public void EnqueueTriggerEnter(in TriggerData data) => Enqueue(_trigEnterBuffer, ref _trigEnterHead, ref _trigEnterCount, in data);
    public void EnqueueTriggerExit(in TriggerData data)  => Enqueue(_trigExitBuffer,  ref _trigExitHead,  ref _trigExitCount,  in data);

    public void DrainEnter(ICollisionEnterSink sink)
    {
        while (_enterCount > 0)
        {
            ref CollisionData d = ref _enterBuffer[_enterHead];
            sink.OnCollisionEnter(ref d);
            _enterHead = (_enterHead + 1) % _capacity;
            _enterCount--;
        }
    }

    public void DrainExit(ICollisionExitSink sink)
    {
        while (_exitCount > 0)
        {
            ref CollisionData d = ref _exitBuffer[_exitHead];
            sink.OnCollisionExit(ref d);
            _exitHead = (_exitHead + 1) % _capacity;
            _exitCount--;
        }
    }

    public void DrainTriggerEnter(ITriggerEnterSink sink)
    {
        while (_trigEnterCount > 0)
        {
            ref TriggerData d = ref _trigEnterBuffer[_trigEnterHead];
            sink.OnTriggerEnter(ref d);
            _trigEnterHead = (_trigEnterHead + 1) % _capacity;
            _trigEnterCount--;
        }
    }

    public void DrainTriggerExit(ITriggerExitSink sink)
    {
        while (_trigExitCount > 0)
        {
            ref TriggerData d = ref _trigExitBuffer[_trigExitHead];
            sink.OnTriggerExit(ref d);
            _trigExitHead = (_trigExitHead + 1) % _capacity;
            _trigExitCount--;
        }
    }

    public void DrainAll(IFullCollisionSink sink)
    {
        DrainEnter(sink);
        DrainExit(sink);
        DrainTriggerEnter(sink);
        DrainTriggerExit(sink);
    }

    // Discard all pending events without dispatching (e.g. scene unload).
    public void Clear()
    {
        _enterCount = _exitCount = _trigEnterCount = _trigExitCount = 0;
        _enterHead  = _exitHead  = _trigEnterHead  = _trigExitHead  = 0;
    }

    private void Enqueue(CollisionData[] buf, ref int head, ref int count, in CollisionData data)
    {
        int idx = (head + count) % _capacity;
        buf[idx] = data;
        if (count < _capacity)
        {
            count++;
        }
        else
        {
            // Ring is full — drop oldest and advance head
            head = (head + 1) % _capacity;
            Debug.WriteLine("[VelcroNet] CollisionEventQueue overflow — oldest collision event dropped.");
        }
    }

    private void Enqueue(TriggerData[] buf, ref int head, ref int count, in TriggerData data)
    {
        int idx = (head + count) % _capacity;
        buf[idx] = data;
        if (count < _capacity)
        {
            count++;
        }
        else
        {
            head = (head + 1) % _capacity;
            Debug.WriteLine("[VelcroNet] CollisionEventQueue overflow — oldest trigger event dropped.");
        }
    }
}

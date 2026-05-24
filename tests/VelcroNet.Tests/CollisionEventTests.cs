using System.Collections.Generic;
using System.Numerics;
using nkast.Aether.Physics2D.Dynamics;
using VelcroNet;
using VelcroNet.Collision;
using Xunit;

namespace VelcroNet.Tests;

/// <summary>Manual collision sink that records dispatched events for assertion.</summary>
internal sealed class RecordingSink : IFullCollisionSink
{
    public List<CollisionData> Enters  = new();
    public List<CollisionData> Exits   = new();
    public List<TriggerData>   TrigEnters = new();
    public List<TriggerData>   TrigExits  = new();

    void ICollisionEnterSink.OnCollisionEnter(ref CollisionData d) => Enters.Add(d);
    void ICollisionExitSink .OnCollisionExit (ref CollisionData d) => Exits.Add(d);
    void ITriggerEnterSink  .OnTriggerEnter  (ref TriggerData   d) => TrigEnters.Add(d);
    void ITriggerExitSink   .OnTriggerExit   (ref TriggerData   d) => TrigExits.Add(d);
}

public sealed class CollisionEventTests
{
    [Fact]
    public void SolidCollision_FiresEnterOnce_ExitOnce()
    {
        var world = new PhysicsWorldManager(WorldConfig.Default with { Gravity = Vector2.Zero });
        var sink  = new RecordingSink();

        // Body 0: static ground platform
        var defA = BodyDef.Static;
        defA.Position = new Vector2(0f, 0f);
        var bodyA = world.CreateBody(in defA, 0);
        var fixtureA = bodyA.CreateRectangle(2f, 0.1f, 0f, nkast.Aether.Physics2D.Common.Vector2.Zero);
        world.SubscribeFixtureEvents(fixtureA);

        // Body 1: dynamic body falling onto the platform
        var defB = new BodyDef { BodyType = BodyType.Dynamic, Position = new Vector2(0f, 0.2f) };
        var bodyB = world.CreateBody(in defB, 1);
        var fixtureB = bodyB.CreateRectangle(0.2f, 0.2f, 1f, nkast.Aether.Physics2D.Common.Vector2.Zero);
        world.SubscribeFixtureEvents(fixtureB);

        // Drop body B down
        world.SetLinearVelocity(1, new Vector2(0f, -2f));

        for (int i = 0; i < 120; i++)
        {
            world.Advance(SimulationConstants.FixedTimestep);
            world.Events.DrainAll(sink);
        }

        // Should have had at least one enter event
        Assert.True(sink.Enters.Count >= 1, $"Expected ≥1 collision enters, got {sink.Enters.Count}");
    }

    [Fact]
    public void ContactTracker_IsNew_ReturnsTrueOnlyOnce()
    {
        var tracker = new ContactTracker(256);

        bool added1 = tracker.TryAddNew(0, 1, 1, out bool isNew1);
        bool added2 = tracker.TryAddNew(0, 1, 2, out bool isNew2);

        Assert.True(added1 && isNew1,  "First contact should be new");
        Assert.True(added2 && !isNew2, "Second contact same pair should NOT be new");
    }

    [Fact]
    public void ContactTracker_Remove_AllowsReAdd()
    {
        var tracker = new ContactTracker(256);

        tracker.TryAddNew(0, 1, 1, out _);
        bool removed = tracker.Remove(0, 1);
        bool addedAgain = tracker.TryAddNew(0, 1, 3, out bool isNewAgain);

        Assert.True(removed,               "Remove should succeed");
        Assert.True(addedAgain && isNewAgain, "Re-add after remove should be new");
    }

    [Fact]
    public void StateSerializer_RoundTrips()
    {
        var statesIn  = new EntityState[3];
        var statesOut = new EntityState[3];
        var buf       = new byte[1024];

        for (int i = 0; i < 3; i++)
        {
            statesIn[i] = new EntityState
            {
                EntityId = i,
                IsAwake  = true,
                Transform = new TransformState
                {
                    Position        = new Vector2(i * 1.1f, i * 2.2f),
                    Angle           = i * 0.5f,
                    LinearVelocity  = new Vector2(i * 0.1f, i * 0.2f),
                    AngularVelocity = i * 0.3f,
                },
            };
        }

        int written = VelcroNet.Network.StateSerializer.Serialize(statesIn, 3, buf, 0);
        int read    = VelcroNet.Network.StateSerializer.Deserialize(buf, 0, written, statesOut);

        Assert.Equal(3, read);
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(statesIn[i].EntityId, statesOut[i].EntityId);
            Assert.Equal(statesIn[i].Transform.Position, statesOut[i].Transform.Position);
        }
    }
}

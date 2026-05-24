using Genbox.VelcroPhysics.Dynamics;
using System.Numerics;
using VelcroNet;
using Xunit;

namespace VelcroNet.Tests;

public sealed class ForceApiTests
{
    private static PhysicsWorldManager CreateWorld()
        => new PhysicsWorldManager(WorldConfig.Default with { Gravity = Vector2.Zero });

    [Fact]
    public void Impulse_ChangesVelocityByImpulseOverMass()
    {
        var world = CreateWorld();
        var def   = new BodyDef { BodyType = BodyType.Dynamic, GravityScale = 0f };
        world.CreateBody(in def, entityId: 0);

        float mass    = world.GetMass(0); // default mass after no fixtures ~ 1
        var impulse   = new Vector2(10f, 0f);
        world.ApplyForce(0, in impulse, ForceMode.Impulse);

        // Step once to let impulse register
        world.Advance(SimulationConstants.FixedTimestep);

        Vector2 vel = world.GetLinearVelocity(0);

        // Velocity should approximately equal impulse / mass
        float expected = mass > 0f ? impulse.X / mass : impulse.X;
        Assert.True(System.Math.Abs(vel.X - expected) < 0.01f,
            $"Expected vx ≈ {expected}, got {vel.X}");
    }

    [Fact]
    public void VelocityChange_ChangesVelocityRegardlessOfMass()
    {
        var world = CreateWorld();
        var def   = new BodyDef { BodyType = BodyType.Dynamic, GravityScale = 0f };
        world.CreateBody(in def, 0);

        var delta = new Vector2(5f, 0f);
        world.ApplyForce(0, in delta, ForceMode.VelocityChange);
        world.Advance(SimulationConstants.FixedTimestep);

        Vector2 vel = world.GetLinearVelocity(0);
        Assert.True(System.Math.Abs(vel.X - delta.X) < 0.1f,
            $"Expected vx ≈ {delta.X}, got {vel.X}");
    }

    [Fact]
    public void FreezePositionX_LocksXAxis()
    {
        var world = CreateWorld();
        var def = new BodyDef
        {
            BodyType    = BodyType.Dynamic,
            GravityScale = 0f,
            Constraints = RigidbodyConstraints.FreezePositionX,
        };
        world.CreateBody(in def, 0);

        var impulse = new Vector2(100f, 0f);
        world.ApplyForce(0, in impulse, ForceMode.Impulse);

        for (int i = 0; i < 60; i++)
            world.Advance(SimulationConstants.FixedTimestep);

        var state = world.GetBodyState(0);
        Assert.True(System.Math.Abs(state.Position.X) < 0.001f,
            $"Expected X ≈ 0, got {state.Position.X}");
    }

    [Fact]
    public void StaticBody_IgnoresForce()
    {
        var world = CreateWorld();
        var def   = BodyDef.Static;
        world.CreateBody(in def, 0);

        var force = new Vector2(1000f, 0f);
        world.ApplyForce(0, in force, ForceMode.Impulse);
        world.Advance(SimulationConstants.FixedTimestep);

        var state = world.GetBodyState(0);
        Assert.Equal(0f, state.Position.X);
        Assert.Equal(0f, state.LinearVelocity.X);
    }
}

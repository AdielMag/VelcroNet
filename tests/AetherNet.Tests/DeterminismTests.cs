using nkast.Aether.Physics2D.Dynamics;
using AetherNet;
using Xunit;

namespace AetherNet.Tests;

public sealed class DeterminismTests
{
    private static PhysicsWorldManager CreateWorld()
        => new PhysicsWorldManager(WorldConfig.Default);

    private static void SeedWorld(PhysicsWorldManager world)
    {
        for (int i = 0; i < 10; i++)
        {
            var def = new BodyDef
            {
                BodyType     = BodyType.Dynamic,
                Position     = new System.Numerics.Vector2(i * 0.5f, i * 0.3f),
                GravityScale = 1f,
            };
            world.CreateBody(in def, i);
        }
    }

    [Fact]
    public void SameInputProducesSameSnapshot()
    {
        var worldA = CreateWorld();
        var worldB = CreateWorld();

        SeedWorld(worldA);
        SeedWorld(worldB);

        var bufA = new EntityState[SimulationConstants.MaxBodies];
        var bufB = new EntityState[SimulationConstants.MaxBodies];

        for (int tick = 0; tick < 600; tick++)
        {
            worldA.Advance(SimulationConstants.FixedTimestep);
            worldB.Advance(SimulationConstants.FixedTimestep);
        }

        worldA.CopyStateTo(bufA, out int countA);
        worldB.CopyStateTo(bufB, out int countB);

        Assert.Equal(countA, countB);

        uint hashA = DeterminismChecker.ComputeHash(bufA, countA);
        uint hashB = DeterminismChecker.ComputeHash(bufB, countB);

        Assert.Equal(hashA, hashB);
    }

    [Fact]
    public void DeterminismHashDiffersForDifferentStates()
    {
        var worldA = CreateWorld();
        var worldB = CreateWorld();

        SeedWorld(worldA);
        SeedWorld(worldB);

        // Advance A further than B to get divergent state
        for (int tick = 0; tick < 600; tick++) worldA.Advance(SimulationConstants.FixedTimestep);
        for (int tick = 0; tick < 300; tick++) worldB.Advance(SimulationConstants.FixedTimestep);

        var bufA = new EntityState[SimulationConstants.MaxBodies];
        var bufB = new EntityState[SimulationConstants.MaxBodies];

        worldA.CopyStateTo(bufA, out int countA);
        worldB.CopyStateTo(bufB, out int countB);

        uint hashA = DeterminismChecker.ComputeHash(bufA, countA);
        uint hashB = DeterminismChecker.ComputeHash(bufB, countB);

        Assert.NotEqual(hashA, hashB);
    }
}

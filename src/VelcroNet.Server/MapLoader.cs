using System;
using System.IO;
using System.Text.Json;
using Genbox.VelcroPhysics.Collision.Filtering;
using Genbox.VelcroPhysics.Dynamics;
using Genbox.VelcroPhysics.Factories;
using Genbox.VelcroPhysics.Shared;
using VelcroNet;
using VelcroNet.Collision;
using SNV2 = System.Numerics.Vector2;

namespace VelcroNet.Server;

/// <summary>
/// Parses a baked JSON map file and constructs the corresponding bodies and
/// fixtures in the provided PhysicsWorldManager.
/// </summary>
public sealed class MapLoader
{
    // Reused across multiple polygon definitions — avoid repeated List allocations
    private readonly Vertices _scratchVertices = new Vertices(64);

    public MapData Load(string jsonPath)
    {
        string json = File.ReadAllText(jsonPath);
        return JsonSerializer.Deserialize(json, MapSerializerContext.Default.MapData)
               ?? throw new InvalidOperationException($"Failed to deserialize map at '{jsonPath}'.");
    }

    public void LoadInto(PhysicsWorldManager world, string jsonPath)
    {
        MapData map = Load(jsonPath);
        ApplyTo(world, map);
    }

    public void ApplyTo(PhysicsWorldManager world, MapData map)
    {
        foreach (BakedEntityDef entity in map.Entities)
        {
            var def = new BodyDef
            {
                BodyType       = entity.BodyType,
                Position       = new SNV2(entity.PositionX, entity.PositionY),
                Angle          = entity.Angle,
                LinearDamping  = entity.LinearDamping,
                AngularDamping = entity.AngularDamping,
                GravityScale   = entity.GravityScale,
                FixedRotation  = entity.FixedRotation,
                Constraints    = entity.Constraints,
            };

            Body body = world.CreateBody(in def, entity.EntityId);

            foreach (BakedFixtureDef fixtureData in entity.Fixtures)
                AttachFixture(world, body, fixtureData);
        }
    }

    private void AttachFixture(PhysicsWorldManager world, Body body, BakedFixtureDef data)
    {
        var offset = new SNV2(data.OffsetX, data.OffsetY);

        Fixture fixture = data.Shape switch
        {
            BakedFixtureShape.Box     => FixtureFactory.AttachRectangle(body, data.Width, data.Height, data.Density, offset),
            BakedFixtureShape.Circle  => FixtureFactory.AttachCircle(body, data.Radius, data.Density, offset),
            BakedFixtureShape.Polygon => AttachPolygon(body, data),
            _ => throw new ArgumentOutOfRangeException(nameof(data.Shape))
        };

        fixture.Friction    = data.Friction;
        fixture.Restitution = data.Restitution;
        fixture.IsSensor    = data.IsSensor;

        ApplyFilter(fixture, data.Layer, data.CollisionMask);
        world.SubscribeFixtureEvents(fixture);
    }

    private Fixture AttachPolygon(Body body, BakedFixtureDef data)
    {
        _scratchVertices.Clear();
        int count = Math.Min(data.VerticesX.Length, data.VerticesY.Length);
        for (int i = 0; i < count; i++)
            _scratchVertices.Add(new SNV2(data.VerticesX[i], data.VerticesY[i]));

        return FixtureFactory.AttachPolygon(body, _scratchVertices, data.Density);
    }

    private static void ApplyFilter(Fixture fixture, int layer, int mask)
    {
        var filter = CollisionFilter.FromLayer(layer, mask);
        fixture.CollisionCategories = (Category)filter.CategoryBits;
        fixture.CollidesWith        = (Category)filter.MaskBits;
        fixture.CollisionGroup      = filter.GroupIndex;
    }
}

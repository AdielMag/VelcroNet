# AetherNet — Developer Notes

## Physics Backend

AetherNet uses [Aether.Physics2D 2.2.0](https://github.com/nkast/Aether.Physics2D) as its physics backend.

- **Package**: `nkast.Aether.Physics2D` on NuGet
- **Namespace prefix**: `nkast.Aether.Physics2D.*`
- **API style**: Box2D 2.x (Farseer lineage)
- **Pure C#, no native deps** — works on Unity IL2CPP (iOS, Android, WebGL)
- **Targets**: netstandard2.0 + net8.0

## Vector2 Boundary

The public API (`EntityState`, `TransformState`, `BodyDef`, force methods, query results) uses `System.Numerics.Vector2`.

The physics backend uses `nkast.Aether.Physics2D.Common.Vector2`.

`AetherInterop.ToAether(SNV2)` / `AetherInterop.FromAether(AVec2)` convert at the boundary (inlined, zero overhead).

## Key API Mapping

| Task | Aether.Physics2D |
|---|---|
| Create body | `World.CreateBody(pos, angle, BodyType)` |
| Step | `World.Step(float dt)` |
| Remove body | `World.Remove(Body)` |
| Attach box | `Body.CreateRectangle(w, h, density, offset)` |
| Attach circle | `Body.CreateCircle(r, density, offset)` |
| Attach polygon | `Body.CreatePolygon(Vertices, density)` |
| User data | `Body.Tag` (object) |
| Awake state | `Body.Awake` (bool) |
| Collision delegate | `OnCollisionEventHandler` |
| Separation delegate | `OnSeparationEventHandler` |
| Raycast delegate | `RayCastReportFixtureDelegate` |

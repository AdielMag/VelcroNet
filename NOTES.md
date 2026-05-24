# API Verification Notes

When integrating with the actual Genbox.VelcroPhysics NuGet package, verify these
call sites in case the library version you install has a slightly different signature.

## 1. `World.Step` signature
Expected: `void Step(float timeStep, int velocityIterations = 8, int positionIterations = 3)`
Used in: `PhysicsWorldManager.Advance()`

## 2. `World.Remove(Body)` vs `World.DestroyBody(Body)`
Expected: `void Remove(Body body)`
Used in: `PhysicsWorldManager.DestroyBody()`

## 3. `Fixture.CollisionCategories` / `CollidesWith` type
Expected: `Category` enum from `Genbox.VelcroPhysics.Collision.Filtering`
Used in: `VelcroBoxCollider`, `VelcroCircleCollider`, `VelcroPolygonCollider`, `MapLoader`
If compilation fails, update the `using` alias or cast target.

## 4. `Contact.GetWorldManifold` + `FixedArray2<Vector2>` indexer
Expected: `void GetWorldManifold(out Vector2 normal, out FixedArray2<Vector2> points)`
and `points[0]` or `points.Value0` for the first contact point.
Used in: `PhysicsWorldManager.HandleCollision()`
If `FixedArray2<T>` has no indexer, replace `points[0]` with `points.Value0`.

## 5. `OnSeparationEventHandler` signature
Expected: `delegate void OnSeparationEventHandler(Fixture sender, Fixture other, Contact contact)`
Used in: `PhysicsWorldManager._onSeparationHandler`
If the Contact parameter is absent, change the handler signature to `(Fixture, Fixture)`.

## 6. `FixtureFactory.AttachRectangle` parameter order
Expected: `(Body body, float width, float height, float density, Vector2 offset = default)`
If the library uses a different order (e.g., body last), update all factory call sites.

## 7. `Vertices` class location
Expected: `Genbox.VelcroPhysics.Shared.Vertices` extending `List<Vector2>`
Used in: `VelcroPolygonCollider.Awake()`, `MapLoader.AttachPolygon()`

## 8. `Body.Rotation` vs `Body.Angle`
Expected: `float Rotation` (radians)
Used throughout as the body's current angle.
If the property is named `Angle`, update all references.

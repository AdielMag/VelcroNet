using System;
using Genbox.VelcroPhysics.Collision.ContactSystem;
using Genbox.VelcroPhysics.Dynamics;
using Genbox.VelcroPhysics.Factories;
using VelcroNet.Collision;
using VelcroNet.Network;
using VelcroNet.Queries;
using SNV2 = System.Numerics.Vector2;

namespace VelcroNet;

/// <summary>
/// Owns the VelcroPhysics World, the fixed-timestep accumulator, the body registry,
/// and the collision event queues. This class is the same on server and Unity client.
/// </summary>
public sealed class PhysicsWorldManager
{
    // --- Physics world ---
    private readonly World _world;

    // --- Body registry (parallel arrays, indexed by entityId) ---
    private readonly Body[]                 _bodyRegistry;
    private readonly EntityToken[]          _entityTokens;  // Body.UserData without boxing
    private readonly EntityState[]          _stateBuffer;   // for CopyStateTo / snapshot
    private readonly RigidbodyConstraints[] _constraints;
    private readonly SNV2[]                 _frozenPositions;

    // --- Compact list of bodies with positional freeze constraints ---
    private readonly int[] _frozenIds;
    private int            _frozenCount;

    // --- Counters ---
    private int  _activeBodyCount;
    private uint _tickNumber;

    // --- Fixed-timestep accumulator ---
    private float _accumulator;

    // --- Collision system ---
    private readonly CollisionEventQueue _events;
    private readonly ContactTracker      _contactTracker;

    // Pre-allocated delegates — subscribed to fixtures at CreateBody time, never reallocated.
    private readonly Func<Fixture, Fixture, Contact, bool> _onCollisionHandler;
    private readonly Action<Fixture, Fixture, Contact>     _onSeparationHandler;

    // --- Query system ---
    private readonly PhysicsQueryBuffer _queryBuffer;
    private readonly Func<Fixture, SNV2, SNV2, float, float> _rayCastCallback;
    private PhysicsQueryBuffer? _activeQueryBuffer; // set during Raycast call

    // --- Optional network provider ---
    private INetworkStateProvider? _networkProvider;

    private readonly int _maxBodies;

    // ─────────────────────────────────────────────────────────────────────────
    // Construction
    // ─────────────────────────────────────────────────────────────────────────

    public PhysicsWorldManager(in WorldConfig config)
    {
        _maxBodies = config.MaxBodies > 0 ? config.MaxBodies : SimulationConstants.MaxBodies;

        _world = new World(config.Gravity);
        _world.Enabled = true;

        _bodyRegistry    = new Body[_maxBodies];
        _entityTokens    = new EntityToken[_maxBodies];
        _stateBuffer     = new EntityState[_maxBodies];
        _constraints     = new RigidbodyConstraints[_maxBodies];
        _frozenPositions = new SNV2[_maxBodies];
        _frozenIds       = new int[_maxBodies];

        _events         = new CollisionEventQueue();
        _contactTracker = new ContactTracker(SimulationConstants.MaxContacts);
        _queryBuffer    = new PhysicsQueryBuffer();

        // Allocate delegate wrappers ONCE — never again during gameplay
        _onCollisionHandler  = HandleCollision;
        _onSeparationHandler = HandleSeparation;
        _rayCastCallback     = RayCastCallback;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Alpha in [0, 1] for visual interpolation between the last two physics ticks.</summary>
    public float InterpolationAlpha
        => _accumulator / SimulationConstants.FixedTimestep;

    public uint  TickNumber      => _tickNumber;
    public int   ActiveBodyCount => _activeBodyCount;

    /// <summary>
    /// The raw event queue. Drain after each Advance() call from VelcroViewManager or server loop.
    /// </summary>
    public CollisionEventQueue Events => _events;

    // ─────────────────────────────────────────────────────────────────────────
    // Simulation tick
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Advance the simulation by <paramref name="deltaTime"/> seconds.
    /// May execute zero, one, or more fixed steps depending on accumulator.
    /// </summary>
    public void Advance(float deltaTime)
    {
        _accumulator += deltaTime;
        while (_accumulator >= SimulationConstants.FixedTimestep)
        {
            _world.Step(
                SimulationConstants.FixedTimestep,
                SimulationConstants.VelocityIterations,
                SimulationConstants.PositionIterations);

            ApplyFreezeConstraints();
            _tickNumber++;
            _accumulator -= SimulationConstants.FixedTimestep;
        }

        // Network provider gets a snapshot after all steps are done for this frame
        if (_networkProvider != null)
        {
            CopyStateTo(_stateBuffer, out int count);
            _networkProvider.OnTickComplete(_tickNumber, _stateBuffer, count);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Body lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Register a body with the given entityId. entityId must be 0 ≤ id &lt; MaxBodies.</summary>
    public Body CreateBody(in BodyDef def, int entityId)
    {
        if ((uint)entityId >= (uint)_maxBodies)
            throw new ArgumentOutOfRangeException(nameof(entityId), $"entityId {entityId} out of range [0, {_maxBodies}).");
        if (_bodyRegistry[entityId] != null)
            throw new InvalidOperationException($"Entity {entityId} is already registered.");

        Body body = _world.CreateBody(def.Position, def.Angle, def.BodyType);
        body.LinearDamping  = def.LinearDamping;
        body.AngularDamping = def.AngularDamping;
        body.GravityScale   = def.GravityScale;
        body.FixedRotation  = def.FixedRotation
                              || (def.Constraints & RigidbodyConstraints.FreezeRotation) != 0;
        body.IsBullet = def.IsBullet;

        var token = new EntityToken { EntityId = entityId };
        _entityTokens[entityId]  = token;
        body.UserData            = token;

        _bodyRegistry[entityId]    = body;
        _constraints[entityId]     = def.Constraints;
        _frozenPositions[entityId] = def.Position;

        if ((def.Constraints & (RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY)) != 0)
            _frozenIds[_frozenCount++] = entityId;

        _activeBodyCount++;
        return body;
    }

    public void DestroyBody(int entityId)
    {
        Body? body = GetBodyOrNull(entityId);
        if (body == null) return;

        _world.Remove(body);
        _bodyRegistry[entityId]  = null!;
        _entityTokens[entityId]  = null!;
        _constraints[entityId]   = RigidbodyConstraints.None;
        _activeBodyCount--;

        // Remove from frozen compact list
        for (int i = 0; i < _frozenCount; i++)
        {
            if (_frozenIds[i] == entityId)
            {
                _frozenIds[i] = _frozenIds[--_frozenCount];
                break;
            }
        }
    }

    /// <summary>
    /// Attach a fixture to a body and subscribe the pre-allocated collision delegates.
    /// Call after CreateBody; typically called by the collider components via IVelcroColliderProvider.
    /// </summary>
    public void SubscribeFixtureEvents(Fixture fixture)
    {
        fixture.OnCollision  += _onCollisionHandler;
        fixture.OnSeparation += _onSeparationHandler;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // State snapshot
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Copy current body states into <paramref name="destination"/>.
    /// Zero allocation — writes directly into the caller's pre-allocated array.
    /// </summary>
    public void CopyStateTo(EntityState[] destination, out int count)
    {
        count = 0;
        for (int id = 0; id < _maxBodies; id++)
        {
            Body? body = _bodyRegistry[id];
            if (body == null) continue;

            destination[count++] = new EntityState
            {
                EntityId  = id,
                IsAwake   = body.IsAwake,
                Transform = new TransformState
                {
                    Position        = body.Position,
                    Angle           = body.Rotation,
                    LinearVelocity  = body.LinearVelocity,
                    AngularVelocity = body.AngularVelocity,
                },
            };
        }
    }

    public TransformState GetBodyState(int entityId)
    {
        Body? body = GetBodyOrNull(entityId);
        if (body == null) return default;
        return new TransformState
        {
            Position        = body.Position,
            Angle           = body.Rotation,
            LinearVelocity  = body.LinearVelocity,
            AngularVelocity = body.AngularVelocity,
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Force API
    // ─────────────────────────────────────────────────────────────────────────

    public void ApplyForce(int entityId, in SNV2 force, ForceMode mode = ForceMode.Force)
    {
        Body? body = GetDynamicBodyOrNull(entityId);
        if (body == null) return;
        switch (mode)
        {
            case ForceMode.Force:
                body.ApplyForce(force);
                break;
            case ForceMode.Impulse:
                body.ApplyLinearImpulse(force);
                break;
            case ForceMode.VelocityChange:
                // mass-independent: supply the impulse that would produce deltaV regardless of mass
                body.ApplyLinearImpulse(force * body.Mass);
                break;
            case ForceMode.Acceleration:
                // mass-independent continuous force
                body.ApplyForce(force * body.Mass);
                break;
        }
    }

    public void ApplyForceAtPoint(int entityId, in SNV2 force, in SNV2 worldPoint)
        => GetDynamicBodyOrNull(entityId)?.ApplyForce(force, worldPoint);

    public void ApplyTorque(int entityId, float torque)
        => GetDynamicBodyOrNull(entityId)?.ApplyTorque(torque);

    public void ApplyAngularImpulse(int entityId, float impulse)
        => GetDynamicBodyOrNull(entityId)?.ApplyAngularImpulse(impulse);

    // ─────────────────────────────────────────────────────────────────────────
    // State getters / setters
    // ─────────────────────────────────────────────────────────────────────────

    public SNV2  GetLinearVelocity(int entityId)  => GetBodyOrNull(entityId)?.LinearVelocity ?? default;
    public float GetAngularVelocity(int entityId) => GetBodyOrNull(entityId)?.AngularVelocity ?? 0f;
    public float GetMass(int entityId)            => GetBodyOrNull(entityId)?.Mass ?? 0f;

    public void SetLinearVelocity(int entityId, in SNV2 vel)
    {
        Body? b = GetBodyOrNull(entityId);
        if (b != null) b.LinearVelocity = vel;
    }

    public void SetAngularVelocity(int entityId, float angVel)
    {
        Body? b = GetBodyOrNull(entityId);
        if (b != null) b.AngularVelocity = angVel;
    }

    public void SetPosition(int entityId, in SNV2 pos)
    {
        Body? b = GetBodyOrNull(entityId);
        if (b != null) b.Position = pos;
    }

    public bool IsSleeping(int entityId) => !(GetBodyOrNull(entityId)?.IsAwake ?? false);

    public void SetSleepState(int entityId, bool sleep)
    {
        Body? b = GetBodyOrNull(entityId);
        if (b == null) return;
        b.IsAwake = !sleep;
    }

    public void ResetDynamics(int entityId)
        => GetBodyOrNull(entityId)?.ResetDynamics();

    public void SetGravityScale(int entityId, float scale)
    {
        Body? b = GetBodyOrNull(entityId);
        if (b != null) b.GravityScale = scale;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Physics queries
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Cast a ray from <paramref name="origin"/> in <paramref name="direction"/> for
    /// <paramref name="distance"/> simulation-units. Results written into <paramref name="buffer"/>.
    /// </summary>
    public void Raycast(
        in SNV2              origin,
        in SNV2              direction,
        float                distance,
        PhysicsQueryBuffer   buffer,
        int                  layerMask = -1)
    {
        buffer.ClearRaycast();
        _activeQueryBuffer = buffer;
        SNV2 end = origin + direction * distance;
        _world.RayCast(_rayCastCallback, origin, end);
        _activeQueryBuffer = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Network
    // ─────────────────────────────────────────────────────────────────────────

    public void SetNetworkProvider(INetworkStateProvider? provider)
        => _networkProvider = provider;

    // ─────────────────────────────────────────────────────────────────────────
    // Private — collision handlers (called inline during World.Step)
    // ─────────────────────────────────────────────────────────────────────────

    private bool HandleCollision(Fixture sender, Fixture other, Contact contact)
    {
        if (sender.Body.UserData is not EntityToken tokenA) return true;
        if (other.Body.UserData  is not EntityToken tokenB) return true;

        int idA = tokenA.EntityId;
        int idB = tokenB.EntityId;

        if (sender.IsSensor || other.IsSensor)
        {
            if (_contactTracker.TryAddTrigger(idA, idB, _tickNumber, out bool isNew) && isNew)
            {
                _events.EnqueueTriggerEnter(new TriggerData
                {
                    TriggerEntityId = sender.IsSensor ? idA : idB,
                    OtherEntityId   = sender.IsSensor ? idB : idA,
                    TickNumber      = _tickNumber,
                });
            }
        }
        else
        {
            if (_contactTracker.TryAddNew(idA, idB, _tickNumber, out bool isNew) && isNew)
            {
                SNV2 contactPoint = default;
                SNV2 contactNormal = default;

                // GetWorldManifold only valid when the manifold has points
                if (contact.Manifold.PointCount > 0)
                {
                    contact.GetWorldManifold(out contactNormal, out var points);
                    // FixedArray2<T> — first contact point is at index 0
                    contactPoint = points[0];
                }

                _events.EnqueueEnter(new CollisionData
                {
                    EntityIdA     = idA,
                    EntityIdB     = idB,
                    ContactPoint  = contactPoint,
                    ContactNormal = contactNormal,
                    TickNumber    = _tickNumber,
                });
            }
        }
        return true; // always allow collision to proceed
    }

    private void HandleSeparation(Fixture sender, Fixture other, Contact contact)
    {
        if (sender.Body.UserData is not EntityToken tokenA) return;
        if (other.Body.UserData  is not EntityToken tokenB) return;

        int idA = tokenA.EntityId;
        int idB = tokenB.EntityId;

        if (sender.IsSensor || other.IsSensor)
        {
            if (_contactTracker.RemoveTrigger(idA, idB))
            {
                _events.EnqueueTriggerExit(new TriggerData
                {
                    TriggerEntityId = sender.IsSensor ? idA : idB,
                    OtherEntityId   = sender.IsSensor ? idB : idA,
                    TickNumber      = _tickNumber,
                });
            }
        }
        else
        {
            if (_contactTracker.Remove(idA, idB))
            {
                _events.EnqueueExit(new CollisionData
                {
                    EntityIdA  = idA,
                    EntityIdB  = idB,
                    TickNumber = _tickNumber,
                });
            }
        }
    }

    private float RayCastCallback(Fixture fixture, SNV2 point, SNV2 normal, float fraction)
    {
        if (_activeQueryBuffer == null) return -1f;
        if (_activeQueryBuffer.RaycastCount >= _activeQueryBuffer.RaycastResults.Length)
            return 0f; // buffer full — stop ray

        int entityId = (fixture.Body.UserData as EntityToken)?.EntityId ?? -1;

        _activeQueryBuffer.RaycastResults[_activeQueryBuffer.RaycastCount++] = new RaycastHit
        {
            EntityId     = entityId,
            FixtureIndex = 0, // future: look up fixture index
            Point        = point,
            Normal       = normal,
            Fraction     = fraction,
            IsTrigger    = fixture.IsSensor,
        };

        return 1f; // continue ray
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private — freeze constraints (applied after each World.Step)
    // ─────────────────────────────────────────────────────────────────────────

    private void ApplyFreezeConstraints()
    {
        for (int i = 0; i < _frozenCount; i++)
        {
            int   id  = _frozenIds[i];
            Body? body = _bodyRegistry[id];
            if (body == null) continue;

            RigidbodyConstraints c   = _constraints[id];
            SNV2                 pos = body.Position;
            SNV2                 vel = body.LinearVelocity;

            if ((c & RigidbodyConstraints.FreezePositionX) != 0)
            {
                pos.X = _frozenPositions[id].X;
                vel.X = 0f;
            }
            if ((c & RigidbodyConstraints.FreezePositionY) != 0)
            {
                pos.Y = _frozenPositions[id].Y;
                vel.Y = 0f;
            }

            body.Position       = pos;
            body.LinearVelocity = vel;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private — body lookup helpers (O(1))
    // ─────────────────────────────────────────────────────────────────────────

    private Body? GetBodyOrNull(int entityId)
        => (uint)entityId < (uint)_maxBodies ? _bodyRegistry[entityId] : null;

    private Body? GetDynamicBodyOrNull(int entityId)
    {
        Body? b = GetBodyOrNull(entityId);
        return b?.BodyType == BodyType.Dynamic ? b : null;
    }
}

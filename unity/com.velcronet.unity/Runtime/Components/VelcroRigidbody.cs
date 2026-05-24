using nkast.Aether.Physics2D.Dynamics;
using UnityEngine;

namespace VelcroNet
{
    /// <summary>
    /// The primary per-entity component. Caches its Transform in Awake and routes
    /// physics calls to PhysicsWorldManager. No Update() — goes fully dormant after
    /// registration. All GetComponent calls happen at init time, never at runtime.
    /// </summary>
    [AddComponentMenu("VelcroNet/Rigidbody")]
    [DisallowMultipleComponent]
    public sealed class VelcroRigidbody : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────
        [SerializeField] private BodyType             _bodyType      = BodyType.Dynamic;
        [SerializeField] private float                _mass          = 1f;
        [SerializeField] private float                _linearDamping;
        [SerializeField] private float                _angularDamping = 0.05f;
        [SerializeField] private float                _gravityScale   = 1f;
        [SerializeField] private bool                 _fixedRotation;
        [SerializeField] private RigidbodyConstraints _constraints;
        [SerializeField] private InterpolationMode    _interpolation  = InterpolationMode.Interpolate;

        // ─── Runtime state ───────────────────────────────────────────────────
        private Transform             _transform;     // cached in Awake
        private PhysicsWorldManager?  _world;
        private int                   _entityId = -1;

        // Interpolation state (set by VelcroViewManager)
        private System.Numerics.Vector2 _prevPosition;
        private System.Numerics.Vector2 _currPosition;
        private float                   _prevAngle;
        private float                   _currAngle;

        // ─── Inspector accessors (read by VelcroViewManager during registration) ─
        public BodyType             BodyType      => _bodyType;
        public float                Mass          => _mass;
        public float                LinearDamping => _linearDamping;
        public float                AngularDamping => _angularDamping;
        public float                GravityScale  => _gravityScale;
        public bool                 FixedRotation => _fixedRotation;
        public RigidbodyConstraints Constraints   => _constraints;
        public int                  EntityId      => _entityId;

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _transform = transform; // the ONLY GetComponent-class call ever made
        }

        /// <summary>Called once by VelcroViewManager after the world is created.</summary>
        internal void Register(PhysicsWorldManager world, int entityId)
        {
            _world    = world;
            _entityId = entityId;
        }

        // ─── Force API ───────────────────────────────────────────────────────

        public void AddForce(Vector2 force, ForceMode mode = ForceMode.Force)
            => _world?.ApplyForce(_entityId, MathBridge.WorldToSim(force), mode);

        public void AddForce(Vector2 force)
            => _world?.ApplyForce(_entityId, MathBridge.WorldToSim(force));

        public void AddForceAtPosition(Vector2 force, Vector2 worldPos)
        {
            var f = MathBridge.WorldToSim(force);
            var p = MathBridge.WorldToSim(worldPos);
            _world?.ApplyForceAtPoint(_entityId, in f, in p);
        }

        public void AddRelativeForce(Vector2 relativeForce)
        {
            // Rotate force by body's current angle before applying
            if (_world == null) return;
            float angle = _world.GetBodyState(_entityId).Angle;
            float cos   = UnityEngine.Mathf.Cos(angle);
            float sin   = UnityEngine.Mathf.Sin(angle);
            var worldForce = new System.Numerics.Vector2(
                relativeForce.x * cos - relativeForce.y * sin,
                relativeForce.x * sin + relativeForce.y * cos);
            var simForce = worldForce / SimulationConstants.PixelsPerMeter;
            _world.ApplyForce(_entityId, in simForce);
        }

        public void AddTorque(float torque, ForceMode mode = ForceMode.Force)
        {
            if (_world == null) return;
            if (mode == ForceMode.Impulse || mode == ForceMode.VelocityChange)
                _world.ApplyAngularImpulse(_entityId, torque);
            else
                _world.ApplyTorque(_entityId, torque);
        }

        // ─── State API ───────────────────────────────────────────────────────

        public Vector2 velocity
        {
            get => MathBridge.SimToWorld(_world?.GetLinearVelocity(_entityId) ?? default);
            set
            {
                var v = MathBridge.WorldToSim(value);
                _world?.SetLinearVelocity(_entityId, in v);
            }
        }

        public float angularVelocity
        {
            get => _world?.GetAngularVelocity(_entityId) ?? 0f;
            set => _world?.SetAngularVelocity(_entityId, value);
        }

        public bool isSleeping => _world?.IsSleeping(_entityId) ?? false;

        public void Sleep()  => _world?.SetSleepState(_entityId, true);
        public void WakeUp() => _world?.SetSleepState(_entityId, false);

        public void ResetDynamics() => _world?.ResetDynamics(_entityId);

        // ─── Interpolation (called by VelcroViewManager) ─────────────────────

        /// <summary>Record state before a physics tick for interpolation.</summary>
        internal void SnapshotPreTick(in System.Numerics.Vector2 pos, float angle)
        {
            _prevPosition = _currPosition;
            _prevAngle    = _currAngle;
            _currPosition = pos;
            _currAngle    = angle;
        }

        /// <summary>
        /// Push the interpolated transform to Unity. Called from VelcroViewManager.LateUpdate.
        /// Uses batched SetPositionAndRotation — one C++  call per body.
        /// </summary>
        internal void ApplyInterpolatedTransform(float alpha)
        {
            System.Numerics.Vector2 simPos;
            float worldAngle;

            switch (_interpolation)
            {
                case InterpolationMode.None:
                    simPos     = _currPosition;
                    worldAngle = MathExtensions.ToWorldAngle(_currAngle);
                    break;

                case InterpolationMode.Interpolate:
                    simPos = System.Numerics.Vector2.Lerp(_prevPosition, _currPosition, alpha);
                    worldAngle = MathExtensions.LerpAngle(
                        MathExtensions.ToWorldAngle(_prevAngle),
                        MathExtensions.ToWorldAngle(_currAngle),
                        alpha);
                    break;

                case InterpolationMode.Extrapolate:
                default:
                    float clampedAlpha = alpha < 0f ? 0f : alpha > 1.5f ? 1.5f : alpha;
                    simPos = _currPosition + (_currPosition - _prevPosition) * clampedAlpha;
                    worldAngle = MathExtensions.ToWorldAngle(
                        _currAngle + (_currAngle - _prevAngle) * clampedAlpha);
                    break;
            }

            _transform.SetPositionAndRotation(
                MathBridge.SimToWorld3(simPos),
                UnityEngine.Quaternion.Euler(0f, 0f, worldAngle));
        }
    }
}

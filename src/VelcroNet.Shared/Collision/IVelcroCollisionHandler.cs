namespace VelcroNet.Collision;

/// <summary>
/// Implement on any MonoBehaviour (alongside VelcroRigidbody) to receive Unity-style
/// collision callbacks. Uses <c>ref</c> to avoid copying the struct.
/// </summary>
public interface IVelcroCollisionHandler
{
    void OnCollisionEnter(ref CollisionData collision);
    void OnCollisionExit (ref CollisionData collision);
}

/// <summary>
/// Implement to receive trigger callbacks from sensor fixtures.
/// </summary>
public interface IVelcroTriggerHandler
{
    void OnTriggerEnter(ref TriggerData trigger);
    void OnTriggerExit (ref TriggerData trigger);
}

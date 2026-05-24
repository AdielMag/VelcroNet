using UnityEngine;

namespace VelcroNet
{
    [CreateAssetMenu(fileName = "VelcroPhysicsMaterial", menuName = "VelcroNet/Physics Material")]
    public sealed class VelcroPhysicsMaterial : ScriptableObject
    {
        [Range(0f, 10f)]  public float Friction    = 0.2f;
        [Range(0f, 1f)]   public float Restitution = 0f;
        [Min(0.001f)]     public float Density     = 1f;
    }
}
